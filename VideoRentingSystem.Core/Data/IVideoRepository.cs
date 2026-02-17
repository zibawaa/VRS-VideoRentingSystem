using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public interface IVideoRepository
{
    Video[] LoadAllVideos();
    void UpsertVideo(Video video);
    void DeleteVideo(int videoId);
}
