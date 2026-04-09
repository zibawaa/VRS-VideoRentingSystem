using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Security;

/// <summary>
/// Immutable snapshot of a user's active session. Created at login/register
/// and attached to HttpContext by AuthMiddleware for the request lifetime.
/// </summary>
public sealed class AuthSession
{
    public required string Token { get; init; }
    public required int UserId { get; init; }
    public required string Username { get; init; }
    public required UserRole Role { get; init; }
    public string? StudioName { get; init; }
    public required DateTime ExpiresAtUtc { get; init; }
}
