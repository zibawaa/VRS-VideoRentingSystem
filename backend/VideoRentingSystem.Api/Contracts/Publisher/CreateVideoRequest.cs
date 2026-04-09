namespace VideoRentingSystem.Api.Contracts.Publisher;

public sealed class CreateVideoRequest
{
    public int VideoId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Genre { get; set; } = string.Empty;
    public int ReleaseYear { get; set; }
    public string Type { get; set; } = "Movie";
    public decimal RentalPrice { get; set; }
    public int RentalHours { get; set; }
    public bool IsPublished { get; set; } = true;
    public int? OwnerPublisherId { get; set; }
}
