// Mediator between the UI, SQLite repositories, and the custom indexes. Anything that mutates videos goes through here
// so we never forget to update the AVL tree *and* the hash table at the same time.
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

    // Repositories are optional so unit tests can pass null and stay fast/in-memory only.
    public VideoStore(IVideoRepository? repository = null, IRentalRepository? rentalRepository = null)
    {
        _titleIndex = new AvlTitleIndex();
        _idIndex = new IdHashIndex();
        _userRentalsMap = new UserRentalsMap();
        _repository = repository;
        _rentalRepository = rentalRepository;
    }

    public int Count => _idIndex.Count;

    public void LoadFromRepository()
    {
        if (_repository != null)
        {
            Video[] videos = _repository.LoadAllVideos();
            for (int i = 0; i < videos.Length; i++)
            {
                // false = do not write back to SQL again while we are only hydrating RAM from disk.
                AddVideo(videos[i], false);
            }
        }

        if (_rentalRepository != null)
        {
            Rental[] rentals = _rentalRepository.LoadAllRentals();
            for (int i = 0; i < rentals.Length; i++)
            {
                _userRentalsMap.AddRental(rentals[i].UserId, rentals[i].VideoId);
            }
        }
    }

    public bool AddVideo(Video video)
    {
        return AddVideo(video, true);
    }

    public bool RemoveVideo(int videoId)
    {
        if (!_idIndex.TryGetValue(videoId, out Video? video) || video == null)
        {
            return false;
        }

        bool removedFromId = _idIndex.Remove(videoId);
        bool removedFromTitle = _titleIndex.Remove(video);

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

    // Sort order comes "for free" from an in-order walk of the AVL — that is why we picked a tree keyed by title.
    public Video[] DisplayAllVideos()
    {
        return _titleIndex.InOrderTraversal();
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

        Rental rental = new Rental(userId, videoId, DateTime.UtcNow);
        _rentalRepository?.InsertRental(rental);

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
        }

        bool mapRemoved = _userRentalsMap.RemoveRental(userId, videoId);

        if (!mapRemoved)
        {
            // Stops user B returning a tape that user A checked out — the map is the source of truth for ownership.
            return false;
        }

        video.SetRented(false);
        _repository?.UpsertVideo(video);

        _rentalRepository?.DeleteRental(userId, videoId);

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
            }
        }

        return userVideos;
    }

    // Small wrapper so MainForm does not talk to SQL directly — keeps the UI dumb.
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
        }

        bool addedToTitle = _titleIndex.Add(video);
        if (!addedToTitle)
        {
            _idIndex.Remove(video.VideoId);
            return false;
        }

        if (persist)
        {
            _repository?.UpsertVideo(video);
        }

        return true;
    }
}
