# ReLoop Backend and REST API

## Architecture

```text
Web browser ── Supabase Auth access token ──> ASP.NET web/API ──> Supabase Auth + PostgreSQL
Android app ── Supabase Auth access token ──> Supabase reloop-api Edge Function ──> same PostgreSQL
```

The REST API is part of this ASP.NET Core project, in `Api/ApiEndpointMappings.cs`; it does not require a second project to run. The mobile repository already contains its Edge Function under `supabase/functions/reloop-api`. Both apps use the same Supabase project (`pjsrpajqslyamavbvyxz`) and normalized tables. The web adapter maps the existing web service collection interface to those tables, including `profiles.role`, so administrator role changes apply to the shared profile.

The API uses the Supabase publishable/anon key only for Auth verification and browser Supabase Auth. PostgreSQL access uses a server-side connection string stored in user secrets or deployment secrets. Never expose a database password or Supabase secret/service-role key to the browser or Android app. The migration helper function is allowlisted to ReLoop tables and executable only by `service_role`; the server's database role must be trusted.

## Local setup

1. Create or select the Supabase project used by Android. The mobile project's `local.properties` currently points at `https://pjsrpajqslyamavbvyxz.supabase.co`.
2. Install the .NET 8 SDK and Supabase CLI.
3. In Supabase, copy the publishable key and a PostgreSQL connection string. Use the direct connection where available, otherwise the session pooler connection string.
4. At the web repository root, configure local-only secrets:

   ```powershell
   dotnet user-secrets set "Supabase:Url" "https://pjsrpajqslyamavbvyxz.supabase.co"
   dotnet user-secrets set "Supabase:AnonKey" "<publishable key>"
   dotnet user-secrets set "Supabase:ConnectionString" "Host=<db-host>;Port=5432;Database=postgres;Username=postgres;Password=<db-password>;SSL Mode=Require;Trust Server Certificate=true"
   ```

5. Apply migrations if the mobile app has not already applied them:

   ```powershell
   supabase login
   supabase link --project-ref pjsrpajqslyamavbvyxz
   supabase db push
   ```

   The first two migrations create the shared normalized tables and catalogue. Migration `20261110000300_web_backend_documents.sql` adds fields used only by the web portal and the protected upsert function. If the first two migration versions are already recorded in this Supabase project, the CLI skips them and applies only newer versions.
6. Run the API and web app:

   ```powershell
   dotnet run --project ReLoop_Technologies_Web_App.csproj
   ```

The API is hosted on the same origin as the web app. A local browser can open `http://localhost:5197`; HTTPS is also available at `https://localhost:7036`.

## REST routes

Authentication endpoints accept Supabase Auth access tokens from the web client. Successful login creates an HTTP-only app cookie; normal web API calls use that cookie. JSON errors follow ASP.NET Problem Details.

| Method | Route | Access | Purpose |
|---|---|---|---|
| GET | `/api/supabase/config` | Public | Return Supabase URL and publishable key for browser Auth |
| POST | `/api/auth/signup` | Public + Supabase token | Ensure shared profile, then start web session |
| POST | `/api/auth/login` | Public + Supabase token | Validate token and start web session |
| POST | `/api/auth/logout` | Session | Clear web session |
| GET | `/api/auth/session` | Public | Return current web session state |
| GET | `/api/dashboard` | Member | Dashboard totals and recent activity |
| GET, POST | `/api/pickups` | Member | Read and schedule pickups |
| POST | `/api/scan/classify` | Member | Classify and save a waste scan |
| GET | `/api/rewards` | Member | Reward catalogue and redemptions |
| POST | `/api/rewards/redemptions` | Member | Redeem a reward atomically |
| GET | `/api/notifications` | Member | List current member notifications |
| GET | `/api/waste/guidance` | Public | List guidance, optionally by category |
| GET, PUT | `/api/profile` | Member | Read or update the signed-in profile |
| POST | `/api/contact` | Public | Submit a contact message |
| GET | `/api/admin/stats`, `/overview`, `/activity`, `/reports` | Admin | Admin summary, activity, CSV report |
| GET, PUT | `/api/admin/users` | Admin | Search users and update account/role/active state |
| GET, PUT | `/api/admin/pickups` | Admin | Filter and update assignments/status |
| GET, POST, PUT | `/api/admin/rewards` | Admin | Manage reward catalogue |
| GET, PUT | `/api/admin/redemptions` | Admin | Review and update redemptions |

The mobile app continues to use its existing function routes under `/functions/v1/reloop-api` as configured in Android `app/build.gradle.kts`. These routes use the same Supabase Auth project and normalized tables. Do not put the web app's cookie in the Android client.

## Administrator setup

Create the administrator as an ordinary Supabase Auth account, then authorize the role through Supabase's SQL editor or an existing project-owner workflow:

```sql
update public.profiles
set role = 'admin', updated_at = now()
where email = 'admin@example.com';
```

The web app reads the live profile role on sign-in/session validation. The mobile Edge Function's `is_staff()` policy also reads this role. Do not grant admin based on client metadata or allow a member-facing profile update to change `role`.

## Deployments

### Supabase database

`.github/workflows/deploy-supabase.yml` applies checked-in migrations when migrations change on `all-updates`, or when manually started. Configure these GitHub repository Actions secrets:

- `SUPABASE_ACCESS_TOKEN`: Supabase account access token.
- `SUPABASE_DB_PASSWORD`: database password for this project.
- `SUPABASE_PROJECT_ID`: `pjsrpajqslyamavbvyxz`.

The workflow does not provision a Supabase project, change Auth providers, import Firebase data, or delete records. Review SQL diffs before merging.

### ASP.NET web/API

`.github/workflows/deploy-cloud-run.yml` deploys the container after CI. It needs the existing Google Cloud Workload Identity configuration as repository variables, plus:

- Variable `SUPABASE_URL` and `SUPABASE_ANON_KEY`.
- Create a Google Cloud Secret Manager secret named `SUPABASE_CONNECTION_STRING` containing the Postgres connection string, then grant the Cloud Run runtime service account `roles/secretmanager.secretAccessor`.

The Cloud Run runtime identity only needs to serve the application; database access is provided by the connection string. Keep the connection string secret and rotate it if exposed.

## Firebase cutover checklist

Changing code does not move or delete Firebase data. Before production cutover:

1. Back up Firestore and export Firebase Auth users.
2. Apply the shared Supabase schema and import profiles/data using the importer documented in the Android repository's `backend/README.md`.
3. Auth users need a Supabase account and password reset; Firebase password hashes are not directly reusable by Supabase Auth.
4. Verify table counts, user/profile UUID mapping, pickup histories, points balances, reward transactions, guidance, and notifications.
5. Assign the first administrator using the SQL above, then verify every admin page and mobile member flow.
6. Change production traffic only after verification. Keep Firebase available until rollback is no longer required.

## API change notes

Shared table definitions originate in the mobile Supabase migrations. Keep the initial schema and seed migration versions synchronized between the mobile and web repositories. Add new shared columns in a forward-only migration and coordinate the Kotlin models/Edge Function before releasing. Keep admin role checks in server/API authorization and Postgres RLS, not in UI visibility alone.

