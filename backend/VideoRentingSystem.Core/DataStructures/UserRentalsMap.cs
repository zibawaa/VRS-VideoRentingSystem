namespace VideoRentingSystem.Core.DataStructures;

public sealed class UserRentalsMap
{
    private sealed class VideoNode
    {
        public int VideoId;
        public VideoNode? Next;

        public VideoNode(int videoId, VideoNode? next)
        {
            VideoId = videoId;
            Next = next;
        }
    }

    private sealed class MapEntry
    {
        public int UserId;
        public VideoNode? RentedVideosHead;
        public MapEntry? Next;

        public MapEntry(int userId, VideoNode? rentedVideosHead, MapEntry? next)
        {
            UserId = userId;
            RentedVideosHead = rentedVideosHead;
            Next = next;
        }
    }

    private MapEntry?[] _buckets;
    private int _userCount;
    private readonly double _maxLoadFactor;

    public UserRentalsMap(int initialCapacity = 17, double maxLoadFactor = 0.75)
    {
        if (initialCapacity < 3)
        {
            initialCapacity = 3;
        }

        _buckets = new MapEntry?[NextPrime(initialCapacity)];
        _maxLoadFactor = maxLoadFactor <= 0.0 || maxLoadFactor >= 1.0 ? 0.75 : maxLoadFactor;
    }

    public void AddRental(int userId, int videoId)
    {
        MapEntry? entry = FindUserEntry(userId);
        if (entry != null)
        {
            if (!ContainsVideo(entry.RentedVideosHead, videoId))
            {
                entry.RentedVideosHead = new VideoNode(videoId, entry.RentedVideosHead);
            }
        }
        else
        {
            if ((_userCount + 1) > _buckets.Length * _maxLoadFactor)
            {
                Resize(NextPrime(_buckets.Length * 2));
            }

            int bucket = GetBucket(userId, _buckets.Length);
            VideoNode newVideoNode = new VideoNode(videoId, null);
            _buckets[bucket] = new MapEntry(userId, newVideoNode, _buckets[bucket]);
            _userCount++;
        }
    }

    public bool RemoveRental(int userId, int videoId)
    {
        MapEntry? entry = FindUserEntry(userId);
        if (entry == null || entry.RentedVideosHead == null)
        {
            return false;
        }

        VideoNode? current = entry.RentedVideosHead;
        VideoNode? previous = null;
        while (current != null)
        {
            if (current.VideoId == videoId)
            {
                if (previous == null)
                {
                    entry.RentedVideosHead = current.Next;
                }
                else
                {
                    previous.Next = current.Next;
                }

                return true;
            }

            previous = current;
            current = current.Next;
        }

        return false;
    }

    public int[] GetUserRentals(int userId)
    {
        MapEntry? entry = FindUserEntry(userId);
        if (entry == null || entry.RentedVideosHead == null)
        {
            return [];
        }

        // Singly linked list: count first so we allocate one tight array (no List<T> churn).
        int count = 0;
        for (VideoNode? c = entry.RentedVideosHead; c != null; c = c.Next)
        {
            count++;
        }

        int[] videos = new int[count];
        int index = 0;
        for (VideoNode? c = entry.RentedVideosHead; c != null; c = c.Next)
        {
            videos[index++] = c.VideoId;
        }

        return videos;
    }

    private MapEntry? FindUserEntry(int userId)
    {
        int bucket = GetBucket(userId, _buckets.Length);
        MapEntry? current = _buckets[bucket];
        while (current != null)
        {
            if (current.UserId == userId)
            {
                return current;
            }

            current = current.Next;
        }

        return null;
    }

    private bool ContainsVideo(VideoNode? head, int videoId)
    {
        VideoNode? current = head;
        while (current != null)
        {
            if (current.VideoId == videoId)
            {
                return true;
            }

            current = current.Next;
        }

        return false;
    }

    private void Resize(int newCapacity)
    {
        MapEntry?[] newBuckets = new MapEntry?[newCapacity];
        for (int i = 0; i < _buckets.Length; i++)
        {
            MapEntry? current = _buckets[i];
            while (current != null)
            {
                MapEntry? next = current.Next;
                int newBucket = GetBucket(current.UserId, newCapacity);
                current.Next = newBuckets[newBucket];
                newBuckets[newBucket] = current;
                current = next;
            }
        }

        _buckets = newBuckets;
    }

    private static int GetBucket(int key, int length)
    {
        int hash = key & int.MaxValue;
        return hash % length;
    }

    private static int NextPrime(int start)
    {
        int candidate = start % 2 == 0 ? start + 1 : start;
        while (!IsPrime(candidate))
        {
            candidate += 2;
        }

        return candidate;
    }

    private static bool IsPrime(int value)
    {
        if (value <= 1)
        {
            return false;
        }

        if (value == 2)
        {
            return true;
        }

        if (value % 2 == 0)
        {
            return false;
        }

        int limit = (int)Math.Sqrt(value);
        for (int i = 3; i <= limit; i += 2)
        {
            if (value % i == 0)
            {
                return false;
            }
        }

        return true;
    }
}
