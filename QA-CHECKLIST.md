# Manual QA checklist

## Supabase Auth and web sessions

- Confirm Supabase URL and publishable key are provided through local secrets or deployment settings.
- Sign up and verify email confirmation behavior for the configured Supabase Auth provider.
- Sign in with valid and invalid credentials; confirm the server validates access tokens with Supabase Auth.
- Confirm login creates an HTTP-only web session and logout clears it.
- Confirm inactive profiles cannot sign in and role changes invalidate stale sessions.

## Shared member data

- Confirm profile, pickups, reward balances, catalogue, redemptions, scan logs, notifications, and guidance use the shared Supabase tables.
- Compare a member record edited in the web portal with the Android app using the same Supabase project.
- Confirm member API routes require authentication and only return the signed-in user's private data.
- Confirm points redemption is transactional and cannot overdraw a balance.

## Administrator access

- Sign in as a profile with `role = 'admin'` and verify the admin dashboard loads.
- Verify user active/role management, pickup assignment/status changes, reward catalogue changes, redemption updates, activity, and CSV report.
- Confirm a member account receives 403 on `/api/admin/*` routes and cannot change its own role.

## Supabase deployment

- Review SQL migrations before deployment and confirm the project reference is correct.
- Confirm deployment workflow applies only reviewed Supabase migrations.
- Confirm the application has its database connection string only in secret storage and its database role can write the required tables/function.
