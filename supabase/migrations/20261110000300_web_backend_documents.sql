-- Web-only fields extend the shared normalized mobile schema.
alter table public.profiles add column if not exists preferred_category text not null default 'Recyclables';
alter table public.pickups add column if not exists reference_number text not null default '';
alter table public.pickups add column if not exists weight_kg numeric(10, 3);
alter table public.pickups alter column notes drop not null;
alter table public.pickups drop constraint if exists pickups_status_check;
alter table public.pickups add constraint pickups_status_check check (status in ('Scheduled', 'Assigned', 'In Progress', 'Completed', 'Cancelled'));
alter table public.redemption_logs alter column redeemed_at drop not null;
alter table public.waste_logs add column if not exists item_name text not null default '';
alter table public.waste_logs add column if not exists disposition text not null default 'Recyclable';
alter table public.waste_logs add column if not exists points_awarded integer not null default 0;
alter table public.guidance_content add column if not exists preparation text;
alter table public.guidance_content add column if not exists accepted_items text[] not null default '{}';
alter table public.guidance_content add column if not exists excluded_items text[] not null default '{}';

create table if not exists public.contact_messages (
  contact_id text primary key default gen_random_uuid()::text,
  full_name text not null,
  email text not null,
  subject text not null,
  message text not null,
  status text not null default 'New',
  created_at timestamptz not null default now()
);
alter table public.contact_messages enable row level security;
revoke all on public.contact_messages from anon, authenticated;

-- Service role database connections are used only by the ASP.NET server.
-- The allowlist prevents callers from using this helper to write arbitrary tables.
create or replace function public.upsert_reloop_row(p_table text, p_primary_key text, p_document_id text, p_payload jsonb)
returns void
language plpgsql
security definer
set search_path = ''
as $$
declare
  allowed_key text;
  assignments text;
begin
  allowed_key := case p_table
    when 'profiles' then 'id'
    when 'agents' then 'agent_id'
    when 'pickups' then 'pickup_id'
    when 'waste_logs' then 'waste_log_id'
    when 'waste_categories' then 'waste_category_id'
    when 'guidance_content' then 'guidance_id'
    when 'reward_accounts' then 'user_id'
    when 'reward_items' then 'reward_item_id'
    when 'redemption_logs' then 'redemption_id'
    when 'points_transactions' then 'transaction_id'
    when 'notifications' then 'notification_id'
    when 'contact_messages' then 'contact_id'
    else null
  end;
  if allowed_key is null or p_primary_key <> allowed_key then raise exception 'Unsupported ReLoop table/key'; end if;
  select string_agg(format('%I = excluded.%I', a.attname, a.attname), ', ' order by a.attnum)
    into assignments
    from pg_attribute a
    where a.attrelid = format('public.%I', p_table)::regclass and a.attnum > 0 and not a.attisdropped and a.attname <> allowed_key;
  execute format(
    'insert into public.%I select (jsonb_populate_record(null::public.%I, $1)).* on conflict (%I) do update set %s',
    p_table, p_table, allowed_key, assignments
  ) using jsonb_set(p_payload, array[allowed_key], to_jsonb(p_document_id), true);
end;
$$;
revoke all on function public.upsert_reloop_row(text, text, text, jsonb) from public, anon, authenticated;
grant execute on function public.upsert_reloop_row(text, text, text, jsonb) to service_role;
