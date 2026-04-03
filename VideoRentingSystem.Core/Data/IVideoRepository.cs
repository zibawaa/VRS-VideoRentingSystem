// Video persistence contract — VideoStore talks to this, never directly to Sqlite* types, so swapping storage is easier in tests.
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public interface IVideoRepository
{
    Video[] LoadAllVideos();
    void UpsertVideo(Video video);
    void DeleteVideo(int videoId);
}
