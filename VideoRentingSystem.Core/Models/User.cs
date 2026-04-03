// Minimal user record: an id, the login name, and the hash we compare against on sign-in. We never expose the hash through
// the UI — it is only for storage and tests.
namespace VideoRentingSystem.Core.Models;

public sealed class User
{
    public int UserId { get; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }

    public User(int userId, string username, string passwordHash)
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

        UserId = userId;
        Username = username.Trim();
        PasswordHash = passwordHash;
    }
}
