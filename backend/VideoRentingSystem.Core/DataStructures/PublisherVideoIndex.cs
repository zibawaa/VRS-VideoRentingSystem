namespace VideoRentingSystem.Core.DataStructures;

public sealed class PublisherVideoIndex
{
    private sealed class VideoIdNode
    {
        public int VideoId;
        public VideoIdNode? Next;

        public VideoIdNode(int videoId, VideoIdNode? next)
        {
            VideoId = videoId;
            Next = next;
        }
    }

    private sealed class Entry
    {
        public int PublisherId;
        public VideoIdNode? Videos;
        public Entry? Next;

        public Entry(int publisherId, VideoIdNode? videos, Entry? next)
        {
            PublisherId = publisherId;
            Videos = videos;
            Next = next;
        }
    }

    private Entry?[] _buckets;
    private int _publisherCount;
    private readonly double _maxLoadFactor;

    public PublisherVideoIndex(int initialCapacity = 17, double maxLoadFactor = 0.75)
    {
        if (initialCapacity < 3)
        {
            initialCapacity = 3;
        }

        _buckets = new Entry?[NextPrime(initialCapacity)];
        _maxLoadFactor = maxLoadFactor <= 0.0 || maxLoadFactor >= 1.0 ? 0.75 : maxLoadFactor;
    }

    public void AddVideo(int publisherId, int videoId)
    {
        if (publisherId <= 0)
        {
            return;
        }
        // ids <= 0 represent legacy/unowned catalogue rows, so they are skipped

        Entry? entry = FindEntry(publisherId);
        if (entry != null)
        {
            if (!ContainsVideo(entry.Videos, videoId))
            {
                entry.Videos = new VideoIdNode(videoId, entry.Videos);
            }

            return;
        }

        if ((_publisherCount + 1) > _buckets.Length * _maxLoadFactor)
        {
            Resize(NextPrime(_buckets.Length * 2));
        }

        int bucket = GetBucket(publisherId, _buckets.Length);
        _buckets[bucket] = new Entry(publisherId, new VideoIdNode(videoId, null), _buckets[bucket]);
        _publisherCount++;
    }

    public void RemoveVideo(int publisherId, int videoId)
    {
        if (publisherId <= 0)
        {
            return;
        }

        int bucket = GetBucket(publisherId, _buckets.Length);
        Entry? previousEntry = null;
        Entry? currentEntry = _buckets[bucket];
        while (currentEntry != null)
        {
            if (currentEntry.PublisherId == publisherId)
            {
                VideoIdNode? previousVideo = null;
                VideoIdNode? currentVideo = currentEntry.Videos;
                while (currentVideo != null)
                {
                    if (currentVideo.VideoId == videoId)
                    {
                        if (previousVideo == null)
                        {
                            currentEntry.Videos = currentVideo.Next;
                        }
                        else
                        {
                            previousVideo.Next = currentVideo.Next;
                        }

                        if (currentEntry.Videos == null)
                        {
                            if (previousEntry == null)
                            {
                                _buckets[bucket] = currentEntry.Next;
                            }
                            else
                            {
                                previousEntry.Next = currentEntry.Next;
                            }

                            _publisherCount--;
                        }

                        return;
                    }

                    previousVideo = currentVideo;
                    currentVideo = currentVideo.Next;
                }

                return;
            }

            previousEntry = currentEntry;
            currentEntry = currentEntry.Next;
        }
    }

    public int[] GetVideoIds(int publisherId)
    {
        if (publisherId <= 0)
        {
            return [];
        }

        Entry? entry = FindEntry(publisherId);
        if (entry == null || entry.Videos == null)
        {
            return [];
        }

        int count = 0;
        for (VideoIdNode? c = entry.Videos; c != null; c = c.Next)
        {
            count++;
        }

        int[] ids = new int[count];
        int index = 0;
        for (VideoIdNode? c = entry.Videos; c != null; c = c.Next)
        {
            ids[index++] = c.VideoId;
        }

        return ids;
    }

    private Entry? FindEntry(int publisherId)
    {
        int bucket = GetBucket(publisherId, _buckets.Length);
        Entry? current = _buckets[bucket];
        while (current != null)
        {
            if (current.PublisherId == publisherId)
            {
                return current;
            }

            current = current.Next;
        }

        return null;
    }

    private static bool ContainsVideo(VideoIdNode? head, int videoId)
    {
        for (VideoIdNode? c = head; c != null; c = c.Next)
        {
            if (c.VideoId == videoId)
            {
                return true;
            }
        }

        return false;
    }

    private void Resize(int newCapacity)
    {
        Entry?[] newBuckets = new Entry?[newCapacity];
        for (int i = 0; i < _buckets.Length; i++)
        {
            Entry? current = _buckets[i];
            while (current != null)
            {
                Entry? next = current.Next;
                int newBucket = GetBucket(current.PublisherId, newCapacity);
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
