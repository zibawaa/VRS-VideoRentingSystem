namespace VideoRentingSystem.Api.Contracts.Auth;

public sealed class RegisterRequest
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = "Customer";
    public string? StudioName { get; set; }
}
