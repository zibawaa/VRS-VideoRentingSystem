namespace VideoRentingSystem.Api.Contracts.Rentals;

public sealed class RentalResponse
{
    public required int VideoId { get; init; }
    public required string Title { get; init; }
    public required string Genre { get; init; }
    public required DateTime RentDateUtc { get; init; }
    public required DateTime ExpiryUtc { get; init; }
    public required decimal PaidAmount { get; init; }
    public required bool IsRented { get; init; }
}
