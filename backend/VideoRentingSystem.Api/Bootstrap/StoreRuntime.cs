using VideoRentingSystem.Core.Core;

namespace VideoRentingSystem.Api.Bootstrap;

/// <summary>
/// Singleton container registered in DI that holds the shared VideoStore,
/// UserStore, and a SyncRoot for thread-safe access to the custom data structures.
/// </summary>
public sealed class StoreRuntime
{
    public StoreRuntime(VideoStore videoStore, UserStore userStore, string databasePath)
    {
        VideoStore = videoStore;
        UserStore = userStore;
        DatabasePath = databasePath;
    }

    public VideoStore VideoStore { get; }
    public UserStore UserStore { get; }
    public string DatabasePath { get; }

    // custom structures (AVL, hash, BST) are mutable and non-thread-safe,
    // so controllers must lock on SyncRoot before reading or writing
    public object SyncRoot { get; } = new();
}
