# Video Renting System (CST2550)

## Projects

- `VideoRentingSystem.App` - WinForms prototype
- `VideoRentingSystem.Core` - domain, custom data structures, and SQL repository
- `VideoRentingSystem.Tests` - MSTest unit tests

## Build

```bash
dotnet build VideoRentingSystem.slnx
```

## Run WinForms app

```bash
dotnet run --project VideoRentingSystem.App
```

## Run tests

```bash
dotnet test VideoRentingSystem.Tests
```

## SQL Notes (Local File Database)

- App now defaults to a local SQLite file at:
  - `%LOCALAPPDATA%\VideoRentingSystem\videos.db`
- In the UI, set/confirm the file path and click `Open Local DB`.
- Database file and table are auto-created when opened.
- SQL schema scripts:
  - SQL Server version: `VideoRentingSystem.App/Data/schema.sql`
  - SQLite version: `VideoRentingSystem.App/Data/schema_sqlite.sql`
