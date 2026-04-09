using System.Text;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Api.Agent;

/// <summary>
/// AI Agent with "arms" — a rule-based natural language assistant that
/// interprets user messages, selects the most appropriate tool, extracts
/// parameters, and executes actions against the live VideoStore/UserStore.
///
/// Architecture follows the classic agent loop:
///   1. Perceive  — tokenise and normalise the user message
///   2. Reason    — classify intent via keyword/pattern matching
///   3. Act       — invoke the matched tool arm with extracted parameters
///   4. Respond   — return a human-readable result to the caller
/// </summary>
public sealed class AgentService
{
    // each tool is an "arm" the agent can use to interact with the system
    private readonly AgentTool[] _tools;

    public AgentService()
    {
        _tools = BuildToolArms();
    }

    /// <summary>
    /// Main entry point: takes a raw user message and an execution context,
    /// runs the agent loop, and returns a natural-language response.
    /// </summary>
    public string ProcessMessage(string userMessage, AgentContext context)
    {
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            return "I didn't catch that. Try asking me to search for a movie, recommend something, or rent a title.";
        }

        // PERCEIVE: normalise input to lowercase tokens for intent matching
        string normalised = userMessage.Trim().ToLowerInvariant();
        string[] tokens = normalised.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        // REASON: walk through intent rules to find the best matching tool
        (AgentTool? tool, string[] args) = ClassifyIntent(normalised, tokens);

        if (tool == null)
        {
            return BuildHelpResponse();
        }

