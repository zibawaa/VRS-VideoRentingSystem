namespace VideoRentingSystem.Core.Models;

public enum VideoType
{
    Movie = 0,
    Series = 1
}

public sealed class Video
{
    public int VideoId { get; }
    public string Title { get; private set; }
    public string Genre { get; private set; }
    public int ReleaseYear { get; private set; }
    public bool IsRented { get; private set; }
    public int OwnerPublisherId { get; private set; }
    public VideoType Type { get; private set; }
    public decimal RentalPrice { get; private set; }
    public int RentalHours { get; private set; }
    public bool IsPublished { get; private set; }

    public Video(
        int videoId,
        string title,
        string genre,
        int releaseYear,
        bool isRented = false,
        int ownerPublisherId = 0,
        VideoType type = VideoType.Movie,
        decimal rentalPrice = 2.99m,
        int rentalHours = 48,
        bool isPublished = true)
    {
        if (videoId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(videoId), "Video ID must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(genre))
        {
            throw new ArgumentException("Genre is required.", nameof(genre));
        }
        // reject bad id and empty strings before validating numeric ranges

        if (releaseYear < 1888 || releaseYear > DateTime.UtcNow.Year + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(releaseYear), "Release year is outside valid movie range.");
            // earliest film era through next year to catch typos without blocking imminent releases
        }

        if (ownerPublisherId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerPublisherId), "Owner publisher ID cannot be negative.");
        }

        if (rentalPrice < 0.49m || rentalPrice > 999.99m)
        {
            throw new ArgumentOutOfRangeException(nameof(rentalPrice), "Rental price must be between 0.49 and 999.99.");
        }

        if (rentalHours < 1 || rentalHours > 24 * 90)
        {
            throw new ArgumentOutOfRangeException(nameof(rentalHours), "Rental hours must be between 1 and 2160.");
        }
        // marketplace fields validate ownership, pricing, and rent-window boundaries

        VideoId = videoId;
        Title = title.Trim();
        Genre = genre.Trim();
        ReleaseYear = releaseYear;
        IsRented = isRented;
        OwnerPublisherId = ownerPublisherId;
        Type = type;
        RentalPrice = decimal.Round(rentalPrice, 2, MidpointRounding.AwayFromZero);
        RentalHours = rentalHours;
        IsPublished = isPublished;
        // assign after validation so objects are always normalised
    }

    public void SetRented(bool rented)
    {
        IsRented = rented;
    }

    public void UpdateDetails(
        string title,
        string genre,
        int releaseYear,
        int ownerPublisherId,
        VideoType type,
        decimal rentalPrice,
        int rentalHours,
        bool isPublished)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Title is required.", nameof(title));
        }

        if (string.IsNullOrWhiteSpace(genre))
        {
            throw new ArgumentException("Genre is required.", nameof(genre));
        }

        if (releaseYear < 1888 || releaseYear > DateTime.UtcNow.Year + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(releaseYear), "Release year is outside valid movie range.");
        }

        if (ownerPublisherId < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(ownerPublisherId), "Owner publisher ID cannot be negative.");
        }

        if (rentalPrice < 0.49m || rentalPrice > 999.99m)
        {
            throw new ArgumentOutOfRangeException(nameof(rentalPrice), "Rental price must be between 0.49 and 999.99.");
        }

        if (rentalHours < 1 || rentalHours > 24 * 90)
        {
            throw new ArgumentOutOfRangeException(nameof(rentalHours), "Rental hours must be between 1 and 2160.");
        }

        Title = title.Trim();
        Genre = genre.Trim();
        ReleaseYear = releaseYear;
        OwnerPublisherId = ownerPublisherId;
        Type = type;
        RentalPrice = decimal.Round(rentalPrice, 2, MidpointRounding.AwayFromZero);
        RentalHours = rentalHours;
        IsPublished = isPublished;
        // same rules as ctor but id and rental flag stay fixed
    }
}
