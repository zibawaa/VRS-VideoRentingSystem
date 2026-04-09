# Video Rental Store Management System (CST2550 prototype)

WinForms desktop app (**C# / .NET Framework 4.8**) for the group coursework: video rental rows live in a **SQL Server LocalDB** `.mdf` file (path chosen at runtime — **no hardcoded database path**), and the app keeps a **custom singly linked list** in memory for searching (no `List<T>` / `Dictionary` inside that structure).

This lines up with the brief: custom data structure + complexity comments, SQL script in repo, readme, MSTest unit tests, and **no third-party NuGet packages in the WinForms app** (only built-in `System.Data.SqlClient`). The test project still uses MSTest packages from NuGet, which is normal for coursework test harnesses.

## Prerequisites

- Windows
- **.NET Framework 4.8 Developer Pack** or Visual Studio with .NET desktop development workload
- [.NET SDK](https://dotnet.microsoft.com/download) (8.x is fine) if you want `dotnet build` / `dotnet test` from the command line
- **SQL Server Express LocalDB** (often installed with Visual Studio). If Connect fails, install [SQL Server Express](https://www.microsoft.com/sql-server/sql-server-downloads) and include LocalDB.

## Folder layout

```
VideoRentalSystem/
  VideoRentalSystem/          WinForms project (net48, no app NuGet)
  VideoRentalSystem.Tests/    MSTest project
  schema.sql                  T-SQL CREATE + sample INSERTs
  README.md
```

## Build (command line)

```bash
cd path\to\VideoRentalSystem
dotnet build VideoRentalSystem.sln -c Release
```

Or open `VideoRentalSystem.sln` in Visual Studio and build (F6).

## Run the app

```bash
dotnet run --project VideoRentalSystem/VideoRentalSystem.csproj -c Release
```

Or set the WinForms project as startup and press F5.

### Database file (runtime)

1. Click **Browse...** and choose a **full path** ending in `.mdf` (a new filename is OK — the app will try to create the files on first use).
2. Click **Connect**. The app attaches the database with `(localdb)\MSSQLLocalDB` and creates `dbo.VideoRentals` if needed.
3. Optional: open the same `.mdf` in SSMS, run `schema.sql` to wipe/reload the 12 sample rows, then use **Show all videos** in the app.

## Run unit tests

```bash
dotnet test VideoRentalSystem.sln -c Release
```

Tests exercise **`RentalLinkedList`** (add, remove, search, empty list, duplicate ID).

## Extra feature (optional marking)

The form includes a small animated **“rental helper”** figure with moving arms (`RentalAgentPanel.cs`) as a light-hearted nod to the “AI agent with arms” extra on the rubric.

## Report / academic notes

- Put **pseudo-code** and **complexity discussion** in the PDF report (not in code), as the brief asks.
- Add any extra references you use in **Harvard** style in comments (a couple of Microsoft Learn links are already stubbed in `RentalDatabase.cs` and `RentalAgentPanel.cs` as examples).

## Tester role

Use this prototype to tie your **test plan** (table of cases) to the MSTest methods, and mention **integration** tests you ran manually (e.g. Connect, seed `schema.sql`, rent/return) in the report.
