namespace VideoRentingSystem.Core.Models;

public enum UserRole
{
    Customer = 0,
    Publisher = 1,
    Admin = 2
}

public sealed class User
{
    public int UserId { get; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public UserRole Role { get; private set; }
    public string? StudioName { get; private set; }

    public User(int userId, string username, string passwordHash, UserRole role = UserRole.Customer, string? studioName = null)
    {
        if (userId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(userId), "User ID must be greater than 0.");
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        if (role == UserRole.Customer && !string.IsNullOrWhiteSpace(studioName))
        {
            throw new ArgumentException("Studio name can only be set for publisher or admin users.", nameof(studioName));
        }
        // only creator-style accounts should carry studio metadata

        UserId = userId;
        Username = username.Trim();
        PasswordHash = passwordHash;
        Role = role;
        StudioName = string.IsNullOrWhiteSpace(studioName) ? null : studioName.Trim();
        // usernames are trimmed everywhere so tree search stays consistent with registration
    }
}
