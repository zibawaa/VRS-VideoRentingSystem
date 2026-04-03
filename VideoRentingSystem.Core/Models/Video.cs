// Plain data holder for one tape/stream entry. Properties use private set so only methods like UpdateDetails can mutate state,
// which makes it obvious where business rules (validation) live when we read the file later.
namespace VideoRentingSystem.Core.Models;

public sealed class Video
{
    public int VideoId { get; }
    public string Title { get; private set; }
    public string Genre { get; private set; }
    public int ReleaseYear { get; private set; }
    public bool IsRented { get; private set; }

    public Video(int videoId, string title, string genre, int releaseYear, bool isRented = false)
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

        if (releaseYear < 1888 || releaseYear > DateTime.UtcNow.Year + 1)
        {
            throw new ArgumentOutOfRangeException(nameof(releaseYear), "Release year is outside valid movie range.");
        }

        VideoId = videoId;
        Title = title.Trim();
        Genre = genre.Trim();
        ReleaseYear = releaseYear;
        IsRented = isRented;
    }

    public void SetRented(bool rented)
    {
        IsRented = rented;
    }

    public void UpdateDetails(string title, string genre, int releaseYear)
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

        Title = title.Trim();
        Genre = genre.Trim();
        ReleaseYear = releaseYear;
    }
}
