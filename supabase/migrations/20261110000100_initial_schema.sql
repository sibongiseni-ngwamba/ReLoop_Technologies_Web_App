create extension if not exists pgcrypto with schema extensions;

create table if not exists public.profiles (
  id uuid primary key references auth.users (id) on delete cascade,
  first_name text not null default '',
  last_name text not null default '',
  email text not null default '',
  phone_number text not null default '',
  address text not null default '',
  role text not null default 'resident' check (role in ('resident', 'collector', 'admin')),
  is_active boolean not null default true,
  profile_image_url text,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create table if not exists public.agents (
  agent_id text primary key default gen_random_uuid()::text,
  user_id uuid not null unique references public.profiles (id) on delete cascade,
  name text not null default '',
  contact_number text not null default '',
  assigned_zone text not null default '',
  is_available boolean not null default true,
  is_active boolean not null default true,
  created_at timestamptz not null default now()
);

create table if not exists public.pickups (
  pickup_id text primary key default gen_random_uuid()::text,
  user_id uuid not null references public.profiles (id) on delete cascade,
  agent_id text references public.agents (agent_id) on delete set null,
  scheduled_date timestamptz not null,
  waste_type text not null default '',
  waste_types text[] not null default '{}',
  address text not null default '',
  status text not null default 'Scheduled' check (status in ('Scheduled', 'In Progress', 'Completed', 'Cancelled')),
  notes text not null default '',
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now(),
  completed_at timestamptz,
  cancelled_at timestamptz,
  cancellation_reason text
);

create table if not exists public.reward_accounts (
  user_id uuid primary key references public.profiles (id) on delete cascade,
  points_balance integer not null default 0 check (points_balance >= 0),
  total_earned integer not null default 0 check (total_earned >= 0),
  total_redeemed integer not null default 0 check (total_redeemed >= 0),
  last_updated timestamptz not null default now()
);

create table if not exists public.reward_items (
  reward_item_id text primary key default gen_random_uuid()::text,
  item_name text not null,
  description text not null default '',
  points_cost integer not null check (points_cost > 0),
  is_active boolean not null default true,
  image_url text,
  stock_quantity integer check (stock_quantity is null or stock_quantity >= 0),
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create table if not exists public.redemption_logs (
  redemption_id text primary key default gen_random_uuid()::text,
  user_id uuid not null references public.profiles (id) on delete cascade,
  reward_item_id text not null references public.reward_items (reward_item_id),
  points_used integer not null check (points_used > 0),
  voucher_code text,
  status text not null default 'Redeemed',
  redeemed_at timestamptz not null default now(),
  created_at timestamptz not null default now()
);

create table if not exists public.points_transactions (
  transaction_id text primary key default gen_random_uuid()::text,
  user_id uuid not null references public.profiles (id) on delete cascade,
  transaction_type text not null check (transaction_type in ('earn', 'redeem', 'adjust')),
  points integer not null,
  related_id text,
  description text not null default '',
  created_at timestamptz not null default now(),
  unique (user_id, transaction_type, related_id)
);

create table if not exists public.waste_categories (
  waste_category_id text primary key default gen_random_uuid()::text,
  category_name text not null,
  description text not null default '',
  model_label text not null default '',
  is_active boolean not null default true,
  created_at timestamptz not null default now()
);

create table if not exists public.waste_logs (
  waste_log_id text primary key default gen_random_uuid()::text,
  user_id uuid not null references public.profiles (id) on delete cascade,
  pickup_id text references public.pickups (pickup_id) on delete set null,
  waste_category_id text not null references public.waste_categories (waste_category_id),
  weight_kg numeric(10, 3) not null default 0 check (weight_kg >= 0),
  logged_at timestamptz not null default now(),
  source text not null default 'Mobile AI Scan',
  model_label text,
  confidence_score numeric(5, 4) check (confidence_score is null or confidence_score between 0 and 1),
  image_url text
);

create table if not exists public.guidance_content (
  guidance_id text primary key default gen_random_uuid()::text,
  waste_category_id text not null references public.waste_categories (waste_category_id) on delete cascade,
  title text not null,
  content text not null,
  image_path text,
  is_active boolean not null default true,
  created_at timestamptz not null default now(),
  updated_at timestamptz not null default now()
);

create table if not exists public.notifications (
  notification_id text primary key default gen_random_uuid()::text,
  user_id uuid not null references public.profiles (id) on delete cascade,
  title text not null,
  message text not null,
  type text not null default 'INFO',
  sent_at timestamptz not null default now(),
  is_read boolean not null default false,
  related_pickup_id text references public.pickups (pickup_id) on delete set null,
  created_at timestamptz not null default now()
);

create index if not exists pickups_user_scheduled_idx on public.pickups (user_id, scheduled_date desc);
create index if not exists pickups_agent_status_idx on public.pickups (agent_id, status);
create index if not exists waste_logs_user_logged_idx on public.waste_logs (user_id, logged_at desc);
create index if not exists notifications_user_sent_idx on public.notifications (user_id, sent_at desc);
create index if not exists points_transactions_user_created_idx on public.points_transactions (user_id, created_at desc);
create index if not exists redemption_logs_user_created_idx on public.redemption_logs (user_id, created_at desc);

create or replace function public.handle_new_user()
returns trigger
language plpgsql
security definer
set search_path = ''
as $$
begin
  insert into public.profiles (id, email, first_name, last_name)
  values (
    new.id,
    coalesce(new.email, ''),
    coalesce(new.raw_user_meta_data ->> 'first_name', ''),
    coalesce(new.raw_user_meta_data ->> 'last_name', '')
  ) on conflict (id) do nothing;
  insert into public.reward_accounts (user_id) values (new.id) on conflict (user_id) do nothing;
  return new;
end;
$$;

drop trigger if exists on_auth_user_created on auth.users;
create trigger on_auth_user_created
after insert on auth.users
for each row execute procedure public.handle_new_user();

create or replace function public.is_staff()
returns boolean
language sql
stable
security definer
set search_path = ''
as $$
  select exists (
    select 1 from public.profiles
    where id = (select auth.uid()) and role in ('collector', 'admin') and is_active
  );
$$;

create or replace function public.redeem_reward(p_reward_item_id text)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_user_id uuid := auth.uid();
  v_item public.reward_items%rowtype;
  v_account public.reward_accounts%rowtype;
  v_redemption public.redemption_logs%rowtype;
  v_tx public.points_transactions%rowtype;
begin
  if v_user_id is null then
    raise exception 'Authentication required';
  end if;
  select * into v_item from public.reward_items where reward_item_id = p_reward_item_id and is_active for update;
  if not found then
    raise exception 'Reward is unavailable';
  end if;
  select * into v_account from public.reward_accounts where user_id = v_user_id for update;
  if not found or v_account.points_balance < v_item.points_cost then
    raise exception 'Insufficient points';
  end if;
  if v_item.stock_quantity is not null and v_item.stock_quantity < 1 then
    raise exception 'Reward is out of stock';
  end if;

  update public.reward_accounts
    set points_balance = points_balance - v_item.points_cost,
        total_redeemed = total_redeemed + v_item.points_cost,
        last_updated = now()
    where user_id = v_user_id returning * into v_account;
  if v_item.stock_quantity is not null then
    update public.reward_items set stock_quantity = stock_quantity - 1, updated_at = now() where reward_item_id = v_item.reward_item_id;
  end if;
  insert into public.redemption_logs (user_id, reward_item_id, points_used, voucher_code)
    values (v_user_id, v_item.reward_item_id, v_item.points_cost, 'RELOOP-' || upper(substr(encode(extensions.gen_random_bytes(5), 'hex'), 1, 8)))
    returning * into v_redemption;
  insert into public.points_transactions (user_id, transaction_type, points, related_id, description)
    values (v_user_id, 'redeem', -v_item.points_cost, v_redemption.redemption_id, 'Redeemed: ' || v_item.item_name)
    returning * into v_tx;

  return jsonb_build_object(
    'account', jsonb_build_object('userId', v_account.user_id, 'pointsBalance', v_account.points_balance, 'totalEarned', v_account.total_earned, 'totalRedeemed', v_account.total_redeemed, 'lastUpdated', (extract(epoch from v_account.last_updated) * 1000)::bigint),
    'redemption', jsonb_build_object('redemptionId', v_redemption.redemption_id, 'userId', v_redemption.user_id, 'rewardItemId', v_redemption.reward_item_id, 'pointsUsed', v_redemption.points_used, 'voucherCode', v_redemption.voucher_code, 'status', v_redemption.status, 'redeemedAt', (extract(epoch from v_redemption.redeemed_at) * 1000)::bigint, 'createdAt', (extract(epoch from v_redemption.created_at) * 1000)::bigint),
    'transaction', jsonb_build_object('transactionId', v_tx.transaction_id, 'userId', v_tx.user_id, 'transactionType', v_tx.transaction_type, 'points', v_tx.points, 'relatedId', v_tx.related_id, 'description', v_tx.description, 'createdAt', (extract(epoch from v_tx.created_at) * 1000)::bigint)
  );
end;
$$;

revoke all on function public.redeem_reward(text) from public;
grant execute on function public.redeem_reward(text) to authenticated;

create or replace function public.award_points(p_points integer, p_related_id text, p_description text)
returns jsonb
language plpgsql
security definer
set search_path = ''
as $$
declare
  v_user_id uuid := auth.uid();
  v_tx public.points_transactions%rowtype;
  v_account public.reward_accounts%rowtype;
  v_expected_points integer;
begin
  if v_user_id is null then raise exception 'Authentication required'; end if;
  if p_points <= 0 then raise exception 'Points must be positive'; end if;
  select 10 into v_expected_points from public.waste_logs
    where waste_log_id = p_related_id and user_id = v_user_id;
  if not found then
    select 150 into v_expected_points from public.pickups
      where pickup_id = p_related_id and user_id = v_user_id and status = 'Completed';
  end if;
  if not found or v_expected_points <> p_points then
    raise exception 'Points can only be awarded for a verified scan or completed pickup';
  end if;
  insert into public.points_transactions (user_id, transaction_type, points, related_id, description)
  values (v_user_id, 'earn', p_points, nullif(p_related_id, ''), coalesce(p_description, ''))
  on conflict (user_id, transaction_type, related_id) do nothing
  returning * into v_tx;

  if not found then
    select * into v_tx from public.points_transactions
    where user_id = v_user_id and transaction_type = 'earn' and related_id is not distinct from nullif(p_related_id, '');
    select * into v_account from public.reward_accounts where user_id = v_user_id;
    return jsonb_build_object('transactionId', v_tx.transaction_id, 'alreadyApplied', true, 'pointsBalance', v_account.points_balance);
  end if;

  update public.reward_accounts
  set points_balance = points_balance + p_points, total_earned = total_earned + p_points, last_updated = now()
  where user_id = v_user_id returning * into v_account;
  return jsonb_build_object(
    'transactionId', v_tx.transaction_id,
    'alreadyApplied', false,
    'pointsBalance', v_account.points_balance
  );
end;
$$;

revoke all on function public.award_points(integer, text, text) from public;
grant execute on function public.award_points(integer, text, text) to authenticated;

alter table public.profiles enable row level security;
alter table public.agents enable row level security;
alter table public.pickups enable row level security;
alter table public.reward_accounts enable row level security;
alter table public.reward_items enable row level security;
alter table public.redemption_logs enable row level security;
alter table public.points_transactions enable row level security;
alter table public.waste_categories enable row level security;
alter table public.waste_logs enable row level security;
alter table public.guidance_content enable row level security;
alter table public.notifications enable row level security;

create policy "profiles are readable by owner or staff" on public.profiles for select to authenticated using (id = (select auth.uid()) or (select public.is_staff()));
create policy "users can create own profile" on public.profiles for insert to authenticated with check (id = (select auth.uid()) and role = 'resident');
create policy "users can update own non-role profile" on public.profiles for update to authenticated using (id = (select auth.uid())) with check (id = (select auth.uid()) and role = 'resident');

create policy "agents visible to signed in users" on public.agents for select to authenticated using (true);
create policy "staff manage agents" on public.agents for all to authenticated using ((select public.is_staff())) with check ((select public.is_staff()));

create policy "users view own pickups and staff view all" on public.pickups for select to authenticated using (user_id = (select auth.uid()) or (select public.is_staff()));
create policy "users create own pickups" on public.pickups for insert to authenticated with check (user_id = (select auth.uid()));
create policy "users update own scheduled pickups or staff update any" on public.pickups for update to authenticated using ((user_id = (select auth.uid()) and status in ('Scheduled', 'Cancelled')) or (select public.is_staff())) with check (user_id = (select auth.uid()) or (select public.is_staff()));
create policy "staff delete pickups" on public.pickups for delete to authenticated using ((select public.is_staff()));

create policy "users view own rewards" on public.reward_accounts for select to authenticated using (user_id = (select auth.uid()) or (select public.is_staff()));
create policy "users cannot directly edit rewards" on public.reward_accounts for update to authenticated using (false);
create policy "users cannot create reward balances" on public.reward_accounts for insert to authenticated with check (false);
create policy "signed in users view reward catalogue" on public.reward_items for select to authenticated using (is_active or (select public.is_staff()));
create policy "staff manage reward catalogue" on public.reward_items for all to authenticated using ((select public.is_staff())) with check ((select public.is_staff()));
create policy "users view own redemptions" on public.redemption_logs for select to authenticated using (user_id = (select auth.uid()) or (select public.is_staff()));
create policy "users cannot directly create redemptions" on public.redemption_logs for insert to authenticated with check (false);
create policy "users view own points ledger" on public.points_transactions for select to authenticated using (user_id = (select auth.uid()) or (select public.is_staff()));
create policy "users cannot directly write points ledger" on public.points_transactions for insert to authenticated with check (false);

create policy "signed in users view waste categories" on public.waste_categories for select to authenticated using (is_active or (select public.is_staff()));
create policy "staff manage waste categories" on public.waste_categories for all to authenticated using ((select public.is_staff())) with check ((select public.is_staff()));
create policy "users view own waste logs and staff view all" on public.waste_logs for select to authenticated using (user_id = (select auth.uid()) or (select public.is_staff()));
create policy "users create own waste logs" on public.waste_logs for insert to authenticated with check (user_id = (select auth.uid()));
create policy "signed in users view active guidance" on public.guidance_content for select to authenticated using (is_active or (select public.is_staff()));
create policy "staff manage guidance" on public.guidance_content for all to authenticated using ((select public.is_staff())) with check ((select public.is_staff()));
create policy "users view own notifications" on public.notifications for select to authenticated using (user_id = (select auth.uid()) or (select public.is_staff()));
create policy "users mark own notifications read" on public.notifications for update to authenticated using (user_id = (select auth.uid())) with check (user_id = (select auth.uid()));
create policy "users create own notifications" on public.notifications for insert to authenticated with check (user_id = (select auth.uid()));
create policy "staff create notifications" on public.notifications for insert to authenticated with check ((select public.is_staff()));
create policy "users delete own notifications" on public.notifications for delete to authenticated using (user_id = (select auth.uid()));

grant usage on schema public to authenticated;
grant select, insert, update on public.profiles to authenticated;
grant select, insert, update, delete on public.agents, public.pickups, public.reward_items, public.waste_categories, public.guidance_content, public.notifications, public.waste_logs to authenticated;
grant select on public.reward_accounts, public.redemption_logs, public.points_transactions to authenticated;
