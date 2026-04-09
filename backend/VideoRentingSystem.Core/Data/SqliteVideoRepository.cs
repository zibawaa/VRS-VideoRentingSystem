using Microsoft.Data.Sqlite;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public sealed class SqliteVideoRepository : IVideoRepository
{
    private readonly string _databaseFilePath;
    private readonly string _connectionString;

    public SqliteVideoRepository(string databaseFilePath)
    {
        if (string.IsNullOrWhiteSpace(databaseFilePath))
        {
            throw new ArgumentException("Database file path is required.", nameof(databaseFilePath));
        }

        _databaseFilePath = databaseFilePath.Trim();
        _connectionString = $"Data Source={_databaseFilePath}";
        // classic sqlite connection string pointing at a single file on disk
    }

    public void EnsureDatabaseAndSchema()
    {
        string? directory = Path.GetDirectoryName(_databaseFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }
        // create parent folders first so Sqlite can create the db file

        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        // One batch: Videos for the app, Users/Rentals for auth + who- rented-what persistence.
        const string createTableSql = """
                                      CREATE TABLE IF NOT EXISTS Videos
                                      (
                                          VideoId INTEGER NOT NULL PRIMARY KEY,
                                          Title TEXT NOT NULL,
                                          Genre TEXT NOT NULL,
                                          ReleaseYear INTEGER NOT NULL,
                                          IsRented INTEGER NOT NULL DEFAULT 0,
                                          OwnerPublisherId INTEGER NOT NULL DEFAULT 0,
                                          VideoType INTEGER NOT NULL DEFAULT 0,
                                          RentalPrice REAL NOT NULL DEFAULT 2.99,
                                          RentalHours INTEGER NOT NULL DEFAULT 48,
                                          IsPublished INTEGER NOT NULL DEFAULT 1
                                      );

                                      CREATE TABLE IF NOT EXISTS Users
                                      (
                                          UserId INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
                                          Username TEXT NOT NULL UNIQUE,
                                          PasswordHash TEXT NOT NULL,
                                          Role INTEGER NOT NULL DEFAULT 0,
                                          StudioName TEXT NULL
                                      );

                                      CREATE TABLE IF NOT EXISTS Rentals
                                      (
                                          UserId INTEGER NOT NULL,
                                          VideoId INTEGER NOT NULL,
                                          RentDate TEXT NOT NULL,
                                          ExpiryUtc TEXT NOT NULL,
                                          PaidAmount REAL NOT NULL DEFAULT 0,
                                          PRIMARY KEY(UserId, VideoId),
                                          FOREIGN KEY(UserId) REFERENCES Users(UserId),
                                          FOREIGN KEY(VideoId) REFERENCES Videos(VideoId)
                                      );
                                      """;

        using SqliteCommand command = new(createTableSql, connection);
        command.ExecuteNonQuery();
        // IF NOT EXISTS keeps this safe to call on every app start

        EnsureColumn(connection, "Videos", "OwnerPublisherId", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Videos", "VideoType", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Videos", "RentalPrice", "REAL NOT NULL DEFAULT 2.99");
        EnsureColumn(connection, "Videos", "RentalHours", "INTEGER NOT NULL DEFAULT 48");
        EnsureColumn(connection, "Videos", "IsPublished", "INTEGER NOT NULL DEFAULT 1");
        EnsureColumn(connection, "Users", "Role", "INTEGER NOT NULL DEFAULT 0");
        EnsureColumn(connection, "Users", "StudioName", "TEXT NULL");
        EnsureColumn(connection, "Rentals", "ExpiryUtc", "TEXT NOT NULL DEFAULT '1970-01-01T00:00:00.0000000Z'");
        EnsureColumn(connection, "Rentals", "PaidAmount", "REAL NOT NULL DEFAULT 0");
        // old coursework databases are migrated by adding missing columns with safe defaults
    }

    public Video[] LoadAllVideos()
    {
        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        const string countSql = "SELECT COUNT(*) FROM Videos;";
        int count;
        using (SqliteCommand countCommand = new(countSql, connection))
        {
            object? scalar = countCommand.ExecuteScalar();
            count = scalar == null ? 0 : Convert.ToInt32(scalar);
        }
        // size the managed array once so we avoid List reallocations on big catalogues

        if (count == 0)
        {
            return [];
        }

        Video[] videos = new Video[count];
        int index = 0;

        const string selectSql = """
                                 SELECT VideoId, Title, Genre, ReleaseYear, IsRented, OwnerPublisherId, VideoType, RentalPrice, RentalHours, IsPublished
                                 FROM Videos
                                 ORDER BY Title ASC, VideoId ASC;
                                 """;

        using SqliteCommand selectCommand = new(selectSql, connection);
        using SqliteDataReader reader = selectCommand.ExecuteReader();
        while (reader.Read())
        {
            videos[index++] = new Video(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetInt64(4) == 1,
                reader.GetInt32(5),
                (VideoType)reader.GetInt32(6),
                Convert.ToDecimal(reader.GetDouble(7)),
                reader.GetInt32(8),
                reader.GetInt64(9) == 1);
            // sqlite stores bool as 0/1 integer flags in this schema
        }

        return videos;
    }

    public void UpsertVideo(Video video)
    {
        const string sql = """
                           INSERT INTO Videos (VideoId, Title, Genre, ReleaseYear, IsRented, OwnerPublisherId, VideoType, RentalPrice, RentalHours, IsPublished)
                           VALUES (@VideoId, @Title, @Genre, @ReleaseYear, @IsRented, @OwnerPublisherId, @VideoType, @RentalPrice, @RentalHours, @IsPublished)
                           ON CONFLICT(VideoId) DO UPDATE SET
                               Title = excluded.Title,
                               Genre = excluded.Genre,
                               ReleaseYear = excluded.ReleaseYear,
                               IsRented = excluded.IsRented,
                               OwnerPublisherId = excluded.OwnerPublisherId,
                               VideoType = excluded.VideoType,
                               RentalPrice = excluded.RentalPrice,
                               RentalHours = excluded.RentalHours,
                               IsPublished = excluded.IsPublished;
                           """;

        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@VideoId", video.VideoId);
        command.Parameters.AddWithValue("@Title", video.Title);
        command.Parameters.AddWithValue("@Genre", video.Genre);
        command.Parameters.AddWithValue("@ReleaseYear", video.ReleaseYear);
        command.Parameters.AddWithValue("@IsRented", video.IsRented ? 1 : 0);
        command.Parameters.AddWithValue("@OwnerPublisherId", video.OwnerPublisherId);
        command.Parameters.AddWithValue("@VideoType", (int)video.Type);
        command.Parameters.AddWithValue("@RentalPrice", Convert.ToDouble(video.RentalPrice));
        command.Parameters.AddWithValue("@RentalHours", video.RentalHours);
        command.Parameters.AddWithValue("@IsPublished", video.IsPublished ? 1 : 0);
        command.ExecuteNonQuery();
        // upsert keeps one row per VideoId and updates metadata after rentals
    }

    public void DeleteVideo(int videoId)
    {
        const string sql = "DELETE FROM Videos WHERE VideoId = @VideoId;";
        using SqliteConnection connection = new(_connectionString);
        connection.Open();
        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@VideoId", videoId);
        command.ExecuteNonQuery();
        // caller already removed from in-memory structures; this drops the disk row
    }

    private static void EnsureColumn(SqliteConnection connection, string table, string column, string definition)
    {
        try
        {
            using SqliteCommand command = new($"ALTER TABLE {table} ADD COLUMN {column} {definition};", connection);
            command.ExecuteNonQuery();
        }
        catch (SqliteException ex) when (ex.SqliteErrorCode == 1 && ex.Message.Contains("duplicate column name", StringComparison.OrdinalIgnoreCase))
        {
            // column already exists, so schema is already at or above this revision
        }
    }
}
