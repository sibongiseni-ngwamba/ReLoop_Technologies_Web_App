# ReLoop SQL Connection Setup

Issue 10 uses Entity Framework Core with SQL Server/Azure SQL.

## Where The Connection String Goes

Place your connection string in `appsettings.Development.json` while working locally:

```json
"ConnectionStrings": {
  "ReLoopDatabase": "Server=tcp:<your-server>.database.windows.net,1433;Initial Catalog=<your-database>;Persist Security Info=False;User ID=<your-user>;Password=<your-password>;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
}
```

For production, set the same key as an environment variable or cloud app setting:

```text
ConnectionStrings__ReLoopDatabase
```

Do not commit real Azure SQL passwords to Git.

## Create Or Update The Database

From the project folder, run:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update
```

The migration creates users, pickups, scan records, activity logs, and reward ledger tables with seed data for the demo screens.
