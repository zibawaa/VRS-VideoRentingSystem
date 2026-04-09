using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.DataStructures;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Core;

public sealed class VideoStore
{
    private readonly AvlTitleIndex _titleIndex;
    private readonly IdHashIndex _idIndex;
    private readonly UserRentalsMap _userRentalsMap;
    private readonly TitleKeywordIndex _keywordIndex;
    private readonly PublisherVideoIndex _publisherIndex;
    private readonly IVideoRepository? _repository;
    private readonly IRentalRepository? _rentalRepository;

    public VideoStore(IVideoRepository? repository = null, IRentalRepository? rentalRepository = null)
    {
        _titleIndex = new AvlTitleIndex();
        _idIndex = new IdHashIndex();
        _userRentalsMap = new UserRentalsMap();
        _keywordIndex = new TitleKeywordIndex();
        _publisherIndex = new PublisherVideoIndex();
        _repository = repository;
        _rentalRepository = rentalRepository;
        // AVL for title order, hash for ids, rental map for users, and two marketplace indexes
    }

    public int Count => _idIndex.Count;
    // video count follows the id index because every tape must have a unique id

    public void LoadFromRepository()
    {
        if (_repository != null)
        {
            Video[] videos = _repository.LoadAllVideos();
            for (int i = 0; i < videos.Length; i++)
            {
                AddVideo(videos[i], false);
                // false skips Upsert so startup does not rewrite every row back to sqlite
            }
        }

        if (_rentalRepository != null)
        {
            Rental[] rentals = _rentalRepository.LoadAllRentals();
            for (int i = 0; i < rentals.Length; i++)
            {
                _userRentalsMap.AddRental(rentals[i].UserId, rentals[i].VideoId);
                // rebuild the adjacency map from persisted rental rows only
            }
        }
    }

    public bool AddVideo(Video video)
    {
        return AddVideo(video, true);
        // public entry point always persists when a backing repo exists
    }

    public bool AddVideo(Video video, User actor)
    {
        if (actor.Role == UserRole.Customer)
        {
            return false;
        }

        if (actor.Role == UserRole.Publisher && video.OwnerPublisherId != actor.UserId)
        {
            return false;
        }
        // publisher accounts can only upload catalogue rows they own

        return AddVideo(video, true);
    }

    public bool RemoveVideo(int videoId)
    {
        if (!_idIndex.TryGetValue(videoId, out Video? video) || video == null)
        {
            return false;
            // nothing to delete
        }

        bool removedFromId = _idIndex.Remove(videoId);
        bool removedFromTitle = _titleIndex.Remove(video);
        // both structures must stay in sync for search and id lookup

        if (!removedFromId || !removedFromTitle)
        {
            return false;
        }

        _keywordIndex.Remove(video);
        _publisherIndex.RemoveVideo(video.OwnerPublisherId, video.VideoId);
        // remove side indexes only after primary structures succeed

        _repository?.DeleteVideo(videoId);
        return true;
    }

    public bool RemoveVideo(int videoId, User actor)
    {
        if (!_idIndex.TryGetValue(videoId, out Video? video) || video == null)
        {
            return false;
        }

        if (actor.Role == UserRole.Customer)
        {
            return false;
        }

        if (actor.Role == UserRole.Publisher && video.OwnerPublisherId != actor.UserId)
        {
            return false;
        }
        // admins can remove any title, publishers can remove only their own

        return RemoveVideo(videoId);
    }

    /// <summary>
    /// Updates a video's mutable fields and re-indexes title/keyword structures.
    /// The video must already exist in the id index.
    /// </summary>
    public bool UpdateVideo(int videoId, string title, string genre, int releaseYear,
        int ownerPublisherId, VideoType type, decimal rentalPrice, int rentalHours, bool isPublished)
    {
        if (!_idIndex.TryGetValue(videoId, out Video? video) || video == null)
        {
            return false;
        }

        // capture old title before mutation so we can re-index if it changed
        string oldTitle = video.Title;

        // remove from keyword and title indexes before the in-place mutation
        _keywordIndex.Remove(video);
        _titleIndex.Remove(video);

        video.UpdateDetails(title, genre, releaseYear, ownerPublisherId, type, rentalPrice, rentalHours, isPublished);

        // re-insert into title and keyword indexes with the updated fields
        _titleIndex.Add(video);
        _keywordIndex.Add(video);

        _repository?.UpsertVideo(video);
        return true;
    }

