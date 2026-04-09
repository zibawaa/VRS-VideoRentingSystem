namespace VideoRentingSystem.Api.Contracts.Admin;

/// <summary>DTO for a user row in the admin users table.</summary>
public sealed class AdminUserResponse
{
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required string Role { get; init; }
    public string? StudioName { get; init; }
    public AdminSessionInfo[] Sessions { get; init; } = [];
}

/// <summary>Lightweight session info exposed to the admin panel.</summary>
public sealed class AdminSessionInfo
{
    public required string Token { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}

/// <summary>DTO for a rental row in the admin rentals table.</summary>
public sealed class AdminRentalResponse
{
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required int VideoId { get; init; }
    public required string Title { get; init; }
    public required string Genre { get; init; }
    public required DateTime RentDateUtc { get; init; }
    public required DateTime ExpiryUtc { get; init; }
    public required decimal PaidAmount { get; init; }
}

/// <summary>Body for admin rent-on-behalf and return-on-behalf requests.</summary>
public sealed class AdminRentRequest
{
    public int UserId { get; set; }
}

/// <summary>Body for admin video edit requests.</summary>
public sealed class AdminEditVideoRequest
{
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public int ReleaseYear { get; set; }
    public int OwnerPublisherId { get; set; }
    public string Type { get; set; } = "Movie";
    public decimal RentalPrice { get; set; }
    public int RentalHours { get; set; }
    public bool IsPublished { get; set; }
}
