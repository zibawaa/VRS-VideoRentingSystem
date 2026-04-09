using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.DataStructures;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Core;

public sealed class UserStore
{
    private readonly UserBstIndex _userIndex;
    private readonly IUserRepository? _repository;
    private int _maxAssignedUserId = 999;

    public UserStore(IUserRepository? repository = null)
    {
        _userIndex = new UserBstIndex();
        _repository = repository;
        // BST keys are lowercased usernames for case-insensitive login
    }

    public int Count => _userIndex.Count;

    // returns every registered user sorted alphabetically by username (BST inorder)
    public User[] GetAllUsers()
    {
        return _userIndex.ToArray();
    }

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
            if (users[i].UserId > _maxAssignedUserId)
            {
                _maxAssignedUserId = users[i].UserId;
            }
            // insert order does not matter; tree sorts by username for inorder export
        }
    }

    public bool RegisterUser(string username, string password)
    {
        return RegisterUser(username, password, UserRole.Customer);
    }

    public bool RegisterUser(string username, string password, UserRole role, string? studioName = null)
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

        int nextId = GenerateUniqueUserId();
        User newUser = new User(nextId, trimmedUsername, passwordHash, role, studioName);
        // generate a high-entropy id so parallel API hosts do not race on sequential ids

        bool added = _userIndex.Add(newUser);
        if (added)
        {
            _maxAssignedUserId = nextId;
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

    private int GenerateUniqueUserId()
    {
        User[] existing = _userIndex.ToArray();
        for (int attempt = 0; attempt < 20; attempt++)
        {
            int candidate = Math.Abs(Guid.NewGuid().GetHashCode());
            if (candidate < 1000)
            {
                candidate += 1000;
            }

            bool collision = false;
            for (int i = 0; i < existing.Length; i++)
            {
                if (existing[i].UserId == candidate)
                {
                    collision = true;
                    break;
                }
            }

            if (!collision)
            {
                return candidate;
            }
        }

        _maxAssignedUserId++;
        return _maxAssignedUserId;
        // deterministic fallback keeps registration progressing if random attempts all collide
    }
}
