# Design

The implemented solution uses a layered design so the data structure logic is independent from UI and database code:

- `VideoRentingSystem.Core/Models/Video.cs`: domain object for one video record.
- `VideoRentingSystem.Core/DataStructures/AvlTitleIndex.cs`: custom AVL tree keyed by normalized title for efficient title search and ordered display.
- `VideoRentingSystem.Core/DataStructures/IdHashIndex.cs`: custom hash table keyed by video ID for fast direct lookup.
- `VideoRentingSystem.Core/Core/VideoStore.cs`: orchestrates operations and keeps both custom indexes consistent.
- `VideoRentingSystem.Core/Data/SqliteVideoRepository.cs`: SQL read/write integration using a local SQLite file.
- `VideoRentingSystem.App/MainForm.cs`: WinForms prototype implementing all required user operations.

No built-in generic collections (`List`, `Dictionary`) are used for core storage/indexing.

# Data Structure Justification

## Recommended Structure

Hybrid indexing with:

1. AVL Tree (key = normalized video title)
2. Custom Hash Table (key = video ID)

## Why this is suitable for CST2550 requirements

- **Search by title** is efficient with AVL: `O(log n)` average/worst.
- **Search by ID** is efficient with hash table: `O(1)` average.
- **Display all videos** in alphabetical order is natural with AVL in-order traversal: `O(n)`.
- **Rent/Return** is fast after ID lookup via hash table.
- **Academic quality**: demonstrates understanding of algorithmic trade-offs and custom implementation from first principles.

## Why not only one structure

- AVL alone gives `O(log n)` for ID operations unless ID is also a tree key.
- Hash table alone does not provide naturally ordered output for display-all and title ordering.
- Combining both satisfies all functionality and supports stronger complexity analysis in the report.

# Pseudocode

## AddVideo(video)

```text
FUNCTION AddVideo(video):
    IF idHashIndex.Contains(video.VideoId):
        RETURN false

    insertedId <- idHashIndex.Add(video)
    insertedTitle <- avlTitleIndex.Add(video)

    IF insertedId = false OR insertedTitle = false:
        IF insertedId = true:
            idHashIndex.Remove(video.VideoId)
        RETURN false

    repository.UpsertVideo(video)   // if repository configured
    RETURN true
```

## RemoveVideo(videoId)

```text
FUNCTION RemoveVideo(videoId):
    video <- idHashIndex.Find(videoId)
    IF video IS NULL:
        RETURN false

    removedId <- idHashIndex.Remove(videoId)
    removedTitle <- avlTitleIndex.Remove(video.Title, video.VideoId)

    IF removedId AND removedTitle:
        repository.DeleteVideo(videoId)   // if repository configured
        RETURN true

    RETURN false
```

## SearchByTitle(title)

```text
FUNCTION SearchByTitle(title):
    key <- Normalize(title)
    node <- avlTitleIndex.FindNode(key)
    IF node IS NULL:
        RETURN empty array
    RETURN node.VideoChainAsArray()
```

## SearchById(videoId)

```text
FUNCTION SearchById(videoId):
    RETURN idHashIndex.Find(videoId)
```

## RentVideo(videoId)

```text
FUNCTION RentVideo(videoId):
    video <- idHashIndex.Find(videoId)
    IF video IS NULL OR video.IsRented = true:
        RETURN false

    video.IsRented <- true
    repository.UpsertVideo(video)   // if repository configured
    RETURN true
```

## ReturnVideo(videoId)

```text
FUNCTION ReturnVideo(videoId):
    video <- idHashIndex.Find(videoId)
    IF video IS NULL OR video.IsRented = false:
        RETURN false

    video.IsRented <- false
    repository.UpsertVideo(video)   // if repository configured
    RETURN true
```

## AVL Insert and Rebalance (core idea)

```text
FUNCTION AvlInsert(node, key, video):
    IF node IS NULL:
        RETURN NewNode(key, video)

    IF key < node.key:
        node.left <- AvlInsert(node.left, key, video)
    ELSE IF key > node.key:
        node.right <- AvlInsert(node.right, key, video)
    ELSE:
        node.chain <- AddToChain(node.chain, video)   // duplicate title
        RETURN node

    UpdateHeight(node)
    balance <- Height(node.left) - Height(node.right)

    IF balance > 1 AND key < node.left.key:
        RETURN RotateRight(node)
    IF balance < -1 AND key > node.right.key:
        RETURN RotateLeft(node)
    IF balance > 1 AND key > node.left.key:
        node.left <- RotateLeft(node.left)
        RETURN RotateRight(node)
    IF balance < -1 AND key < node.right.key:
        node.right <- RotateRight(node.right)
        RETURN RotateLeft(node)

    RETURN node
```

