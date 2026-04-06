using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public interface IVideoRepository
{
    Video[] LoadAllVideos();
    void UpsertVideo(Video video);
    void DeleteVideo(int videoId);
    // optional persistence behind VideoStore: full read, upsert row, delete by id
}
