using Microsoft.Data.SqlClient;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Data;

public sealed class SqlVideoRepository : IVideoRepository
{
    private readonly string _connectionString;
    private readonly string _databaseName;

    public SqlVideoRepository(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        }

        _connectionString = connectionString;
        SqlConnectionStringBuilder builder = new(connectionString);
        if (string.IsNullOrWhiteSpace(builder.InitialCatalog))
        {
            throw new ArgumentException("Connection string must include Initial Catalog (database name).", nameof(connectionString));
        }

        _databaseName = builder.InitialCatalog;
    }

    public void EnsureDatabaseAndSchema()
    {
        // Interpolated catalog name is only safe because we whitelist characters below.
        if (!IsSqlIdentifierSafe(_databaseName))
        {
            throw new InvalidOperationException("Database name contains unsupported characters.");
        }

        SqlConnectionStringBuilder masterBuilder = new(_connectionString)
        {
            InitialCatalog = "master"
        };

        string createDatabaseSql = $"IF DB_ID('{_databaseName}') IS NULL CREATE DATABASE [{_databaseName}];";
        using (SqlConnection masterConnection = new(masterBuilder.ConnectionString))
        {
            masterConnection.Open();
            using SqlCommand createDbCommand = new(createDatabaseSql, masterConnection);
            createDbCommand.ExecuteNonQuery();
        }

        const string sql = """
                           IF OBJECT_ID('dbo.Videos', 'U') IS NULL
                           BEGIN
                               CREATE TABLE dbo.Videos
                               (
                                   VideoId INT NOT NULL PRIMARY KEY,
                                   Title NVARCHAR(200) NOT NULL,
                                   Genre NVARCHAR(100) NOT NULL,
                                   ReleaseYear INT NOT NULL,
                                   IsRented BIT NOT NULL DEFAULT 0
                               );
                           END;
                           """;

        using SqlConnection connection = new(_connectionString);
        connection.Open();
        using SqlCommand command = new(sql, connection);
        command.ExecuteNonQuery();
    }

    public Video[] LoadAllVideos()
    {
        using SqlConnection connection = new(_connectionString);
        connection.Open();

        const string countSql = "SELECT COUNT(*) FROM dbo.Videos;";
        int count;
        using (SqlCommand countCommand = new(countSql, connection))
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
                                 FROM dbo.Videos
                                 ORDER BY Title ASC, VideoId ASC;
                                 """;

        using SqlCommand selectCommand = new(selectSql, connection);
        using SqlDataReader reader = selectCommand.ExecuteReader();
        while (reader.Read())
        {
            videos[index++] = new Video(
                reader.GetInt32(0),
                reader.GetString(1),
                reader.GetString(2),
                reader.GetInt32(3),
                reader.GetBoolean(4));
        }

        return videos;
    }

    public void UpsertVideo(Video video)
    {
        const string sql = """
                           MERGE dbo.Videos AS target
                           USING (VALUES (@VideoId, @Title, @Genre, @ReleaseYear, @IsRented))
                                 AS source (VideoId, Title, Genre, ReleaseYear, IsRented)
                           ON target.VideoId = source.VideoId
                           WHEN MATCHED THEN
                               UPDATE SET
                                   Title = source.Title,
                                   Genre = source.Genre,
                                   ReleaseYear = source.ReleaseYear,
                                   IsRented = source.IsRented
                           WHEN NOT MATCHED THEN
                               INSERT (VideoId, Title, Genre, ReleaseYear, IsRented)
                               VALUES (source.VideoId, source.Title, source.Genre, source.ReleaseYear, source.IsRented);
                           """;

        using SqlConnection connection = new(_connectionString);
        connection.Open();
        using SqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@VideoId", video.VideoId);
        command.Parameters.AddWithValue("@Title", video.Title);
        command.Parameters.AddWithValue("@Genre", video.Genre);
        command.Parameters.AddWithValue("@ReleaseYear", video.ReleaseYear);
        command.Parameters.AddWithValue("@IsRented", video.IsRented);
        command.ExecuteNonQuery();
    }

    public void DeleteVideo(int videoId)
    {
        const string sql = "DELETE FROM dbo.Videos WHERE VideoId = @VideoId;";
        using SqlConnection connection = new(_connectionString);
        connection.Open();
        using SqlCommand command = new(sql, connection);
        command.Parameters.AddWithValue("@VideoId", videoId);
        command.ExecuteNonQuery();
    }

    private static bool IsSqlIdentifierSafe(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool ok = (c >= 'A' && c <= 'Z') ||
                      (c >= 'a' && c <= 'z') ||
                      (c >= '0' && c <= '9') ||
                      c == '_';
            if (!ok)
            {
                return false;
            }
        }

        return true;
    }
}
