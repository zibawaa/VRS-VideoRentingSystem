// One active checkout row: who, what, and when it started. Return handling deletes the row today instead of keeping history,
// which keeps the coursework schema small (we can extend later if the brief asks for full logs).
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
    }
}
