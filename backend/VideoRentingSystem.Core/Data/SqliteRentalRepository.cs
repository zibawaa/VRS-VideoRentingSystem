using Microsoft.Data.Sqlite;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public sealed class SqliteRentalRepository : IRentalRepository
{
    private readonly string _connectionString;

    public SqliteRentalRepository(string databaseFilePath)
    {
        if (string.IsNullOrWhiteSpace(databaseFilePath))
        {
            throw new ArgumentException("Database file path is required.", nameof(databaseFilePath));
        }

        _connectionString = $"Data Source={databaseFilePath.Trim()}";
    }

    public Rental[] LoadAllRentals()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        const string countSql = "SELECT COUNT(*) FROM Rentals;";
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

        Rental[] rentals = new Rental[count];
        int index = 0;

        const string selectSql = "SELECT UserId, VideoId, RentDate, ExpiryUtc, PaidAmount FROM Rentals;";
        using SqliteCommand selectCommand = new(selectSql, connection);
        using SqliteDataReader reader = selectCommand.ExecuteReader();
        while (reader.Read())
        {
            rentals[index++] = new Rental(
                reader.GetInt32(0),
                reader.GetInt32(1),
                DateTime.Parse(reader.GetString(2), null, System.Globalization.DateTimeStyles.RoundtripKind),
                DateTime.Parse(reader.GetString(3), null, System.Globalization.DateTimeStyles.RoundtripKind),
                Convert.ToDecimal(reader.GetDouble(4)));
        }

        return rentals;
    }

    public void InsertRental(Rental rental)
    {
        // OR IGNORE: composite PK (UserId,VideoId) — idempotent if UI double-submits.
        const string sql = """
                           INSERT OR IGNORE INTO Rentals (UserId, VideoId, RentDate, ExpiryUtc, PaidAmount)
                           VALUES (@UserId, @VideoId, @RentDate, @ExpiryUtc, @PaidAmount);
                           """;

        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@UserId", rental.UserId);
        command.Parameters.AddWithValue("@VideoId", rental.VideoId);
        command.Parameters.AddWithValue("@RentDate", rental.RentDate.ToString("o"));
        command.Parameters.AddWithValue("@ExpiryUtc", rental.ExpiryUtc.ToString("o"));
        command.Parameters.AddWithValue("@PaidAmount", Convert.ToDouble(rental.PaidAmount));
        command.ExecuteNonQuery();
    }

    public void DeleteRental(int userId, int videoId)
    {
        const string sql = "DELETE FROM Rentals WHERE UserId = @UserId AND VideoId = @VideoId;";

        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@VideoId", videoId);
        command.ExecuteNonQuery();
    }

    public bool TryGetRentDate(int userId, int videoId, out DateTime rentDate)
    {
        const string sql = """
                           SELECT RentDate FROM Rentals
                           WHERE UserId = @UserId AND VideoId = @VideoId
                           LIMIT 1;
                           """;

        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@VideoId", videoId);

        object? scalar = command.ExecuteScalar();
        if (scalar == null || scalar is DBNull)
        {
            rentDate = default;
            return false;
        }

        rentDate = DateTime.Parse((string)scalar, null, System.Globalization.DateTimeStyles.RoundtripKind);
        return true;
    }

    public bool TryGetRentalInfo(int userId, int videoId, out DateTime rentDate, out DateTime expiryUtc, out decimal paidAmount)
    {
        const string sql = """
                           SELECT RentDate, ExpiryUtc, PaidAmount FROM Rentals
                           WHERE UserId = @UserId AND VideoId = @VideoId
                           LIMIT 1;
                           """;

        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@UserId", userId);
        command.Parameters.AddWithValue("@VideoId", videoId);
        using SqliteDataReader reader = command.ExecuteReader();
        if (!reader.Read())
        {
            rentDate = default;
            expiryUtc = default;
            paidAmount = 0m;
            return false;
        }

        rentDate = DateTime.Parse(reader.GetString(0), null, System.Globalization.DateTimeStyles.RoundtripKind);
        expiryUtc = DateTime.Parse(reader.GetString(1), null, System.Globalization.DateTimeStyles.RoundtripKind);
        paidAmount = Convert.ToDecimal(reader.GetDouble(2));
        return true;
    }
}
