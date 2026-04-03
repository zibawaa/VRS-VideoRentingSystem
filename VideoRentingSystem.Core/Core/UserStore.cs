// Handles registration/login only. We store hashes instead of plaintext passwords because the coursework brief calls that out,
// even though our hashing function is deliberately tiny (easy to read in a viva, not safe for real apps).
using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.DataStructures;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Core;

public sealed class UserStore
{
    private readonly UserBstIndex _userIndex;
    private readonly IUserRepository? _repository;

    public UserStore(IUserRepository? repository = null)
    {
        _userIndex = new UserBstIndex();
        _repository = repository;
    }

    public int Count => _userIndex.Count;

    public void LoadFromRepository()
    {
        if (_repository == null)
        {
            return;
        }

        User[] users = _repository.LoadAllUsers();
        for (int i = 0; i < users.Length; i++)
        {
            _userIndex.Add(users[i]);
        }
    }

    public bool RegisterUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        string trimmedUsername = username.Trim();
        User? existing = _userIndex.SearchByUsername(trimmedUsername);

        if (existing != null)
        {
            // Easiest UX: refuse duplicates instead of silently overwriting — avoids "where did my account go" confusion.
            return false;
        }

        string passwordHash = ComputeSimpleHash(password);

        // Cheap deterministic IDs for coursework — production code would ask the database for an identity/sequence value.
        int nextId = _userIndex.Count + 1000;
        User newUser = new User(nextId, trimmedUsername, passwordHash);

        bool added = _userIndex.Add(newUser);

        if (added)
        {
            _repository?.InsertUser(newUser);
            return true;
        }

        return false;
    }

    public User? Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return null;
        }

        User? user = _userIndex.SearchByUsername(username.Trim());
        if (user == null)
        {
            return null;
        }

        string inputHash = ComputeSimpleHash(password);
        if (user.PasswordHash == inputHash)
        {
            return user;
        }

        return null;
    }

    // Not SHA256 on purpose — we wanted something we could explain line-by-line without pulling in extra crypto APIs.
    private string ComputeSimpleHash(string input)
    {
        int hash = 0;
        foreach (char c in input)
        {
            hash = (hash * 31) + c;
        }

        return hash.ToString("X8");
    }
}
