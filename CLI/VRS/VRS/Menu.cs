using System;
using System.Collections.Generic;

namespace VRS
{
    public static class Menu
    {
        public static void MainMenu()
        {
            // Reload BST from DB every time we hit main menu
            Program.videoTree.Clear();
            var videos = Database.LoadAllVideos();
            foreach (var v in videos)
                Program.videoTree.Insert(v);

            Console.Clear();
            Console.WriteLine("===== VRS MAIN MENU =====");
            Console.WriteLine("1. Create Fan Account");
            Console.WriteLine("2. Login as Fan");
            Console.WriteLine("3. Create Artist Account");
            Console.WriteLine("4. Login as Artist");
            Console.WriteLine("5. Create Admin Account");
            Console.WriteLine("6. Login as Admin");
            Console.WriteLine("7. Exit");
            Console.Write("Choose: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    CreateFanAccount();
                    break;
                case "2":
                    FanLogin();
                    break;
                case "3":
                    CreateArtistAccount();
                    break;
                case "4":
                    ArtistLogin();
                    break;
                case "5":
                    CreateAdminAccount();
                    break;
                case "6":
                    AdminLogin();
                    break;
                case "7":
                    Environment.Exit(0);
                    break;
                default:
                    MainMenu();
                    break;
            }
        }

        // =======================
        // FAN ACCOUNT CREATION
        // =======================
        private static void CreateFanAccount()
        {
            Console.Clear();
            Console.WriteLine("===== CREATE FAN ACCOUNT =====");

            Console.Write("Name: ");
            string name = Console.ReadLine();

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            string salt = Authentication.GenerateSalt();
            string hash = Authentication.HashPassword(password, salt);

            Fan fan = new Fan
            {
                Name = name,
                Email = email,
                PasswordHash = hash,
                Salt = salt
            };

            Database.CreateFan(fan);

            Console.WriteLine("Fan account created successfully!");
            Console.ReadLine();
            MainMenu();
        }

        // =======================
        // ARTIST ACCOUNT CREATION
        // =======================
        private static void CreateArtistAccount()
        {
            Console.Clear();
            Console.WriteLine("===== CREATE ARTIST ACCOUNT =====");

            Console.Write("Name: ");
            string name = Console.ReadLine();

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            string salt = Authentication.GenerateSalt();
            string hash = Authentication.HashPassword(password, salt);

            Artist artist = new Artist
            {
                Name = name,
                Email = email,
                PasswordHash = hash,
                Salt = salt,
                TotalEarnings = 0
            };

            Database.CreateArtist(artist);

            Console.WriteLine("Artist account created successfully!");
            Console.ReadLine();
            MainMenu();
        }

        // =======================
        // ADMIN ACCOUNT CREATION
        // =======================
        private static void CreateAdminAccount()
        {
            Console.Clear();
            Console.WriteLine("===== CREATE ADMIN ACCOUNT =====");

            Console.Write("Name: ");
            string name = Console.ReadLine();

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            string salt = Authentication.GenerateSalt();
            string hash = Authentication.HashPassword(password, salt);

            Admin admin = new Admin
            {
                Name = name,
                Email = email,
                PasswordHash = hash,
                Salt = salt
            };

            Database.CreateAdmin(admin);

            Console.WriteLine("Admin account created successfully!");
            Console.ReadLine();
            MainMenu();
        }

        // =======================
        // FAN LOGIN
        // =======================
        private static void FanLogin()
        {
            Console.Clear();
            Console.WriteLine("===== FAN LOGIN =====");

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            Fan fan = Database.LoginFan(email);

            if (fan == null)
            {
                Console.WriteLine("Fan not found.");
                Console.ReadLine();
                MainMenu();
                return;
            }

            bool ok = Authentication.VerifyPassword(password, fan.PasswordHash, fan.Salt);

            if (!ok)
            {
                Console.WriteLine("Invalid password.");
                Console.ReadLine();
                MainMenu();
                return;
            }

            FanMenu(fan);
        }

        // =======================
        // FAN MENU
        // =======================
        private static void FanMenu(Fan fan)
        {
            Console.Clear();
            Console.WriteLine($"===== FAN MENU ({fan.Name}) =====");
            Console.WriteLine("1. View All Videos");
            Console.WriteLine("2. Search Videos by Title");
            Console.WriteLine("3. Purchase Video by ID");
            Console.WriteLine("4. View Purchased Videos");
            Console.WriteLine("5. Logout");
            Console.Write("Choose: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ViewAllVideos(() => FanMenu(fan));
                    break;
                case "2":
                    FanSearchVideos(fan);
                    break;
                case "3":
                    FanPurchaseVideo(fan);
                    break;
                case "4":
                    FanViewPurchasedVideos(fan);
                    break;
                case "5":
                    MainMenu();
                    break;
                default:
                    FanMenu(fan);
                    break;
            }
        }

