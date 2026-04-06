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
        // in-memory store: AVL + hash with no sqlite IO in this test

        bool added = store.AddVideo(video);
        Assert.IsTrue(added);
        Assert.IsTrue(store.TrySearchById(1, out Video? foundById));
        Assert.IsNotNull(foundById);
        Assert.AreEqual("Inception", foundById.Title);

        Video[] foundByTitle = store.SearchByTitle("inception");
        // title index normalises case, so lowercase query should still hit
        Assert.HasCount(1, foundByTitle);
        Assert.AreEqual(1, foundByTitle[0].VideoId);
    }

    [TestMethod]
    public void AddVideo_DuplicateId_ShouldFail()
    {
        VideoStore store = new();
        bool first = store.AddVideo(new Video(1, "Inception", "Sci-Fi", 2010));
        bool second = store.AddVideo(new Video(1, "Interstellar", "Sci-Fi", 2014));
        // same VideoId must be rejected even if other fields differ

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
        // deliberate non-sorted insert order

        Video[] ordered = store.DisplayAllVideos();
        // DisplayAllVideos must not mirror insert order; titles should sort
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

        bool rentOk = store.RentVideo(10, 500);
        bool rentAgain = store.RentVideo(10, 500);
        bool returnOk = store.ReturnVideo(10, 500);
        bool returnAgain = store.ReturnVideo(10, 500);
        // second rent and second return should both fail (already rented / already shelved)

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
        // removal must clear both id hash and AVL title index

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
        store.RentVideo(2, 500);
        store.RemoveVideo(1);
        // leave one video behind and mark it rented

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
                // copy fields into new instances so callers cannot mutate our backing array
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
            // grow backing array if append would overflow the slot

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
                    // shift tail left to erase the hole without allocating

                    _count--;
                    return;
                }
            }
        }
    }
}

[TestClass]
public sealed class UserStoreTests
{
    [TestMethod]
    public void UserStore_ShouldRegisterAndAuthenticate()
    {
        UserStore userStore = new();
        bool registered = userStore.RegisterUser("john", "password123");
        bool duplicate = userStore.RegisterUser("john", "otherpass");
        // first insert wins; same username with different password still conflicts

        Assert.IsTrue(registered);
        Assert.IsFalse(duplicate);

        User? successLogin = userStore.Login("john", "password123");
        User? failedLogin = userStore.Login("john", "wrongpass");

        Assert.IsNotNull(successLogin);
        Assert.IsNull(failedLogin);
        Assert.AreEqual("john", successLogin.Username);
    }

    [TestMethod]
    public void VideoStore_RentAndReturnWithUser_ShouldTrackRentals()
    {
        VideoStore store = new();
        store.AddVideo(new Video(10, "The Matrix", "Sci-Fi", 1999));
        store.AddVideo(new Video(11, "Inception", "Sci-Fi", 2010));

        store.RentVideo(10, 500);
        store.RentVideo(11, 600);
        // split rentals across two user ids to test per-user lists

        Video[] user500Rentals = store.GetUserRentedVideos(500);
        Video[] user600Rentals = store.GetUserRentedVideos(600);

        Assert.HasCount(1, user500Rentals);
        Assert.AreEqual(10, user500Rentals[0].VideoId);

        Assert.HasCount(1, user600Rentals);
        Assert.AreEqual(11, user600Rentals[0].VideoId);

        bool returnFailed = store.ReturnVideo(10, 600);
        // return must be denied when the acting user is not the recorded renter
        Assert.IsFalse(returnFailed, "User 600 cannot return User 500's video");

        bool returnSuccess = store.ReturnVideo(10, 500);
        Assert.IsTrue(returnSuccess);

        Assert.HasCount(0, store.GetUserRentedVideos(500));
    }
}
