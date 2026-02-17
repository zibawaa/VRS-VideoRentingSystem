using VideoRentingSystem.Core.Core;
using VideoRentingSystem.Core.Data;
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Tests;

[TestClass]
public sealed class VideoStoreTests
{
    [TestMethod]
    public void AddVideo_ShouldStoreAndFindByIdAndTitle()
    {
        VideoStore store = new();
        Video video = new(1, "Inception", "Sci-Fi", 2010);

        bool added = store.AddVideo(video);

        Assert.IsTrue(added);
        Assert.IsTrue(store.TrySearchById(1, out Video? foundById));
        Assert.IsNotNull(foundById);
        Assert.AreEqual("Inception", foundById.Title);

        Video[] foundByTitle = store.SearchByTitle("inception");
        Assert.HasCount(1, foundByTitle);
        Assert.AreEqual(1, foundByTitle[0].VideoId);
    }

    [TestMethod]
    public void AddVideo_DuplicateId_ShouldFail()
    {
        VideoStore store = new();
        bool first = store.AddVideo(new Video(1, "Inception", "Sci-Fi", 2010));
        bool second = store.AddVideo(new Video(1, "Interstellar", "Sci-Fi", 2014));

        Assert.IsTrue(first);
        Assert.IsFalse(second);
        Assert.AreEqual(1, store.Count);
    }

    [TestMethod]
    public void DisplayAllVideos_ShouldReturnTitleSortedOrder()
    {
        VideoStore store = new();
        store.AddVideo(new Video(2, "Titanic", "Drama", 1997));
        store.AddVideo(new Video(1, "Avatar", "Sci-Fi", 2009));
        store.AddVideo(new Video(3, "Zodiac", "Thriller", 2007));

        Video[] ordered = store.DisplayAllVideos();

        Assert.HasCount(3, ordered);
        Assert.AreEqual("Avatar", ordered[0].Title);
        Assert.AreEqual("Titanic", ordered[1].Title);
        Assert.AreEqual("Zodiac", ordered[2].Title);
    }

    [TestMethod]
    public void RentAndReturn_ShouldToggleRentalState()
    {
        VideoStore store = new();
        store.AddVideo(new Video(10, "The Matrix", "Sci-Fi", 1999));

        bool rentOk = store.RentVideo(10);
        bool rentAgain = store.RentVideo(10);
        bool returnOk = store.ReturnVideo(10);
        bool returnAgain = store.ReturnVideo(10);

        Assert.IsTrue(rentOk);
        Assert.IsFalse(rentAgain);
        Assert.IsTrue(returnOk);
        Assert.IsFalse(returnAgain);
    }

    [TestMethod]
    public void RemoveVideo_ShouldDeleteFromBothIndexes()
    {
        VideoStore store = new();
        store.AddVideo(new Video(11, "The Matrix", "Sci-Fi", 1999));

        bool removed = store.RemoveVideo(11);
        bool idExists = store.TrySearchById(11, out _);
        Video[] titleMatches = store.SearchByTitle("The Matrix");

        Assert.IsTrue(removed);
        Assert.IsFalse(idExists);
        Assert.IsEmpty(titleMatches);
    }

    [TestMethod]
    public void RepositoryIntegration_ShouldPersistOperations()
    {
        FakeRepository repo = new();
        VideoStore store = new(repo);
        store.AddVideo(new Video(1, "Inception", "Sci-Fi", 2010));
        store.AddVideo(new Video(2, "Titanic", "Drama", 1997));
        store.RentVideo(2);
        store.RemoveVideo(1);

        Video[] all = repo.LoadAllVideos();
        Assert.HasCount(1, all);
        Assert.AreEqual(2, all[0].VideoId);
        Assert.IsTrue(all[0].IsRented);
    }

    private sealed class FakeRepository : IVideoRepository
    {
        private Video[] _items = new Video[8];
        private int _count;

        public Video[] LoadAllVideos()
        {
            Video[] result = new Video[_count];
            for (int i = 0; i < _count; i++)
            {
                Video source = _items[i];
                result[i] = new Video(source.VideoId, source.Title, source.Genre, source.ReleaseYear, source.IsRented);
            }

            return result;
        }

        public void UpsertVideo(Video video)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_items[i].VideoId == video.VideoId)
                {
                    _items[i] = new Video(video.VideoId, video.Title, video.Genre, video.ReleaseYear, video.IsRented);
                    return;
                }
            }

            if (_count == _items.Length)
            {
                Video[] expanded = new Video[_items.Length * 2];
                for (int i = 0; i < _items.Length; i++)
                {
                    expanded[i] = _items[i];
                }

                _items = expanded;
            }

            _items[_count++] = new Video(video.VideoId, video.Title, video.Genre, video.ReleaseYear, video.IsRented);
        }

        public void DeleteVideo(int videoId)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_items[i].VideoId == videoId)
                {
                    for (int j = i; j < _count - 1; j++)
                    {
                        _items[j] = _items[j + 1];
                    }

                    _count--;
                    return;
                }
            }
        }
    }
}
