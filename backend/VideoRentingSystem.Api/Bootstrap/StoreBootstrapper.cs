using VideoRentingSystem.Core.Core;
using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Bootstrap;

/// <summary>
/// Creates and initialises the singleton stores (VideoStore, UserStore) that
/// back every API request. Run once at startup before the pipeline begins.
/// </summary>
public static class StoreBootstrapper
{
    public static StoreRuntime Initialize()
    {
        // keep the same per-user SQLite location so WinForms and API share data during migration
        string databasePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "VideoRentingSystem",
            "videos.db");

        // create the three repository implementations backed by the shared database file
        SqliteVideoRepository videoRepository = new(databasePath);
        SqliteRentalRepository rentalRepository = new(databasePath);
        SqliteUserRepository userRepository = new(databasePath);

        // ensure all tables and columns exist before loading data
        videoRepository.EnsureDatabaseAndSchema();

        // construct stores and hydrate in-memory indexes from persisted rows
        UserStore userStore = new(userRepository);
        VideoStore videoStore = new(videoRepository, rentalRepository);
        userStore.LoadFromRepository();
        videoStore.LoadFromRepository();

        // seed deterministic demo accounts so the API is usable on first run
        userStore.RegisterUser("admin", "admin", UserRole.Admin, "VRS HQ");
        userStore.RegisterUser("123", "123", UserRole.Admin, "VRS Demo Studio");
        userStore.RegisterUser("publisher1", "publisher1", UserRole.Publisher, "Indie Studio One");
        userStore.RegisterUser("customer1", "customer1", UserRole.Customer);

        // populate the catalogue with sample titles when running against a fresh database
        if (videoStore.Count == 0)
        {
            SeedDefaultVideos(videoStore);
        }

        return new StoreRuntime(videoStore, userStore, databasePath);
    }

    // loads a handful of well-known titles so the browse page has content immediately
    private static void SeedDefaultVideos(VideoStore videoStore)
    {
        videoStore.AddVideo(new Video(1001, "Interstellar", "Sci-Fi", 2014, false, 0, VideoType.Movie, 3.99m, 72, true));
        videoStore.AddVideo(new Video(1002, "The Dark Knight", "Action", 2008, false, 0, VideoType.Movie, 3.49m, 72, true));
        videoStore.AddVideo(new Video(1003, "Inception", "Sci-Fi", 2010, false, 0, VideoType.Movie, 3.29m, 48, true));
        videoStore.AddVideo(new Video(1004, "Pulp Fiction", "Crime", 1994, false, 0, VideoType.Movie, 2.79m, 48, true));
        videoStore.AddVideo(new Video(1005, "Our Planet", "Documentary", 2019, false, 0, VideoType.Series, 4.49m, 96, true));
    }
}
