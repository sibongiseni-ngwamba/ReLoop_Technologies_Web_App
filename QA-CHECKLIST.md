# ReLoop QA Checklist

Use this checklist before submitting or demoing the project.

## Automated Checks

```powershell
dotnet restore
dotnet build
dotnet test
```

## Manual UI Checks

- Home page loads on desktop and mobile widths.
- Login shows Firebase feedback for invalid credentials and network failures.
- Sign up rejects mismatched passwords and redirects to dashboard after success.
- Dashboard metrics, activity, and upcoming pickups populate from `/api/dashboard`.
- Scan Waste requires an image before classification and shows a result card after upload.
- Schedule Pickup rejects incomplete forms and creates a scheduled pickup when valid.
- My Pickups tabs filter all, scheduled, completed, and cancelled collections.
- Admin console shows KPI cards, waste distribution, and recent pickup rows.

## API Checks

- `POST /api/auth/login` returns `400` for invalid DTOs and `200` for valid login.
- `POST /api/auth/signup` returns `201` for valid sign-up data.
- `GET /api/dashboard` returns user metrics and activity.
- `POST /api/pickups` creates a pickup and returns `201`.
- `GET /api/pickups?status=Scheduled` filters scheduled records.
- `POST /api/scan/classify` returns item, category, confidence, points, and weight.
- `GET /api/admin/stats` returns KPI metrics and waste distribution.

## Firebase Checks

- Confirm the Firebase web-app config is provided through local secrets/environment variables.
- Confirm the server is authenticated through Application Default Credentials and has least-privilege Firestore access.
- Confirm the `(default)` Firestore database exists in project `relooptechnologies-2eda2`.
- Deploy `firestore.rules` and `firestore.indexes.json` with the Firebase CLI after reviewing mobile-client access needs.
- Verify users, pickups, waste logs, rewards, redemptions, guidance, and notifications persist in the shared collections.

## Edge Cases

- Empty pickup ledger filter displays the empty state.
- Long addresses wrap cleanly in the pickup table.
- Upload filenames with `glass`, `paper`, or other names produce different scan categories.
- API validation errors return a consistent `ValidationProblem` response.
