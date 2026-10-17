# ReLoop Technologies Web App

ReLoop is a recycling management web application built with ASP.NET Core Razor Pages. It supports Firebase Authentication, waste scan classification, pickup scheduling, pickup history, rewards, admin management, and Firestore-backed persistence shared with the mobile app.

## Features

- Landing page based on the ReLoop visual design.
- Login and sign-up pages with client-side and API validation.
- User dashboard with impact metrics, activity, quick actions, and upcoming pickups.
- Waste scan upload flow with classification result details.
- Doorstep pickup scheduling form with category selection and validation.
- Pickup ledger with status filtering.
- Admin command console with KPIs, waste distribution, and verification rows.
- REST API endpoints for authentication, member profiles, pickups, rewards/redemptions, notifications, waste guidance, contact, and administrator operations/reports.
- Firebase Authentication (Email/Password) with server-verified Firebase ID tokens and an HTTP-only application session cookie.
- Firestore persistence through the Google Cloud .NET client and Application Default Credentials (no service-account key committed to source).
- xUnit integration tests and GitHub Actions CI workflow.

## Tech Stack

- ASP.NET Core Razor Pages
- .NET 8
- Firebase Authentication
- Cloud Firestore
- Bootstrap static assets
- xUnit, ASP.NET Core TestHost, in-memory document store for tests

## Branches

- `issue-1-ui-design-system`: global UI design system foundation.
- `issue-2-landing-page`: ReLoop landing/home page.
- `issue-3-9`: authentication, dashboard, scan, pickup, admin, and API work.
- `issue-10-azure-sql-ef-core`: EF Core SQL Server/Azure SQL persistence.
- `issue-11-e2e-testing-qa`: automated tests, QA checklist, and CI workflow.
- `complete-reloop-app`: combined final branch with all implemented pages, APIs, SQL persistence, tests, README, and wireframes.

## Run Locally

```powershell
dotnet restore
dotnet run
```

The project launch settings use:

```text
http://localhost:5197
https://localhost:7036
```

If that port is already being used, run:

```powershell
dotnet run --urls http://localhost:5299
```

## Firebase Configuration

The shared Firebase project ID is `relooptechnologies-2eda2`; the app uses the `(default)` Firestore database. The browser Firebase web-app configuration is public client configuration, while server credentials must use Application Default Credentials or the hosting environment's workload identity. Never commit a service-account JSON key.

```text
dotnet user-secrets init
dotnet user-secrets set "Firebase:WebApiKey" "<Firebase web API key>"
dotnet user-secrets set "Firebase:WebAppId" "<Firebase web app ID>"
dotnet user-secrets set "Firebase:AuthDomain" "<Firebase auth domain>"
```

The Firebase web configuration can also be provided with `Firebase__WebApiKey`, `Firebase__WebAppId`, and `Firebase__AuthDomain` environment variables. The .NET server uses Application Default Credentials; on a local development machine, authenticate with the Google Cloud CLI for the selected project. In production, use the host's workload identity/service account with least-privilege Firestore access.

The app does not create Firebase resources. Firebase Authentication, the Firestore database, and IAM access must be configured in the Firebase/Google Cloud project. Firebase web API key and app ID are not server secrets, but must correspond to this project.

The data model is documented in `Data/FirestoreDocuments.cs` and matches the shared ERD. Firestore security rules are in [firestore.rules](firestore.rules); composite indexes are in [firestore.indexes.json](firestore.indexes.json). The server uses Admin SDK/service credentials and must enforce authorization in the application; these client rules do not restrict trusted server credentials.

## Shared Firestore Data Model

The ERD below is the Firestore collection model shared by the website and its mobile counterpart.

![ReLoop Technologies Firestore ERD](docs/wireframes/reloop-firestore-erd.png)

## Testing

Run all tests:

```powershell
dotnet test
```

The test project uses an in-memory Firestore document-store substitute and fake verified identities, so CI does not contact Firebase.

The manual QA checklist is in [QA-CHECKLIST.md](QA-CHECKLIST.md).

## Wireframes

The images below are the supplied wireframes and brand references used for the implementation.

### Landing Page

![Landing page wireframe](docs/wireframes/01-landing.jpeg)

### Login Page

![Login page wireframe](docs/wireframes/02-login.jpeg)

### Admin Dashboard

![Admin dashboard wireframe](docs/wireframes/03-admin-dashboard.jpeg)

### Scan Upload Page

![Scan upload wireframe](docs/wireframes/04-scan-upload.jpeg)

### Scan Result Page

![Scan result wireframe](docs/wireframes/05-scan-result.jpeg)

### Schedule Pickup Page

![Schedule pickup wireframe](docs/wireframes/06-schedule-pickup.jpeg)

### Pickup Ledger Page

![Pickup ledger wireframe](docs/wireframes/07-pickup-ledger.jpeg)

### App Logo

![App logo reference](docs/wireframes/08-app-logo.jpeg)

### Brand Logo

![Brand logo reference](docs/wireframes/09-brand-logo.jpeg)

## Notes

- Firebase Auth handles passwords. The server verifies Firebase ID tokens and authorizes the secure application session against the current Firestore user role and active state.
- Scan classification is a deterministic demo classifier based on filename hints until an AI/image model is connected.
- Scan classification and points are recorded in Firestore; production image recognition still requires a connected model.
