using System.Data;
using System.Data.SqlClient;
using VideoRentalSystem.Models;

namespace VideoRentalSystem.Data;

// References (Harvard):
// Microsoft (2025) 'SqlConnection Class', .NET Framework documentation. Available at: https://learn.microsoft.com/en-us/dotnet/api/system.data.sqlclient.sqlconnection (Accessed: 9 April 2026).
// Microsoft (2025) 'SQL Server Express LocalDB', SQL Server documentation. Available at: https://learn.microsoft.com/en-us/sql/database-engine/configure-windows/sql-server-express-localdb (Accessed: 9 April 2026).

// SQL Server LocalDB + attach .mdf — no extra NuGet packages (meets CST2550 "no third-party libraries" for the app).
public class RentalDatabase
{
    private string _attachConnectionString = "";
    private string _mdfPath = "";

    public bool IsOpen => !string.IsNullOrWhiteSpace(_attachConnectionString);

    public string MdfPath => _mdfPath;

    public void Open(string mdfFullPath)
    {
        if (string.IsNullOrWhiteSpace(mdfFullPath))
            throw new ArgumentException("Database path is empty.");

        _mdfPath = Path.GetFullPath(mdfFullPath.Trim());
        string folder = Path.GetDirectoryName(_mdfPath) ?? "";
        if (folder.Length > 0 && !Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        if (!File.Exists(_mdfPath))
            CreateLocalDbFiles(_mdfPath);

        var builder = new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\MSSQLLocalDB",
            IntegratedSecurity = true,
            ConnectTimeout = 30,
            AttachDBFilename = _mdfPath
        };
        _attachConnectionString = builder.ConnectionString;

        EnsureSchema();
    }

    // creates the .mdf/.ldf pair on the user's chosen path (first run)
    private static void CreateLocalDbFiles(string mdfPath)
    {
        string logPath = Path.ChangeExtension(mdfPath, ".ldf");
        string dbName = "VideoRental_" + Guid.NewGuid().ToString("N").Substring(0, 12);
        string dataFile = mdfPath.Replace("'", "''");
        string logFile = logPath.Replace("'", "''");

        using var conn = new SqlConnection(MasterConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = $@"
CREATE DATABASE [{dbName}] ON PRIMARY
  (NAME = N'{dbName}_data', FILENAME = N'{dataFile}')
LOG ON
  (NAME = N'{dbName}_log', FILENAME = N'{logFile}');";
        cmd.ExecuteNonQuery();
    }

    private static string MasterConnectionString =>
        new SqlConnectionStringBuilder
        {
            DataSource = @"(localdb)\MSSQLLocalDB",
            IntegratedSecurity = true,
            InitialCatalog = "master",
            ConnectTimeout = 30
        }.ConnectionString;

    private void EnsureSchema()
    {
        using var conn = new SqlConnection(_attachConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
IF NOT EXISTS (SELECT 1 FROM sys.tables WHERE name = N'VideoRentals')
BEGIN
    CREATE TABLE dbo.VideoRentals (
        RentalID INT NOT NULL PRIMARY KEY,
        Title NVARCHAR(200) NOT NULL,
        Genre NVARCHAR(100) NOT NULL,
        Director NVARCHAR(200) NOT NULL,
        [Year] INT NOT NULL,
        RentalPrice DECIMAL(10,2) NOT NULL,
        AvailableCopies INT NOT NULL
    );
END";
        cmd.ExecuteNonQuery();
    }

    public void Insert(RentalVideo v)
    {
        using var conn = new SqlConnection(_attachConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
INSERT INTO dbo.VideoRentals (RentalID, Title, Genre, Director, [Year], RentalPrice, AvailableCopies)
VALUES (@id, @t, @g, @d, @y, @p, @c);";
        cmd.Parameters.Add("@id", SqlDbType.Int).Value = v.RentalID;
        cmd.Parameters.Add("@t", SqlDbType.NVarChar, 200).Value = v.Title;
        cmd.Parameters.Add("@g", SqlDbType.NVarChar, 100).Value = v.Genre;
        cmd.Parameters.Add("@d", SqlDbType.NVarChar, 200).Value = v.Director;
        cmd.Parameters.Add("@y", SqlDbType.Int).Value = v.Year;
        var pPrice = cmd.Parameters.Add("@p", SqlDbType.Decimal);
        pPrice.Value = v.RentalPrice;
        pPrice.Precision = 10;
        pPrice.Scale = 2;
        cmd.Parameters.Add("@c", SqlDbType.Int).Value = v.AvailableCopies;
        cmd.ExecuteNonQuery();
    }

    public void Delete(int rentalId)
    {
        using var conn = new SqlConnection(_attachConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "DELETE FROM dbo.VideoRentals WHERE RentalID = @id;";
        cmd.Parameters.Add("@id", SqlDbType.Int).Value = rentalId;
        cmd.ExecuteNonQuery();
    }

    public void UpdateCopies(int rentalId, int newAvailable)
    {
        using var conn = new SqlConnection(_attachConnectionString);
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE dbo.VideoRentals SET AvailableCopies = @c WHERE RentalID = @id;";
        cmd.Parameters.Add("@c", SqlDbType.Int).Value = newAvailable;
        cmd.Parameters.Add("@id", SqlDbType.Int).Value = rentalId;
        cmd.ExecuteNonQuery();
    }

    // Time: O(n) for the count query + O(n) read — still linear in table size
    public RentalVideo[] LoadAll()
    {
        using var conn = new SqlConnection(_attachConnectionString);
        conn.Open();

        int n;
        using (var countCmd = conn.CreateCommand())
        {
            countCmd.CommandText = "SELECT COUNT(*) FROM dbo.VideoRentals;";
            n = Convert.ToInt32(countCmd.ExecuteScalar());
        }

        if (n == 0)
            return Array.Empty<RentalVideo>();

        var arr = new RentalVideo[n];
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT RentalID, Title, Genre, Director, [Year], RentalPrice, AvailableCopies FROM dbo.VideoRentals ORDER BY RentalID;";
        int i = 0;
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            arr[i++] = new RentalVideo
            {
                RentalID = r.GetInt32(0),
                Title = r.GetString(1),
                Genre = r.GetString(2),
                Director = r.GetString(3),
                Year = r.GetInt32(4),
                RentalPrice = r.GetDecimal(5),
                AvailableCopies = r.GetInt32(6)
            };
        }

        return arr;
    }
}
