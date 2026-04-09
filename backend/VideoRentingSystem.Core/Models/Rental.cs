namespace VideoRentingSystem.Core.Models;

public sealed class Rental
{
    public int UserId { get; }
    public int VideoId { get; }
    public DateTime RentDate { get; }
    public DateTime ExpiryUtc { get; }
    public decimal PaidAmount { get; }

    public Rental(int userId, int videoId, DateTime rentDate, DateTime expiryUtc, decimal paidAmount)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be greater than 0.");
        }

        if (videoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(videoId), "Video ID must be greater than 0.");
        }

        if (expiryUtc < rentDate)
        {
            throw new ArgumentOutOfRangeException(nameof(expiryUtc), "Expiry cannot be earlier than rent date.");
        }

        if (paidAmount < 0m)
        {
            throw new ArgumentOutOfRangeException(nameof(paidAmount), "Paid amount cannot be negative.");
        }

        UserId = userId;
        VideoId = videoId;
        RentDate = rentDate;
        ExpiryUtc = expiryUtc;
        PaidAmount = decimal.Round(paidAmount, 2, MidpointRounding.AwayFromZero);
        // composite key (userId,videoId) lives in sqlite; model is immutable once created
    }
}
