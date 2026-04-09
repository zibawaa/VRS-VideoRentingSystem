# Video rental coursework (CST2550)

Small **WinForms** app in **C#** (**.NET Framework 4.8**) that manages video rental records using a custom linked list data structure.

## What it does

- Lets you load a database file at runtime by typing the path or using the browse button — no hardcoded paths, the brief was clear about that.
- Uses **SQL Server LocalDB** with a **`.mdf`** file to store records.
- Keeps everything in memory using a hand-built singly linked list (`RentalLinkedList.cs`) — no `List<T>` or `Dictionary`, which was one of the rules.
- The linked list methods have **time complexity comments** on them as required.
- There's also a small animated character on the form with moving arms — the marking scheme mentioned an optional "AI agent with arms" for extra marks so I gave it a go.

## What you need

- **Windows**
- **.NET Framework 4.8** (or Visual Studio with the desktop workload)
- **.NET SDK 8.x** if you want to build from terminal
- **LocalDB** — if the Connect button crashes, this is probably why. Install it through Visual Studio (tick the data storage workload) or grab SQL Server Express and make sure LocalDB is included. This took me a while to sort out so don't skip it.

## Folder structure

```
VideoRentalSystem/
  VideoRentalSystem/        ← WinForms app
  VideoRentalSystem.Tests/  ← MSTest unit tests
  schema.sql                ← T-SQL for LocalDB: creates table + 12 sample rows
  README.md
```

## How to build

Open the `.sln` in Visual Studio and build normally, or from terminal:

```bash
cd path\to\VideoRentalSystem
dotnet build VideoRentalSystem.sln -c Release
```

## How to run

```bash
dotnet run --project VideoRentalSystem/VideoRentalSystem.csproj -c Release
```

Or just hit **F5** in Visual Studio with the WinForms project set as startup.

## Connecting the database

- Type a path or browse to a **`.mdf`** file — it doesn't need to exist yet.
- Hit **Connect** — it'll create the file and the `VideoRentals` table automatically.
- If you want the sample data, run **`schema.sql`** against the `.mdf` in **SSMS** first, then click **Show all videos**.

## Running the tests

```bash
dotnet test VideoRentalSystem.sln -c Release
```

The tests cover the linked list — adding, removing, searching, empty list edge cases, duplicate IDs etc. I'm doing the tester role so there's more thorough manual testing documented in the report separately.

## Notes for the report

- Pseudocode and big-O analysis go in the **PDF** report, not in the code.
- I added a few Harvard-style reference comments in `RentalDatabase.cs` and `RentalAgentPanel.cs` as examples of the format.

If it doesn't connect first try, check LocalDB is actually installed — that's almost always the problem.
