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
    }

    public void EnsureDatabaseAndSchema()
    {
        string? directory = Path.GetDirectoryName(_databaseFilePath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        const string createTableSql = """
                                      CREATE TABLE IF NOT EXISTS Videos
                                      (
                                          VideoId INTEGER NOT NULL PRIMARY KEY,
                                          Title TEXT NOT NULL,
                                          Genre TEXT NOT NULL,
                                          ReleaseYear INTEGER NOT NULL,
                                          IsRented INTEGER NOT NULL DEFAULT 0
                                      );
                                      """;

        using SqliteCommand command = new(createTableSql, connection);
        command.ExecuteNonQuery();
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

        if (count == 0)
        {
            return [];
        }

        Video[] videos = new Video[count];
        int index = 0;

        const string selectSql = """
                                 SELECT VideoId, Title, Genre, ReleaseYear, IsRented
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
                reader.GetInt64(4) == 1);
        }

        return videos;
    }

    public void UpsertVideo(Video video)
    {
        const string sql = """
                           INSERT INTO Videos (VideoId, Title, Genre, ReleaseYear, IsRented)
                           VALUES (@VideoId, @Title, @Genre, @ReleaseYear, @IsRented)
                           ON CONFLICT(VideoId) DO UPDATE SET
                               Title = excluded.Title,
                               Genre = excluded.Genre,
                               ReleaseYear = excluded.ReleaseYear,
                               IsRented = excluded.IsRented;
                           """;

        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@VideoId", video.VideoId);
        command.Parameters.AddWithValue("@Title", video.Title);
        command.Parameters.AddWithValue("@Genre", video.Genre);
        command.Parameters.AddWithValue("@ReleaseYear", video.ReleaseYear);
        command.Parameters.AddWithValue("@IsRented", video.IsRented ? 1 : 0);
        command.ExecuteNonQuery();
    }

    public void DeleteVideo(int videoId)
    {
        const string sql = "DELETE FROM Videos WHERE VideoId = @VideoId;";

        using SqliteConnection connection = new(_connectionString);
        connection.Open();

        using SqliteCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@VideoId", videoId);
        command.ExecuteNonQuery();
    }
}
