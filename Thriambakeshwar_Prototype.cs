using System;

class Program
{
    static string[] movies =
    {
        "Dune: Part Two",
        "Oppenheimer",
        "The Dark Knight",
        "Interstellar",
        "Parasite"
    };

    static void Main()
    {
        bool isRunning = true;

        while (isRunning)
        {
            Console.Clear();
            DisplayHeader();
            DisplayMenu();

            Console.Write("Choose an option: ");
            string option = Console.ReadLine();

            switch (option)
            {
                case "1":
                    ShowMovies();
                    break;

                case "2":
                    RentMovie();
                    break;

                case "3":
                    ReturnMovie();
                    break;

                case "4":
                    ShowAccount();
                    break;

                case "0":
                    Console.WriteLine("\nClosing the prototype...");
                    isRunning = false;
                    break;

                default:
                    Console.WriteLine("\nInvalid choice. Please try again.");
                    WaitForUser();
                    break;
            }
        }
    }

    static void DisplayHeader()
    {
        Console.WriteLine("========================================");
        Console.WriteLine("        VIDEO RENTAL SYSTEM");
        Console.WriteLine("        Prototype by Thriambakeshwar");
        Console.WriteLine("========================================\n");
    }

    static void DisplayMenu()
    {
        Console.WriteLine("1. Browse Movies");
        Console.WriteLine("2. Rent a Movie");
        Console.WriteLine("3. Return a Movie");
        Console.WriteLine("4. View Account");
        Console.WriteLine("0. Exit");
        Console.WriteLine();
    }

    static void ShowMovies()
    {
        Console.Clear();
        DisplayHeader();

        Console.WriteLine("Available Movies:\n");

        for (int i = 0; i < movies.Length; i++)
        {
            Console.WriteLine((i + 1) + ". " + movies[i]);
        }

        Console.WriteLine();
        WaitForUser();
    }

    static void RentMovie()
    {
        Console.Clear();
        DisplayHeader();

        Console.Write("Enter the movie name you want to rent: ");
        string movieName = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(movieName))
        {
            Console.WriteLine("\nMovie name cannot be empty.");
        }
        else
        {
            Console.WriteLine("\nYou selected: " + movieName);
            Console.WriteLine("The rental request has been recorded in this prototype.");
        }

        Console.WriteLine();
        WaitForUser();
    }

    static void ReturnMovie()
    {
        Console.Clear();
        DisplayHeader();

        Console.Write("Enter the movie name you want to return: ");
        string movieName = Console.ReadLine();

        if (string.IsNullOrWhiteSpace(movieName))
        {
            Console.WriteLine("\nMovie name cannot be empty.");
        }
        else
        {
            Console.WriteLine("\nYou selected: " + movieName);
            Console.WriteLine("The return request has been recorded in this prototype.");
        }

        Console.WriteLine();
        WaitForUser();
    }

    static void ShowAccount()
    {
        Console.Clear();
        DisplayHeader();

        Console.WriteLine("Account Details\n");
        Console.WriteLine("Name: Demo User");
        Console.WriteLine("Membership Type: Standard");
        Console.WriteLine("Current Rentals: 2");
        Console.WriteLine("Note: This is only a prototype version, so no database is connected yet.");

        Console.WriteLine();
        WaitForUser();
    }

    static void WaitForUser()
    {
        Console.WriteLine("Press Enter to continue...");
        Console.ReadLine();
    }
}
