# ReLoop SQL Connection Setup

Keep database credentials out of `appsettings*.json`. For local development, store the connection string with ASP.NET Core User Secrets:

```powershell
dotnet user-secrets init
dotnet user-secrets set "ConnectionStrings:ReLoopDatabase" "<your-connection-string>"
```

For production, inject `ConnectionStrings__ReLoopDatabase` from the hosting platform's secret store. The application fails fast outside the test environment if the setting is missing. Do not commit database passwords or put them in checked-in launch profiles.

## Create Or Update The Database

From the project folder, run:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update
```

Migrations create the user, pickup, scan, activity, reward catalogue/redemption, reward ledger, and contact-message tables.
