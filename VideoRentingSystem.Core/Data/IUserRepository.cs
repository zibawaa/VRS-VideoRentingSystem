using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public interface IUserRepository
{
    User[] LoadAllUsers();
    void InsertUser(User user);
    // minimal user table surface: hydrate on startup, append on register
}