    /// <summary>
    /// Flips the IsPublished flag on a video without touching other fields.
    /// </summary>
    public bool TogglePublish(int videoId)
    {
        if (!_idIndex.TryGetValue(videoId, out Video? video) || video == null)
        {
            return false;
        }

        // toggle by calling UpdateDetails with the inverted publish flag
        video.UpdateDetails(video.Title, video.Genre, video.ReleaseYear,
            video.OwnerPublisherId, video.Type, video.RentalPrice, video.RentalHours, !video.IsPublished);

        _repository?.UpsertVideo(video);
        return true;
    }

    /// <summary>
    /// Returns all active rental records across every user by delegating to the rental repository.
    /// </summary>
    public Rental[] GetAllRentals()
    {
        if (_rentalRepository == null)
        {
            return [];
        }

        return _rentalRepository.LoadAllRentals();
    }

    public Video[] SearchByTitle(string title)
    {
        return _titleIndex.SearchByTitle(title);
    }

    // searches the keyword index for exact multi-word matches first
    public Video[] SearchByKeyword(string keyword)
    {
        int[] ids = _keywordIndex.SearchVideoIds(keyword);
        return ResolveIds(ids);
    }

    // falls back to Levenshtein-based fuzzy matching when exact search returns nothing
    public Video[] FuzzySearchByKeyword(string keyword, int maxDistance = 2)
    {
        int[] ids = _keywordIndex.FuzzySearchVideoIds(keyword, maxDistance);
        return ResolveIds(ids);
    }

    // converts an array of video IDs into resolved Video objects via the hash index
    private Video[] ResolveIds(int[] ids)
    {
        if (ids.Length == 0) return [];

        Video[] found = new Video[ids.Length];
        int index = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            if (_idIndex.TryGetValue(ids[i], out Video? video) && video != null)
            {
                found[index++] = video;
            }
        }

        if (index == found.Length) return found;

