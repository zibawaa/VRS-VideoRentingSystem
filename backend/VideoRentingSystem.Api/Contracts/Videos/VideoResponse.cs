using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Contracts.Videos;

public sealed class VideoResponse
{
    public required int VideoId { get; init; }
    public required string Title { get; init; }
    public required string Genre { get; init; }
    public required int ReleaseYear { get; init; }
    public required bool IsRented { get; init; }
    public required int OwnerPublisherId { get; init; }
    public required string Type { get; init; }
    public required decimal RentalPrice { get; init; }
    public required int RentalHours { get; init; }
    public required bool IsPublished { get; init; }

    public static VideoResponse FromVideo(Video v)
    {
        return new VideoResponse
        {
            VideoId = v.VideoId,
            Title = v.Title,
            Genre = v.Genre,
            ReleaseYear = v.ReleaseYear,
            IsRented = v.IsRented,
            OwnerPublisherId = v.OwnerPublisherId,
            Type = v.Type.ToString(),
            RentalPrice = v.RentalPrice,
            RentalHours = v.RentalHours,
            IsPublished = v.IsPublished
        };
    }
}
