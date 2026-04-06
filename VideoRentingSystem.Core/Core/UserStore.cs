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
        // BST keys are lowercased usernames for case-insensitive login
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
            // insert order does not matter; tree sorts by username for inorder export
        }
    }

    public bool RegisterUser(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            return false;
        }

        string trimmedUsername = username.Trim();
        if (_userIndex.SearchByUsername(trimmedUsername) != null)
        {
            return false;
            // duplicate username rejected at tree layer
        }

        string passwordHash = ComputeSimpleHash(password);
        // Coursework stub: not salted PBKDF2/Argon2 — fine for local demo only.

        int nextId = _userIndex.Count + 1000;
        User newUser = new User(nextId, trimmedUsername, passwordHash);
        // simple id scheme avoids sqlite autoincrement coupling in the coursework brief

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
        // wrong password yields null just like unknown user
    }

    private string ComputeSimpleHash(string input)
    {
        int hash = 0;
        foreach (char c in input)
        {
            hash = (hash * 31) + c;
        }

        return hash.ToString("X8");
        // deterministic short string comparable to stored PasswordHash
    }
}