        Video[] compact = new Video[index];
        for (int i = 0; i < index; i++) compact[i] = found[i];
        return compact;
    }

    public bool TrySearchById(int videoId, out Video? video)
    {
        return _idIndex.TryGetValue(videoId, out video);
    }

    public Video[] DisplayAllVideos()
    {
        return _titleIndex.InOrderTraversal();
        // coursework requirement: ordered by normalised title via inorder AVL walk
    }

    public Video[] DisplayPublishedVideos()
    {
        Video[] all = _titleIndex.InOrderTraversal();
        if (all.Length == 0)
        {
            return [];
        }

        Video[] published = new Video[all.Length];
        int index = 0;
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i].IsPublished)
            {
                published[index++] = all[i];
            }
        }

        if (index == published.Length)
        {
            return published;
        }

        Video[] compact = new Video[index];
        for (int i = 0; i < index; i++)
        {
            compact[i] = published[i];
        }

        return compact;
    }

    public Video[] GetPublisherVideos(int publisherUserId)
    {
        int[] ids = _publisherIndex.GetVideoIds(publisherUserId);
        if (ids.Length == 0)
        {
            return [];
        }

        Video[] videos = new Video[ids.Length];
        int index = 0;
        for (int i = 0; i < ids.Length; i++)
        {
            if (_idIndex.TryGetValue(ids[i], out Video? video) && video != null)
            {
                videos[index++] = video;
            }
        }

        if (index == videos.Length)
        {
            return videos;
        }

        Video[] compact = new Video[index];
        for (int i = 0; i < index; i++)
        {
            compact[i] = videos[i];
        }

        return compact;
    }

    public Video[] FilterCatalog(string? keyword, string? genre, decimal? maxPrice)
    {
        Video[] baseline = string.IsNullOrWhiteSpace(keyword)
            ? DisplayPublishedVideos()
            : SearchByKeyword(keyword);
        // keyword index narrows candidates first, then exact filters trim the set

        return ApplyFilters(baseline, genre, maxPrice);
    }

    // same as FilterCatalog but uses fuzzy (Levenshtein) matching on the keyword
    public Video[] FuzzyFilterCatalog(string keyword, string? genre, decimal? maxPrice)
    {
        Video[] baseline = FuzzySearchByKeyword(keyword);
        return ApplyFilters(baseline, genre, maxPrice);
    }

    // applies genre and price filters to a baseline set of videos
    private static Video[] ApplyFilters(Video[] baseline, string? genre, decimal? maxPrice)
    {
        if (baseline.Length == 0) return [];

        string? normalizedGenre = string.IsNullOrWhiteSpace(genre) ? null : genre.Trim();
        Video[] filtered = new Video[baseline.Length];
        int index = 0;

        for (int i = 0; i < baseline.Length; i++)
        {
            Video v = baseline[i];
            if (!v.IsPublished) continue;
            if (normalizedGenre != null && !string.Equals(v.Genre, normalizedGenre, StringComparison.OrdinalIgnoreCase)) continue;
            if (maxPrice.HasValue && v.RentalPrice > maxPrice.Value) continue;
            filtered[index++] = v;
        }

        Video[] compact = new Video[index];
        for (int i = 0; i < index; i++) compact[i] = filtered[i];
        return compact;
    }

    public bool RentVideo(int videoId, int userId)
    {
        if (!_idIndex.TryGetValue(videoId, out Video? video) || video == null)
        {
            return false;
        }

        if (video.IsRented)
        {
            return false;
        }

        if (!video.IsPublished)
        {
            return false;
        }
        // unpublished catalogue rows should not be rentable from customer views

        video.SetRented(true);
        _repository?.UpsertVideo(video);
        _userRentalsMap.AddRental(userId, videoId);
        // memory structures and optional sqlite both record checkout state

        DateTime rentDate = DateTime.UtcNow;
        DateTime expiry = rentDate.AddHours(video.RentalHours);
        Rental rental = new Rental(userId, videoId, rentDate, expiry, video.RentalPrice);
        _rentalRepository?.InsertRental(rental);
        // rental row persists billing and expiry for pay-per-title access windows

        return true;
    }

    public bool ReturnVideo(int videoId, int userId)
    {
        if (!_idIndex.TryGetValue(videoId, out Video? video) || video == null)
        {
            return false;
        }

        if (!video.IsRented)
        {
            return false;
            // already on shelf in model
        }

        bool mapRemoved = _userRentalsMap.RemoveRental(userId, videoId);
        // must be the recorded renter; otherwise leave IsRented alone
        if (!mapRemoved)
        {
            return false;
        }

        video.SetRented(false);
        _repository?.UpsertVideo(video);
        _rentalRepository?.DeleteRental(userId, videoId);
        // clear flags and remove the composite rental key from sqlite

        return true;
    }

    public Video[] GetUserRentedVideos(int userId)
    {
        int[] rentedVideoIds = _userRentalsMap.GetUserRentals(userId);
        Video[] userVideos = new Video[rentedVideoIds.Length];

        for (int i = 0; i < rentedVideoIds.Length; i++)
        {
            if (_idIndex.TryGetValue(rentedVideoIds[i], out Video? rentedVideo) && rentedVideo != null)
            {
                userVideos[i] = rentedVideo;
                // slot may stay null if the tape was deleted without map cleanup; coursework assumes consistency
            }
        }

        return userVideos;
    }

    public bool TryGetRentDate(int userId, int videoId, out DateTime rentDate)
    {
        rentDate = default;
        if (_rentalRepository == null)
        {
            return false;
        }

        return _rentalRepository.TryGetRentDate(userId, videoId, out rentDate);
    }

    public bool TryGetRentalInfo(int userId, int videoId, out DateTime rentDate, out DateTime expiryUtc, out decimal paidAmount)
    {
        rentDate = default;
        expiryUtc = default;
        paidAmount = 0m;
        if (_rentalRepository == null)
        {
            return false;
        }

        return _rentalRepository.TryGetRentalInfo(userId, videoId, out rentDate, out expiryUtc, out paidAmount);
    }

    private bool AddVideo(Video video, bool persist)
    {
        if (!_idIndex.Add(video))
        {
            return false;
            // duplicate id rejected by hash table
        }

        bool addedToTitle = _titleIndex.Add(video);
        if (!addedToTitle)
        {
            _idIndex.Remove(video.VideoId);
            return false;
            // rollback id insert if avl chain failed for any reason
        }

        _keywordIndex.Add(video);
        _publisherIndex.AddVideo(video.OwnerPublisherId, video.VideoId);
        // keep keyword and owner lookup indexes aligned with id and title indexes

        if (persist)
        {
            _repository?.UpsertVideo(video);
        }

        return true;
    }
}
