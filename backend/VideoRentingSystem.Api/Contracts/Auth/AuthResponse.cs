namespace VideoRentingSystem.Api.Contracts.Auth;

public sealed class AuthResponse
{
    public required string Token { get; init; }
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required string Role { get; init; }
    public string? StudioName { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}
