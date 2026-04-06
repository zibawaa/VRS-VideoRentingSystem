namespace VideoRentingSystem.Core.Models;

public sealed class Rental
{
    public int UserId { get; }
    public int VideoId { get; }
    public DateTime RentDate { get; }

    public Rental(int userId, int videoId, DateTime rentDate)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be greater than 0.");
        }

        if (videoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(videoId), "Video ID must be greater than 0.");
        }

        UserId = userId;
        VideoId = videoId;
        RentDate = rentDate;
        // composite key (userId,videoId) lives in sqlite; model is immutable once created
    }
}
