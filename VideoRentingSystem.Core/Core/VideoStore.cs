using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.DataStructures;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.Core;

public sealed class VideoStore
{
    private readonly AvlTitleIndex _titleIndex;
    private readonly IdHashIndex _idIndex;
    private readonly IVideoRepository? _repository;

    public VideoStore(IVideoRepository? repository = null)
    {
        _titleIndex = new AvlTitleIndex();
        _idIndex = new IdHashIndex();
        _repository = repository;
    }

    public int Count => _idIndex.Count;

    public void LoadFromRepository()
    {
        if (_repository == null)
        {
            return;
        }

        Video[] videos = _repository.LoadAllVideos();
        for (int i = 0; i < videos.Length; i++)
        {
            AddVideo(videos[i], false);
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

    public Video[] DisplayAllVideos()
    {
        return _titleIndex.InOrderTraversal();
    }

    public bool RentVideo(int videoId)
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
        return true;
    }

    public bool ReturnVideo(int videoId)
    {
        if (!_idIndex.TryGetValue(videoId, out Video? video) || video == null)
        {
            return false;
        }

        if (!video.IsRented)
        {
            return false;
        }

        video.SetRented(false);
        _repository?.UpsertVideo(video);
        return true;
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
