using System.Collections.Concurrent;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Security;

/// <summary>
/// In-memory bearer token session store. Tokens are GUID-based and auto-expire
/// after <see cref="SessionLifetime"/>. Thread-safe via ConcurrentDictionary.
/// </summary>
public sealed class AuthSessionService
{
    // thread-safe dictionary mapping token strings to their session objects
    private readonly ConcurrentDictionary<string, AuthSession> _sessions = new(StringComparer.Ordinal);

    // how long a token remains valid after creation
    private static readonly TimeSpan SessionLifetime = TimeSpan.FromHours(8);

    // mints a new opaque bearer token and stores the session keyed by it
    public AuthSession CreateSession(User user)
    {
        string token = Guid.NewGuid().ToString("N");
        AuthSession session = new()
        {
            Token = token,
            UserId = user.UserId,
            Username = user.Username,
            Role = user.Role,
            StudioName = user.StudioName,
            ExpiresAtUtc = DateTime.UtcNow.Add(SessionLifetime)
        };

        _sessions[token] = session;
        return session;
    }

    // returns the session if the token exists and hasn't expired, otherwise evicts it
    public bool TryGetSession(string token, out AuthSession? session)
    {
        if (_sessions.TryGetValue(token, out AuthSession? cached))
        {
            if (cached.ExpiresAtUtc > DateTime.UtcNow)
            {
                session = cached;
                return true;
            }

            // token has expired – remove it from the store so it can't linger
            _sessions.TryRemove(token, out _);
        }

        session = null;
        return false;
    }

    // immediately destroys a session so the token can never be reused
    public void Revoke(string token)
    {
        _sessions.TryRemove(token, out _);
    }

    // returns all non-expired sessions for a given user (admin diagnostics)
    public AuthSession[] GetSessionsByUser(int userId)
    {
        DateTime now = DateTime.UtcNow;
        List<AuthSession> results = new();

        foreach (var kvp in _sessions)
        {
            if (kvp.Value.UserId == userId && kvp.Value.ExpiresAtUtc > now)
            {
                results.Add(kvp.Value);
            }
        }

        return results.ToArray();
    }

    // returns every active (non-expired) session across all users
    public AuthSession[] GetAllSessions()
    {
        DateTime now = DateTime.UtcNow;
        List<AuthSession> results = new();

        foreach (var kvp in _sessions)
        {
            if (kvp.Value.ExpiresAtUtc > now)
            {
                results.Add(kvp.Value);
            }
        }

        return results.ToArray();
    }
}
