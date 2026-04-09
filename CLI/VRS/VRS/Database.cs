using System;
using System.Collections.Generic;
using System.Data.SqlClient;

namespace VRS
{
    public static class Database
    {
        // Make sure this matches your SQL Server + DB
        private static string connectionString = @"Server=localhost;Database=VRS;Trusted_Connection=True;";

        // =======================
        // CREATE FAN
        // =======================
        public static void CreateFan(Fan fan)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO Fans (Name, Email, PasswordHash, Salt) VALUES (@n, @e, @p, @s)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@n", fan.Name);
                cmd.Parameters.AddWithValue("@e", fan.Email);
                cmd.Parameters.AddWithValue("@p", fan.PasswordHash);
                cmd.Parameters.AddWithValue("@s", fan.Salt);
                cmd.ExecuteNonQuery();
            }
        }

        // =======================
        // CREATE ARTIST
        // =======================
        public static void CreateArtist(Artist artist)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO Artists (Name, Email, PasswordHash, Salt, TotalEarnings) VALUES (@n, @e, @p, @s, 0)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@n", artist.Name);
                cmd.Parameters.AddWithValue("@e", artist.Email);
                cmd.Parameters.AddWithValue("@p", artist.PasswordHash);
                cmd.Parameters.AddWithValue("@s", artist.Salt);
                cmd.ExecuteNonQuery();
            }
        }

        // =======================
        // CREATE ADMIN
        // =======================
        public static void CreateAdmin(Admin admin)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO Admins (Name, Email, PasswordHash, Salt) VALUES (@n, @e, @p, @s)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@n", admin.Name);
                cmd.Parameters.AddWithValue("@e", admin.Email);
                cmd.Parameters.AddWithValue("@p", admin.PasswordHash);
                cmd.Parameters.AddWithValue("@s", admin.Salt);
                cmd.ExecuteNonQuery();
            }
        }

        // =======================
        // FAN LOGIN
        // =======================
        public static Fan LoginFan(string email)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Fans WHERE Email=@e";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@e", email);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new Fan
                    {
                        ID = (int)reader["FanID"],
                        Name = reader["Name"].ToString(),
                        Email = reader["Email"].ToString(),
                        PasswordHash = reader["PasswordHash"].ToString(),
                        Salt = reader["Salt"].ToString()
                    };
                }
            }
            return null;
        }

        // =======================
        // ARTIST LOGIN
        // =======================
        public static Artist LoginArtist(string email)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Artists WHERE Email=@e";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@e", email);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new Artist
                    {
                        ID = (int)reader["ArtistID"],
                        Name = reader["Name"].ToString(),
                        Email = reader["Email"].ToString(),
                        PasswordHash = reader["PasswordHash"].ToString(),
                        Salt = reader["Salt"].ToString(),
                        TotalEarnings = Convert.ToDouble(reader["TotalEarnings"])
                    };
                }
            }
            return null;
        }

        // =======================
        // GET ARTIST BY ID
        // =======================
        public static Artist GetArtistById(int artistId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Artists WHERE ArtistID=@id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", artistId);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new Artist
                    {
                        ID = (int)reader["ArtistID"],
                        Name = reader["Name"].ToString(),
                        Email = reader["Email"].ToString(),
                        PasswordHash = reader["PasswordHash"].ToString(),
                        Salt = reader["Salt"].ToString(),
                        TotalEarnings = Convert.ToDouble(reader["TotalEarnings"])
                    };
                }
            }
            return null;
        }

        // =======================
        // ADMIN LOGIN
        // =======================
        public static Admin LoginAdmin(string email)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Admins WHERE Email=@e";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@e", email);
                SqlDataReader reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new Admin
                    {
                        ID = (int)reader["AdminID"],
                        Name = reader["Name"].ToString(),
                        Email = reader["Email"].ToString(),
                        PasswordHash = reader["PasswordHash"].ToString(),
                        Salt = reader["Salt"].ToString()
                    };
                }
            }
            return null;
        }

        // =======================
        // ADD VIDEO
        // =======================
        public static void AddVideo(Video video)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "INSERT INTO Videos (Title, Genre, ReleaseYear, ArtistID, Price) " +
                             "VALUES (@t, @g, @y, @a, @p)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@t", video.Title);
                cmd.Parameters.AddWithValue("@g", video.Genre);
                cmd.Parameters.AddWithValue("@y", video.ReleaseYear);
                cmd.Parameters.AddWithValue("@a", video.ArtistID);
                cmd.Parameters.AddWithValue("@p", video.Price);
                cmd.ExecuteNonQuery();
            }
        }

        // =======================
        // UPDATE VIDEO (EDIT)
        // =======================
        public static void UpdateVideo(Video video)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE Videos SET Title=@t, Genre=@g, ReleaseYear=@y, Price=@p " +
                             "WHERE VideoID=@id AND ArtistID=@a";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@t", video.Title);
                cmd.Parameters.AddWithValue("@g", video.Genre);
                cmd.Parameters.AddWithValue("@y", video.ReleaseYear);
                cmd.Parameters.AddWithValue("@p", video.Price);
                cmd.Parameters.AddWithValue("@id", video.VideoID);
                cmd.Parameters.AddWithValue("@a", video.ArtistID);
                cmd.ExecuteNonQuery();
            }
        }

        // =======================
        // LOAD ALL VIDEOS
        // =======================
        public static List<Video> LoadAllVideos()
        {
            List<Video> videos = new List<Video>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Videos";
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    videos.Add(new Video
                    {
                        VideoID = (int)reader["VideoID"],
                        Title = reader["Title"].ToString(),
                        Genre = reader["Genre"].ToString(),
                        ReleaseYear = (int)reader["ReleaseYear"],
                        ArtistID = (int)reader["ArtistID"],
                        Price = Convert.ToDouble(reader["Price"])
                    });
                }
            }

            return videos;
        }

        // =======================
        // LOAD VIDEOS BY ARTIST
        // =======================
        public static List<Video> LoadVideosByArtist(int artistId)
        {
            List<Video> videos = new List<Video>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Videos WHERE ArtistID=@a";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@a", artistId);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    videos.Add(new Video
                    {
                        VideoID = (int)reader["VideoID"],
                        Title = reader["Title"].ToString(),
                        Genre = reader["Genre"].ToString(),
                        ReleaseYear = (int)reader["ReleaseYear"],
                        ArtistID = (int)reader["ArtistID"],
                        Price = Convert.ToDouble(reader["Price"])
                    });
                }
            }

            return videos;
        }

        // =======================
        // UPDATE ARTIST EARNINGS
        // =======================
        public static void UpdateArtistEarnings(int artistId, double amount)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "UPDATE Artists SET TotalEarnings = TotalEarnings + @amt WHERE ArtistID=@id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@amt", amount);
                cmd.Parameters.AddWithValue("@id", artistId);
                cmd.ExecuteNonQuery();
            }
        }

        // =======================
        // LOAD ALL ARTISTS
        // =======================
        public static List<Artist> LoadAllArtists()
        {
            List<Artist> artists = new List<Artist>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Artists";
                SqlCommand cmd = new SqlCommand(sql, conn);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    artists.Add(new Artist
                    {
                        ID = (int)reader["ArtistID"],
                        Name = reader["Name"].ToString(),
                        Email = reader["Email"].ToString(),
                        PasswordHash = reader["PasswordHash"].ToString(),
                        Salt = reader["Salt"].ToString(),
                        TotalEarnings = Convert.ToDouble(reader["TotalEarnings"])
                    });
                }
            }

            return artists;
        }

        // =======================
        // DELETE VIDEO (ADMIN OR ARTIST)
        // =======================
        public static void DeleteVideo(int videoId)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = "DELETE FROM Videos WHERE VideoID=@id";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@id", videoId);
                cmd.ExecuteNonQuery();
            }
        }

        // =======================
        // ADD PURCHASE
        // =======================
        public static void AddPurchase(int fanId, Video video)
        {
            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();

                // Insert purchase
                string sql = "INSERT INTO Purchases (FanID, VideoID, PurchaseDate, Price) " +
                             "VALUES (@f, @v, @d, @p)";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@f", fanId);
                cmd.Parameters.AddWithValue("@v", video.VideoID);
                cmd.Parameters.AddWithValue("@d", DateTime.Now);
                cmd.Parameters.AddWithValue("@p", video.Price);
                cmd.ExecuteNonQuery();

                // Update artist earnings
                string sql2 = "UPDATE Artists SET TotalEarnings = TotalEarnings + @p WHERE ArtistID=@a";
                SqlCommand cmd2 = new SqlCommand(sql2, conn);
                cmd2.Parameters.AddWithValue("@p", video.Price);
                cmd2.Parameters.AddWithValue("@a", video.ArtistID);
                cmd2.ExecuteNonQuery();
            }
        }

        // =======================
        // GET PURCHASED VIDEOS BY FAN
        // =======================
        public static List<Video> GetPurchasedVideosByFan(int fanId)
        {
            List<Video> videos = new List<Video>();

            using (SqlConnection conn = new SqlConnection(connectionString))
            {
                conn.Open();
                string sql = @"
                    SELECT v.VideoID, v.Title, v.Genre, v.ReleaseYear, v.ArtistID, p.Price
                    FROM Purchases p
                    INNER JOIN Videos v ON p.VideoID = v.VideoID
                    WHERE p.FanID = @f";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("@f", fanId);
                SqlDataReader reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    videos.Add(new Video
                    {
                        VideoID = (int)reader["VideoID"],
                        Title = reader["Title"].ToString(),
                        Genre = reader["Genre"].ToString(),
                        ReleaseYear = (int)reader["ReleaseYear"],
                        ArtistID = (int)reader["ArtistID"],
                        Price = Convert.ToDouble(reader["Price"])
                    });
                }
            }

            return videos;
        }
    }
}