        private static void ViewAllVideos(Action returnAction)
        {
            Console.Clear();
            Console.WriteLine("===== ALL VIDEOS =====");

            List<Video> videos = Program.videoTree.InOrder();

            if (videos.Count == 0)
            {
                Console.WriteLine("No videos available.");
            }
            else
            {
                foreach (var v in videos)
                {
                    Console.WriteLine($"ID: {v.VideoID} | Title: {v.Title} | Genre: {v.Genre} | Year: {v.ReleaseYear} | Price: {v.Price}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press Enter to return...");
            Console.ReadLine();
            returnAction();
        }

        private static void FanSearchVideos(Fan fan)
        {
            Console.Clear();
            Console.WriteLine("===== SEARCH VIDEOS BY TITLE =====");
            Console.Write("Enter part of the title: ");
            string term = Console.ReadLine().ToLower();

            List<Video> videos = Program.videoTree.InOrder();
            List<Video> results = new List<Video>();

            foreach (var v in videos)
            {
                if (!string.IsNullOrEmpty(v.Title) && v.Title.ToLower().Contains(term))
                    results.Add(v);
            }

            Console.WriteLine();
            if (results.Count == 0)
            {
                Console.WriteLine("No matching videos found.");
            }
            else
            {
                Console.WriteLine("Results:");
                foreach (var v in results)
                {
                    Console.WriteLine($"ID: {v.VideoID} | Title: {v.Title} | Genre: {v.Genre} | Year: {v.ReleaseYear} | Price: {v.Price}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press Enter to return...");
            Console.ReadLine();
            FanMenu(fan);
        }

        private static void FanPurchaseVideo(Fan fan)
        {
            Console.Clear();
            Console.WriteLine("===== PURCHASE VIDEO =====");
            Console.Write("Enter Video ID: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadLine();
                FanMenu(fan);
                return;
            }

            Video video = Program.videoTree.Search(id);

            if (video == null)
            {
                Console.WriteLine("Video not found.");
                Console.ReadLine();
                FanMenu(fan);
                return;
            }

            Console.WriteLine($"You are purchasing: {video.Title} for {video.Price}");
            Console.Write("Confirm purchase? (Y/N): ");
            string confirm = Console.ReadLine().ToUpper();

            if (confirm != "Y")
            {
                FanMenu(fan);
                return;
            }

            Database.AddPurchase(fan.ID, video);

            Console.WriteLine("Purchase recorded successfully.");
            Console.ReadLine();
            FanMenu(fan);
        }

        private static void FanViewPurchasedVideos(Fan fan)
        {
            Console.Clear();
            Console.WriteLine("===== YOUR PURCHASED VIDEOS =====");

            List<Video> videos = Database.GetPurchasedVideosByFan(fan.ID);

            if (videos.Count == 0)
            {
                Console.WriteLine("You have not purchased any videos yet.");
            }
            else
            {
                foreach (var v in videos)
                {
                    Console.WriteLine($"ID: {v.VideoID} | Title: {v.Title} | Genre: {v.Genre} | Year: {v.ReleaseYear} | Price Paid: {v.Price}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press Enter to return...");
            Console.ReadLine();
            FanMenu(fan);
        }

        // =======================
        // ARTIST LOGIN
        // =======================
        private static void ArtistLogin()
        {
            Console.Clear();
            Console.WriteLine("===== ARTIST LOGIN =====");

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            Artist artist = Database.LoginArtist(email);

            if (artist == null)
            {
                Console.WriteLine("Artist not found.");
                Console.ReadLine();
                MainMenu();
                return;
            }

            bool ok = Authentication.VerifyPassword(password, artist.PasswordHash, artist.Salt);

            if (!ok)
            {
                Console.WriteLine("Invalid password.");
                Console.ReadLine();
                MainMenu();
                return;
            }

            ArtistMenu(artist);
        }

        // =======================
        // ARTIST MENU
        // =======================
        private static void ArtistMenu(Artist artist)
        {
            Console.Clear();
            Console.WriteLine($"===== ARTIST MENU ({artist.Name}) =====");
            Console.WriteLine("1. Upload Video");
            Console.WriteLine("2. View My Videos");
            Console.WriteLine("3. View Revenue Summary");
            Console.WriteLine("4. Edit My Video");
            Console.WriteLine("5. Delete My Video");
            Console.WriteLine("6. Logout");
            Console.Write("Choose: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    UploadVideo(artist);
                    break;
                case "2":
                    ViewArtistVideos(artist);
                    break;
                case "3":
                    ArtistRevenueSummary(artist);
                    break;
                case "4":
                    ArtistEditVideo(artist);
                    break;
                case "5":
                    ArtistDeleteVideo(artist);
                    break;
                case "6":
                    MainMenu();
                    break;
                default:
                    ArtistMenu(artist);
                    break;
            }
        }

        private static void UploadVideo(Artist artist)
        {
            Console.Clear();
            Console.WriteLine("===== UPLOAD VIDEO =====");

            Console.Write("Title: ");
            string title = Console.ReadLine();

            Console.Write("Genre: ");
            string genre = Console.ReadLine();

            Console.Write("Release Year: ");
            int year = int.Parse(Console.ReadLine());

            Console.Write("Price: ");
            double price = double.Parse(Console.ReadLine());

            Video video = new Video
            {
                Title = title,
                Genre = genre,
                ReleaseYear = year,
                ArtistID = artist.ID,
                Price = price
            };

            Database.AddVideo(video);

            var allVideos = Database.LoadAllVideos();
            var latest = allVideos[allVideos.Count - 1];

            Program.videoTree.Insert(latest);

            Console.WriteLine("Video uploaded successfully!");
            Console.ReadLine();
            ArtistMenu(artist);
        }

        private static void ViewArtistVideos(Artist artist)
        {
            Console.Clear();
            Console.WriteLine("===== MY VIDEOS =====");

            List<Video> videos = Database.LoadVideosByArtist(artist.ID);

            if (videos.Count == 0)
            {
                Console.WriteLine("You have not uploaded any videos yet.");
            }
            else
            {
                foreach (var v in videos)
                {
                    Console.WriteLine($"ID: {v.VideoID} | Title: {v.Title} | Genre: {v.Genre} | Year: {v.ReleaseYear} | Price: {v.Price}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press Enter to return...");
            Console.ReadLine();
            ArtistMenu(artist);
        }

        private static void ArtistRevenueSummary(Artist artist)
        {
            Console.Clear();
            Console.WriteLine("===== REVENUE SUMMARY =====");

            Artist fresh = Database.GetArtistById(artist.ID);
            Console.WriteLine($"Total Earnings: {fresh.TotalEarnings}");

            List<Video> videos = Database.LoadVideosByArtist(artist.ID);

            if (videos.Count == 0)
            {
                Console.WriteLine("No videos uploaded yet.");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("Your Videos:");
                foreach (var v in videos)
                {
                    Console.WriteLine($"ID: {v.VideoID} | Title: {v.Title} | Price: {v.Price}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press Enter to return...");
            Console.ReadLine();
            ArtistMenu(artist);
        }

        private static void ArtistEditVideo(Artist artist)
        {
            Console.Clear();
            Console.WriteLine("===== EDIT MY VIDEO =====");

            List<Video> videos = Database.LoadVideosByArtist(artist.ID);

            if (videos.Count == 0)
            {
                Console.WriteLine("You have no videos to edit.");
                Console.ReadLine();
                ArtistMenu(artist);
                return;
            }

            foreach (var v in videos)
            {
                Console.WriteLine($"ID: {v.VideoID} | Title: {v.Title} | Genre: {v.Genre} | Year: {v.ReleaseYear} | Price: {v.Price}");
            }

            Console.Write("Enter Video ID to edit: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadLine();
                ArtistMenu(artist);
                return;
            }

            Video target = videos.Find(v => v.VideoID == id);
            if (target == null)
            {
                Console.WriteLine("Video not found or not yours.");
                Console.ReadLine();
                ArtistMenu(artist);
                return;
            }

            Console.Write($"New Title (leave blank to keep '{target.Title}'): ");
            string newTitle = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newTitle))
                target.Title = newTitle;

            Console.Write($"New Genre (leave blank to keep '{target.Genre}'): ");
            string newGenre = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(newGenre))
                target.Genre = newGenre;

            Console.Write($"New Year (leave blank to keep '{target.ReleaseYear}'): ");
            string yearInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(yearInput) && int.TryParse(yearInput, out int newYear))
                target.ReleaseYear = newYear;

            Console.Write($"New Price (leave blank to keep '{target.Price}'): ");
            string priceInput = Console.ReadLine();
            if (!string.IsNullOrWhiteSpace(priceInput) && double.TryParse(priceInput, out double newPrice))
                target.Price = newPrice;

            Database.UpdateVideo(target);

            Program.videoTree.Clear();
            var allVideos = Database.LoadAllVideos();
            foreach (var v in allVideos)
                Program.videoTree.Insert(v);

            Console.WriteLine("Video updated successfully.");
            Console.ReadLine();
            ArtistMenu(artist);
        }

        private static void ArtistDeleteVideo(Artist artist)
        {
            Console.Clear();
            Console.WriteLine("===== DELETE MY VIDEO =====");

            List<Video> videos = Database.LoadVideosByArtist(artist.ID);

            if (videos.Count == 0)
            {
                Console.WriteLine("You have no videos to delete.");
                Console.ReadLine();
                ArtistMenu(artist);
                return;
            }

            foreach (var v in videos)
            {
                Console.WriteLine($"ID: {v.VideoID} | Title: {v.Title} | Genre: {v.Genre} | Year: {v.ReleaseYear} | Price: {v.Price}");
            }

            Console.Write("Enter Video ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadLine();
                ArtistMenu(artist);
                return;
            }

            Video target = videos.Find(v => v.VideoID == id);
            if (target == null)
            {
                Console.WriteLine("Video not found or not yours.");
                Console.ReadLine();
                ArtistMenu(artist);
                return;
            }

            Database.DeleteVideo(id);

            Program.videoTree.Clear();
            var allVideos = Database.LoadAllVideos();
            foreach (var v in allVideos)
                Program.videoTree.Insert(v);

            Console.WriteLine("Video deleted successfully.");
            Console.ReadLine();
            ArtistMenu(artist);
        }

        // =======================
        // ADMIN LOGIN
        // =======================
        private static void AdminLogin()
        {
            Console.Clear();
            Console.WriteLine("===== ADMIN LOGIN =====");

            Console.Write("Email: ");
            string email = Console.ReadLine();

            Console.Write("Password: ");
            string password = Console.ReadLine();

            Admin admin = Database.LoginAdmin(email);

            if (admin == null)
            {
                Console.WriteLine("Admin not found.");
                Console.ReadLine();
                MainMenu();
                return;
            }

            bool ok = Authentication.VerifyPassword(password, admin.PasswordHash, admin.Salt);

            if (!ok)
            {
                Console.WriteLine("Invalid password.");
                Console.ReadLine();
                MainMenu();
                return;
            }

            AdminMenu(admin);
        }

        // =======================
        // ADMIN MENU
        // =======================
        private static void AdminMenu(Admin admin)
        {
            Console.Clear();
            Console.WriteLine($"===== ADMIN MENU ({admin.Name}) =====");
            Console.WriteLine("1. View All Videos");
            Console.WriteLine("2. View All Artists + Earnings");
            Console.WriteLine("3. Delete Any Video by ID");
            Console.WriteLine("4. Logout");
            Console.Write("Choose: ");

            string choice = Console.ReadLine();

            switch (choice)
            {
                case "1":
                    ViewAllVideos(() => AdminMenu(admin));
                    break;
                case "2":
                    AdminViewArtists(admin);
                    break;
                case "3":
                    AdminDeleteVideo(admin);
                    break;
                case "4":
                    MainMenu();
                    break;
                default:
                    AdminMenu(admin);
                    break;
            }
        }

        private static void AdminViewArtists(Admin admin)
        {
            Console.Clear();
            Console.WriteLine("===== ALL ARTISTS & EARNINGS =====");

            List<Artist> artists = Database.LoadAllArtists();

            if (artists.Count == 0)
            {
                Console.WriteLine("No artists found.");
            }
            else
            {
                foreach (var a in artists)
                {
                    Console.WriteLine($"ID: {a.ID} | Name: {a.Name} | Email: {a.Email} | Total Earnings: {a.TotalEarnings}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Press Enter to return...");
            Console.ReadLine();
            AdminMenu(admin);
        }

        private static void AdminDeleteVideo(Admin admin)
        {
            Console.Clear();
            Console.WriteLine("===== DELETE VIDEO (ADMIN) =====");
            Console.Write("Enter Video ID to delete: ");
            if (!int.TryParse(Console.ReadLine(), out int id))
            {
                Console.WriteLine("Invalid ID.");
                Console.ReadLine();
                AdminMenu(admin);
                return;
            }

            Database.DeleteVideo(id);

            Program.videoTree.Clear();
            var videos = Database.LoadAllVideos();
            foreach (var v in videos)
                Program.videoTree.Insert(v);

            Console.WriteLine("Video deleted (if it existed).");
            Console.ReadLine();
            AdminMenu(admin);
        }
    }
}
