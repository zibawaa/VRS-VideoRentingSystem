// Same idea as IVideoRepository but for accounts — keeps UserStore testable without shipping a real database file.
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public interface IUserRepository
{
    User[] LoadAllUsers();
    void InsertUser(User user);
}
