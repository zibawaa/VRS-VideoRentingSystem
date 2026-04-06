using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.DataStructures;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Core;

public sealed class VideoStore
{
    private readonly AvlTitleIndex _titleIndex;
    private readonly IdHashIndex _idIndex;
    private readonly UserRentalsMap _userRentalsMap;
    private readonly IVideoRepository? _repository;
    private readonly IRentalRepository? _rentalRepository;

    public VideoStore(IVideoRepository? repository = null, IRentalRepository? rentalRepository = null)
    {
        _titleIndex = new AvlTitleIndex();
        _idIndex = new IdHashIndex();
        _userRentalsMap = new UserRentalsMap();
        _repository = repository;
        _rentalRepository = rentalRepository;
        // AVL for title search and sorted walk, hash table for id, map for who rented what
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

        _repository?.DeleteVideo(videoId);
        return true;
    }

    public Video[] SearchByTitle(string title)
    {
        return _titleIndex.SearchByTitle(title);
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

        video.SetRented(true);
        _repository?.UpsertVideo(video);
        _userRentalsMap.AddRental(userId, videoId);
        // memory structures and optional sqlite both record checkout state

        Rental rental = new Rental(userId, videoId, DateTime.UtcNow);
        _rentalRepository?.InsertRental(rental);
        // rental row stores utc stamp for the ui countdown logic

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

        if (persist)
        {
            _repository?.UpsertVideo(video);
        }

        return true;
    }
}
