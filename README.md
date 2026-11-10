# ReLoop Technologies

ReLoop is a recycling management web app and Android app backed by one Supabase project. The ASP.NET Core app provides the web portal and its REST API; the Android app currently calls the Supabase `reloop-api` Edge Function. Both use Supabase Auth and the same normalized Postgres tables so member activity and administrator changes are shared.

## Projects
## Web Diagram 

[![Architecture diagram of sibongiseni-ngwamba/reloop_technologies_web_app](https://gitdiagram.com/sibongiseni-ngwamba/reloop_technologies_web_app/diagram.png)](https://gitdiagram.com/sibongiseni-ngwamba/reloop_technologies_web_app?utm_source=readme&utm_medium=picture)

## Features

- **Web app and REST API:** ASP.NET Core Razor Pages, .NET 8. API routes are under `/api`; administrator routes are protected by the current profile role.
- **Mobile app:** Android/Kotlin at `C:/Users/ngwam_m1r7qi5/AndroidStudioProjects/ReLoop_Technologies_Mobile_App/`. It uses Supabase Auth and `/functions/v1/reloop-api`.
- **Database and identity:** Supabase project `pjsrpajqslyamavbvyxz` (the same project configured in the mobile app).

For API routes, setup, and deployment steps, see [backend/README.md](backend/README.md).

## Run the web app locally

Install the .NET 8 SDK. At the repository root, configure local Supabase settings and run the app:

```powershell
dotnet user-secrets set "Supabase:Url" "https://pjsrpajqslyamavbvyxz.supabase.co"
dotnet user-secrets set "Supabase:AnonKey" "<Supabase publishable key>"
dotnet user-secrets set "Supabase:ConnectionString" "<PostgreSQL connection string>"
dotnet run --project ReLoop_Technologies_Web_App.csproj
```

The app opens at `http://localhost:5197` or `https://localhost:7036`. Use a Supabase database connection string in **secret storage only**. The publishable key is intended for client use; never place a database password or secret/service-role key in browser or Android code.

## Admin access

The app reads roles from the shared `profiles` table. To grant the first administrator, sign up that person through Supabase Auth and update their `profiles.role` to `admin` using the Supabase SQL editor or an authorized admin process. Refresh their web session afterward. The API supports administrator management of users, pickups, rewards, redemptions, reports, and activity; see the backend README.

## Data and migrations

Versioned Supabase SQL migrations are in `supabase/migrations`. The initial schema and catalogue seed match the mobile app schema; the later migration adds web-only profile, pickup, scan, guidance, and contact fields. They are deployed by the Supabase workflow. Keep the web and Android schema copies aligned when changing shared tables.

The old Firebase project is not modified or deleted by this repository. Existing Firebase Auth users must be imported into Supabase Auth and reset their passwords as part of a planned migration. Review the Android repository's `backend/migration` instructions and back up the Firebase data before cutover. Do not switch production traffic until imported records, Auth accounts, RLS, and administrator access are verified.

## Deployment

- Supabase database migrations: `.github/workflows/deploy-supabase.yml`
- Web/API container: `.github/workflows/deploy-cloud-run.yml`

The workflows require repository Actions secrets and variables described in [backend/README.md](backend/README.md). They do not create a Supabase project or export/import Firebase data.

## Tests and QA

Run the existing .NET test suite with `dotnet test`. QA scenarios are in [QA-CHECKLIST.md](QA-CHECKLIST.md).
- Firebase Auth handles passwords. The server verifies Firebase ID tokens and authorizes the secure application session against the current Firestore user role and active state.
- Scan classification is a deterministic demo classifier based on filename hints until an AI/image model is connected.
- Scan classification and points are recorded in Firestore; production image recognition still requires a connected model.

---

## Team

Work is on GitHub under [sibongiseni-ngwamba/ReLoop_Technologies_Web_App](https://github.com/sibongiseni-ngwamba/ReLoop_Technologies_Web_App).

| GitHub | Name |
| --- | --- |
| [sibongiseni-ngwamba](https://github.com/sibongiseni-ngwamba) | Sibongiseni Ngwamaba |
| [Thokozani Masondo](https://github.com/Globoysosa) | Thokozani Masondo |
| [tirisano-trigga](https://github.com/tirisanomolaolwe-byte) | Tirisano Molaolwe |
| [Katleho Kwakwari](https://github.com/Katleho-1) | Katleho Kwakwari |
| [Katlego Makhuba](https://github.com/st10366193) | Katlego Makhuba |
