using Microsoft.Data.Sqlite;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public sealed class SqliteUserRepository : IUserRepository
{
    private readonly string _connectionString;

    public SqliteUserRepository(string databaseFilePath)
    {
        if (string.IsNullOrWhiteSpace(databaseFilePath))
        {
            throw new ArgumentException("Database file path is required.", nameof(databaseFilePath));
        }

        _connectionString = $"Data Source={databaseFilePath.Trim()}";
    }

    public User[] LoadAllUsers()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        const string countSql = "SELECT COUNT(*) FROM Users;";
        int count;
        using (SqliteCommand countCommand = new(countSql, connection))
        {
            object? scalar = countCommand.ExecuteScalar();
            count = scalar == null ? 0 : Convert.ToInt32(scalar);
        }

        if (count == 0)
        {
            return [];
        }

        User[] users = new User[count];
        int index = 0;

        const string selectSql = "SELECT UserId, Username, PasswordHash FROM Users ORDER BY UserId ASC;";
        using SqliteCommand selectCommand = new(selectSql, connection);
        using SqliteDataReader reader = selectCommand.ExecuteReader();
        while (reader.Read())
        {
            users[index++] = new User(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2));
        }

        return users;
    }

    public void InsertUser(User user)
    {
        const string sql = """
                           INSERT INTO Users (UserId, Username, PasswordHash)
                           VALUES (@UserId, @Username, @PasswordHash);
                           """;

        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@UserId", user.UserId);
        command.Parameters.AddWithValue("@Username", user.Username);
        command.Parameters.AddWithValue("@PasswordHash", user.PasswordHash);
        command.ExecuteNonQuery();
    }
}