        // ACT: execute the selected tool arm within the store context
        try
        {
            return tool.Execute(context, args);
        }
        catch (Exception ex)
        {
            return $"Something went wrong while I was working on that: {ex.Message}";
        }
    }

    /// <summary>
    /// Returns a list of all registered tool descriptions so the UI
    /// can show what the agent is capable of.
    /// </summary>
    public string[] GetCapabilities()
    {
        string[] caps = new string[_tools.Length];
        for (int i = 0; i < _tools.Length; i++)
        {
            caps[i] = $"{_tools[i].Name}: {_tools[i].Description}";
        }
        return caps;
    }

    /* ═══════════════════════════════════════════════════════════
       INTENT CLASSIFICATION — pattern/keyword matching engine
       ═══════════════════════════════════════════════════════════ */

    private (AgentTool? tool, string[] args) ClassifyIntent(string message, string[] tokens)
    {
        // greeting / help intent
        if (ContainsAny(message, "hello", "hi ", "hey", "help", "what can you do"))
        {
            return (FindTool("help"), Array.Empty<string>());
        }

        // rent intent — "rent <title>"
        if (ContainsAny(message, "rent ", "i want to rent", "i'd like to rent", "can i rent"))
        {
            string title = ExtractAfter(message, "rent ");
            return (FindTool("rent"), new[] { title });
        }

        // return intent — "return <title>"
        if (ContainsAny(message, "return ", "give back", "send back"))
        {
            string title = ExtractAfter(message, "return ");
            if (string.IsNullOrEmpty(title)) title = ExtractAfter(message, "give back ");
            if (string.IsNullOrEmpty(title)) title = ExtractAfter(message, "send back ");
            return (FindTool("return"), new[] { title });
        }

        // my rentals intent
        if (ContainsAny(message, "my rental", "my rented", "what am i renting", "what have i rented", "active rental"))
        {
            return (FindTool("my-rentals"), Array.Empty<string>());
        }

        // recommend intent — look for genre or "cheap" / "affordable"
        if (ContainsAny(message, "recommend", "suggest", "what should i watch", "something good", "pick for me"))
        {
            string hint = ExtractGenreHint(message);
            return (FindTool("recommend"), new[] { hint });
        }

        // search by genre
        if (ContainsAny(message, "genre", "show me", "find me", "browse"))
        {
            string genre = ExtractGenreHint(message);
            return (FindTool("search"), new[] { "", genre, "" });
        }

        // search / find intent — "search <keyword>" or "find <keyword>"
        if (ContainsAny(message, "search", "find", "look for", "look up", "any movies about"))
        {
            string keyword = ExtractAfter(message, "search ");
            if (string.IsNullOrEmpty(keyword)) keyword = ExtractAfter(message, "find ");
            if (string.IsNullOrEmpty(keyword)) keyword = ExtractAfter(message, "look for ");
            if (string.IsNullOrEmpty(keyword)) keyword = ExtractAfter(message, "about ");
            return (FindTool("search"), new[] { keyword, "", "" });
        }

        // stats / overview
        if (ContainsAny(message, "stats", "statistics", "how many", "overview", "catalog size", "catalogue size"))
        {
            return (FindTool("stats"), Array.Empty<string>());
        }

        // cheap / affordable / price filter
        if (ContainsAny(message, "cheap", "affordable", "budget", "under"))
        {
            string maxPrice = ExtractPriceHint(message);
            return (FindTool("search"), new[] { "", "", maxPrice });
        }

        // no match — show help
        return (null, Array.Empty<string>());
    }

    /* ═══════════════════════════════════════════════════════════
       TOOL ARM DEFINITIONS — each arm wraps a VideoStore action
       ═══════════════════════════════════════════════════════════ */

    private AgentTool[] BuildToolArms()
    {
        return new AgentTool[]
        {
            new()
            {
                Name = "help",
                Description = "Show what the agent can do",
                Execute = (_, _) => BuildHelpResponse()
            },

            new()
            {
                Name = "search",
                Description = "Search the catalog by keyword, genre, or max price",
                Execute = (ctx, args) =>
                {
                    // args: [keyword, genre, maxPrice]
                    string? keyword = args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]) ? args[0] : null;
                    string? genre = args.Length > 1 && !string.IsNullOrWhiteSpace(args[1]) ? args[1] : null;
                    decimal? maxPrice = args.Length > 2 && decimal.TryParse(args[2], out decimal p) ? p : null;

                    // if the keyword looks like a genre name, reclassify it as a genre filter
                    if (keyword != null && genre == null)
                    {
                        string? detectedGenre = ExtractKnownGenre(keyword);
                        if (detectedGenre != null)
                        {
                            genre = detectedGenre;
                            keyword = null;
                        }
                    }

                    Video[] results;
                    lock (ctx.Runtime.SyncRoot)
                    {
                        results = ctx.Runtime.VideoStore.FilterCatalog(keyword, genre, maxPrice);
                    }

                    if (results.Length == 0)
                    {
                        return "I couldn't find any titles matching that. Try different keywords or a broader genre.";
                    }

                    // format up to 8 results into a readable list
                    StringBuilder sb = new();
                    sb.AppendLine($"I found {results.Length} title(s):");
                    int cap = Math.Min(results.Length, 8);
                    for (int i = 0; i < cap; i++)
                    {
                        Video v = results[i];
                        string status = v.IsRented ? "rented" : "available";
                        sb.AppendLine($"  • {v.Title} ({v.Genre}, {v.ReleaseYear}) — ${v.RentalPrice:F2}/{v.RentalHours}h [{status}]");
                    }
                    if (results.Length > cap)
                    {
                        sb.AppendLine($"  ...and {results.Length - cap} more.");
                    }
                    return sb.ToString().TrimEnd();
                }
            },

            new()
            {
                Name = "recommend",
                Description = "Recommend a title based on genre preference or budget",
                Execute = (ctx, args) =>
                {
                    string hint = args.Length > 0 ? args[0] : "";
                    bool wantsCheap = ContainsAny(hint, "cheap", "budget", "affordable");

                    // pull all published titles
                    Video[] catalog;
                    lock (ctx.Runtime.SyncRoot)
                    {
                        catalog = ctx.Runtime.VideoStore.FilterCatalog(null, null, null);
                    }

                    if (catalog.Length == 0)
                    {
                        return "The catalog is empty right now — no recommendations available.";
                    }

                    // filter to available-only titles for recommendations
                    Video[] available = Array.FindAll(catalog, v => !v.IsRented);
                    if (available.Length == 0)
                    {
                        return "All titles are currently rented. Check back later!";
                    }

                    // try to match a genre from the hint
                    string? genreMatch = ExtractKnownGenre(hint);

                    Video[] pool = available;
                    if (genreMatch != null)
                    {
                        Video[] genreFiltered = Array.FindAll(available, v =>
                            v.Genre.Equals(genreMatch, StringComparison.OrdinalIgnoreCase));
                        if (genreFiltered.Length > 0) pool = genreFiltered;
                    }

                    // sort by price ascending if they want cheap, otherwise pick a random title
                    if (wantsCheap)
                    {
                        Array.Sort(pool, (a, b) => a.RentalPrice.CompareTo(b.RentalPrice));
                    }
                    else
                    {
                        // shuffle the pool using a simple Fisher-Yates for variety
                        Random rng = new();
                        for (int i = pool.Length - 1; i > 0; i--)
                        {
                            int j = rng.Next(i + 1);
                            (pool[i], pool[j]) = (pool[j], pool[i]);
                        }
                    }

                    // pick the top recommendation
                    Video pick = pool[0];
                    StringBuilder sb = new();
                    sb.AppendLine($"I'd recommend: **{pick.Title}**");
                    sb.AppendLine($"  {pick.Genre} • {pick.Type} • {pick.ReleaseYear}");
                    sb.AppendLine($"  ${pick.RentalPrice:F2} for {pick.RentalHours} hours");
                    if (pool.Length > 1)
                    {
                        sb.AppendLine($"  (Runner-up: {pool[1].Title} — ${pool[1].RentalPrice:F2})");
                    }
                    sb.AppendLine("Say \"rent <title>\" if you'd like to grab it!");
                    return sb.ToString().TrimEnd();
                }
            },

            new()
            {
                Name = "rent",
                Description = "Rent a video by title (requires login)",
                Execute = (ctx, args) =>
                {
                    // must be authenticated to rent
                    if (ctx.Session == null)
                    {
                        return "You need to sign in before I can rent a title for you.";
                    }

                    string titleQuery = args.Length > 0 ? args[0] : "";
                    if (string.IsNullOrWhiteSpace(titleQuery))
                    {
                        return "Which title would you like to rent? Say \"rent <title name>\".";
                    }

                    // search for the title in the catalog
                    Video[] matches;
                    lock (ctx.Runtime.SyncRoot)
                    {
                        matches = ctx.Runtime.VideoStore.FilterCatalog(titleQuery, null, null);
                    }

                    if (matches.Length == 0)
                    {
                        return $"I couldn't find a title matching \"{titleQuery}\". Check the spelling and try again.";
                    }

                    // pick the first available match
                    Video? target = Array.Find(matches, v => !v.IsRented);
                    if (target == null)
                    {
                        return $"All matches for \"{titleQuery}\" are currently rented out.";
                    }

                    bool success;
                    lock (ctx.Runtime.SyncRoot)
                    {
                        success = ctx.Runtime.VideoStore.RentVideo(target.VideoId, ctx.Session.UserId);
                    }

                    if (!success)
                    {
                        return $"I tried to rent \"{target.Title}\" but the system denied it. It may have just been rented by someone else.";
                    }

                    return $"Done! I've rented \"{target.Title}\" for you. ${target.RentalPrice:F2} for {target.RentalHours} hours. Enjoy!";
                }
            },

            new()
            {
                Name = "return",
                Description = "Return a rented video by title (requires login)",
                Execute = (ctx, args) =>
                {
                    // must be authenticated to return
                    if (ctx.Session == null)
                    {
                        return "You need to sign in before I can return a title for you.";
                    }

                    string titleQuery = args.Length > 0 ? args[0] : "";
                    if (string.IsNullOrWhiteSpace(titleQuery))
                    {
                        return "Which title would you like to return? Say \"return <title name>\".";
                    }

                    // get the user's active rentals and find one matching the query
                    Video[] rented;
                    lock (ctx.Runtime.SyncRoot)
                    {
                        rented = ctx.Runtime.VideoStore.GetUserRentedVideos(ctx.Session.UserId);
                    }

                    Video? target = Array.Find(rented, v =>
                        v.Title.Contains(titleQuery, StringComparison.OrdinalIgnoreCase));

                    if (target == null)
                    {
                        return $"I don't see \"{titleQuery}\" in your active rentals. Say \"my rentals\" to see what you have.";
                    }

                    bool success;
                    lock (ctx.Runtime.SyncRoot)
                    {
                        success = ctx.Runtime.VideoStore.ReturnVideo(target.VideoId, ctx.Session.UserId);
                    }

                    if (!success)
                    {
                        return $"I tried to return \"{target.Title}\" but something went wrong. Please try again.";
                    }

                    return $"Returned \"{target.Title}\" successfully. Thanks for watching!";
                }
            },

            new()
            {
                Name = "my-rentals",
                Description = "Show the user's currently rented titles",
                Execute = (ctx, _) =>
                {
                    if (ctx.Session == null)
                    {
                        return "You need to sign in to see your rentals.";
                    }

                    Video[] rented;
                    lock (ctx.Runtime.SyncRoot)
                    {
                        rented = ctx.Runtime.VideoStore.GetUserRentedVideos(ctx.Session.UserId);
                    }

                    if (rented.Length == 0)
                    {
                        return "You don't have any active rentals. Browse the catalog or ask me to recommend something!";
                    }

                    StringBuilder sb = new();
                    sb.AppendLine($"You have {rented.Length} active rental(s):");
                    for (int i = 0; i < rented.Length; i++)
                    {
                        Video v = rented[i];

                        // try to get rental metadata (expiry, paid)
                        DateTime rentDate = default;
                        DateTime expiryUtc = default;
                        decimal paid = 0m;
                        lock (ctx.Runtime.SyncRoot)
                        {
                            ctx.Runtime.VideoStore.TryGetRentalInfo(ctx.Session.UserId, v.VideoId, out rentDate, out expiryUtc, out paid);
                        }

                        string timeLeft = expiryUtc > DateTime.UtcNow
                            ? $"{(expiryUtc - DateTime.UtcNow).TotalHours:F0}h left"
                            : "expired";

                        sb.AppendLine($"  • {v.Title} ({v.Genre}) — ${paid:F2} paid, {timeLeft}");
                    }
                    sb.AppendLine("Say \"return <title>\" to return one.");
                    return sb.ToString().TrimEnd();
                }
            },

            new()
            {
                Name = "stats",
                Description = "Show catalog statistics and overview",
                Execute = (ctx, _) =>
                {
                    Video[] all;
                    lock (ctx.Runtime.SyncRoot)
                    {
                        all = ctx.Runtime.VideoStore.DisplayAllVideos();
                    }

                    int total = all.Length;
                    int rented = 0;
                    int published = 0;
                    decimal totalRevenuePotential = 0m;

                    // collect unique genres using a simple scan
                    string[] genres = new string[total];
                    int genreCount = 0;

                    for (int i = 0; i < total; i++)
                    {
                        if (all[i].IsRented) rented++;
                        if (all[i].IsPublished) published++;
                        totalRevenuePotential += all[i].RentalPrice;

                        // track unique genres without using HashSet (coursework constraint)
                        bool found = false;
                        for (int g = 0; g < genreCount; g++)
                        {
                            if (genres[g].Equals(all[i].Genre, StringComparison.OrdinalIgnoreCase))
                            {
                                found = true;
                                break;
                            }
                        }
                        if (!found)
                        {
                            genres[genreCount] = all[i].Genre;
                            genreCount++;
                        }
                    }

                    StringBuilder sb = new();
                    sb.AppendLine("Catalog Overview:");
                    sb.AppendLine($"  Total titles: {total}");
                    sb.AppendLine($"  Published: {published}");
                    sb.AppendLine($"  Currently rented: {rented}");
                    sb.AppendLine($"  Available: {total - rented}");
                    sb.AppendLine($"  Genres: {genreCount} ({string.Join(", ", genres.Take(genreCount))})");
                    sb.AppendLine($"  Total catalog value: ${totalRevenuePotential:F2}");
                    return sb.ToString().TrimEnd();
                }
            }
        };
    }

    /* ═══════════════════════════════════════════════════════════
       HELPER METHODS — text parsing and extraction utilities
       ═══════════════════════════════════════════════════════════ */

    private AgentTool? FindTool(string name)
    {
        for (int i = 0; i < _tools.Length; i++)
        {
            if (_tools[i].Name == name) return _tools[i];
        }
        return null;
    }

    // checks if the message contains any of the given keywords
    private static bool ContainsAny(string text, params string[] keywords)
    {
        for (int i = 0; i < keywords.Length; i++)
        {
            if (text.Contains(keywords[i], StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    // extracts everything after a trigger phrase, trimmed and cleaned
    private static string ExtractAfter(string message, string trigger)
    {
        int idx = message.IndexOf(trigger, StringComparison.OrdinalIgnoreCase);
        if (idx < 0) return "";

        string after = message[(idx + trigger.Length)..].Trim();

        // strip trailing punctuation that might confuse lookups
        after = after.TrimEnd('.', '!', '?', ',');

        // strip common filler words that sit between the trigger and the actual query
        string[] fillers = { "for ", "me ", "some ", "a ", "the " };
        for (int i = 0; i < fillers.Length; i++)
        {
            if (after.StartsWith(fillers[i], StringComparison.OrdinalIgnoreCase))
            {
                after = after[fillers[i].Length..].Trim();
            }
        }

        return after;
    }

    // tries to identify a genre from the user's message
    private static string ExtractGenreHint(string message)
    {
        string[] knownGenres = { "sci-fi", "action", "crime", "documentary", "comedy", "horror", "drama", "thriller", "romance", "animation" };

        for (int i = 0; i < knownGenres.Length; i++)
        {
            if (message.Contains(knownGenres[i], StringComparison.OrdinalIgnoreCase))
                return knownGenres[i];
        }

        // fall back: if they said "cheap" or "affordable", signal that
        if (ContainsAny(message, "cheap", "budget", "affordable")) return "cheap";
        return "";
    }

    // returns the first known genre found in the text, properly capitalised
    private static string? ExtractKnownGenre(string text)
    {
        (string key, string display)[] genres =
        {
            ("sci-fi", "Sci-Fi"), ("action", "Action"), ("crime", "Crime"),
            ("documentary", "Documentary"), ("comedy", "Comedy"), ("horror", "Horror"),
            ("drama", "Drama"), ("thriller", "Thriller"), ("romance", "Romance"),
            ("animation", "Animation")
        };

        for (int i = 0; i < genres.Length; i++)
        {
            if (text.Contains(genres[i].key, StringComparison.OrdinalIgnoreCase))
                return genres[i].display;
        }
        return null;
    }

    // extracts a numeric price from patterns like "under 3", "below 2.50", etc.
    private static string ExtractPriceHint(string message)
    {
        string[] triggers = { "under ", "below ", "less than ", "cheaper than ", "max " };

        for (int i = 0; i < triggers.Length; i++)
        {
            string after = ExtractAfter(message, triggers[i]);
            // strip currency symbols and try to parse the number
            after = after.Replace("£", "").Replace("$", "").Trim();
            string[] parts = after.Split(' ');
            if (parts.Length > 0 && decimal.TryParse(parts[0], out _))
            {
                return parts[0];
            }
        }

        // default to a reasonable "cheap" threshold if no explicit number
        return "3.00";
    }

    // builds the default help / greeting response listing available arms
    private string BuildHelpResponse()
    {
        StringBuilder sb = new();
        sb.AppendLine("Hi! I'm the VRS AI Assistant. Here's what I can do:");
        sb.AppendLine();
        sb.AppendLine("  • \"Search for sci-fi movies\" — browse by keyword or genre");
        sb.AppendLine("  • \"Recommend something\" — I'll pick a title for you");
        sb.AppendLine("  • \"Recommend something cheap\" — budget-friendly picks");
        sb.AppendLine("  • \"Rent Inception\" — rent a title by name");
        sb.AppendLine("  • \"Return Inception\" — return a rented title");
        sb.AppendLine("  • \"My rentals\" — see your active rentals");
        sb.AppendLine("  • \"Stats\" — catalog overview and numbers");
        sb.AppendLine();
        sb.AppendLine("Just type naturally — I'll figure out what you mean!");
        return sb.ToString().TrimEnd();
    }
}