## Hash Add (separate chaining)

```text
FUNCTION HashAdd(video):
    IF loadFactorExceedsThreshold:
        ResizeAndRehash()

    bucket <- Hash(video.VideoId) MOD bucketCount
    current <- buckets[bucket]

    WHILE current IS NOT NULL:
        IF current.key = video.VideoId:
            RETURN false
        current <- current.next

    buckets[bucket] <- NewEntry(video.VideoId, video, buckets[bucket])
    RETURN true
```

# Implementation

Implemented code files:

- `VideoRentingSystem.Core/Models/Video.cs`
- `VideoRentingSystem.Core/DataStructures/AvlTitleIndex.cs`
- `VideoRentingSystem.Core/DataStructures/IdHashIndex.cs`
- `VideoRentingSystem.Core/Core/VideoStore.cs`
- `VideoRentingSystem.Core/Data/IVideoRepository.cs`
- `VideoRentingSystem.Core/Data/SqliteVideoRepository.cs`
- `VideoRentingSystem.App/MainForm.cs`
- `VideoRentingSystem.App/Data/schema_sqlite.sql`
- `VideoRentingSystem.Tests/Test1.cs`

The WinForms prototype supports all required operations:

- Store video records
- Add video
- Remove video
- Search by title
- Search by ID
- Display all videos
- Rent a video
- Return a video

# Complexity Analysis

Let `n` be number of videos, `h` AVL height (`h = O(log n)` for AVL), and `alpha` hash load factor.

- **Add video**
  - Hash insert: `O(1)` average, `O(n)` worst (collision chain / resize).
  - AVL insert: `O(log n)` average and worst.
  - Combined dominant: `O(log n)` typical (plus occasional hash resize cost).

- **Remove video**
  - Hash lookup + remove: `O(1)` average, `O(n)` worst.
  - AVL remove/rebalance: `O(log n)` average and worst.
  - Combined dominant: `O(log n)` typical.

- **Search by ID**
  - Hash lookup: `O(1)` average, `O(n)` worst.

- **Search by title**
  - AVL lookup: `O(log n)` average and worst.
  - Returning duplicates with same title: `+ O(k)` where `k` matches for that title.

- **Display all videos**
  - AVL in-order traversal: `O(n)`.

- **Rent / Return**
  - Hash ID lookup + state update + SQL upsert.
  - In-memory: `O(1)` average.
  - Database step depends on SQL engine/indexing; logically one row update.

Space:

- AVL index: `O(n)`
- Hash index: `O(n)`
- Total in-memory indexing: `O(n)`

# Testing Approach

Framework: `MSTest`.

The tests verify:

1. Add and retrieve by both ID and title.
2. Duplicate ID rejection.
3. Ordered output for display-all.
4. Rent/return state transitions and invalid repeated operations.
5. Consistent delete behavior across both indexes.
6. Repository integration behavior using a fake repository.

Test file:

- `VideoRentingSystem.Tests/Test1.cs`

Executed result:

- `Passed: 6, Failed: 0`.

# Prototype Integration

## SQL Integration

- Uses local SQLite database file (editable in UI path field).
- Default location:
  - `%LOCALAPPDATA%\\VideoRentingSystem\\videos.db`
- Repository auto-creates database file and table through `EnsureDatabaseAndSchema()`.
- SQLite SQL script file included:
  - `VideoRentingSystem.App/Data/schema_sqlite.sql`

## UI Integration (WinForms)

`MainForm` provides:

- Connection setup
- Add/remove workflow
- Search by title and ID
- Rent/return buttons
- Display-all list
- Seed sample data button

All UI actions call `VideoStore`, so implementation stays aligned with design.

# Alignment To Learning Outcomes And Marking Criteria

- **Apply and evaluate data structures/algorithms (LO1, LO2)**:
  custom AVL + custom hash with Big-O analysis and trade-offs.
- **Design and develop software using suitable algorithms (Skill LO1)**:
  data structure selected to match required operations.
- **Automated testing (Skill LO3)**:
  MSTest suite covering core behaviors and edge cases.
- **Implementation matches design (Marking: 15)**:
  same operations modeled in pseudocode and implemented in `VideoStore`.
- **SQL Database code file (Marking: 5)**:
  dedicated SQL script plus repository integration.
- **Implementation meets requirements (Marking: 15)**:
  all required user features present in prototype.
