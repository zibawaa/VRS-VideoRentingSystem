// Separate-chaining hash table for VideoId → Video. Gives us fast rent/return lookups without scanning the whole catalogue.
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.DataStructures;

public sealed class IdHashIndex
{
    private sealed class Entry
    {
        public int Key;
        public Video Value;
        public Entry? Next;

        public Entry(int key, Video value, Entry? next)
        {
            Key = key;
            Value = value;
            Next = next;
        }
    }

    private Entry?[] _buckets;
    private int _count;
    private readonly double _maxLoadFactor;

    public IdHashIndex(int initialCapacity = 17, double maxLoadFactor = 0.75)
    {
        if (initialCapacity < 3)
        {
            initialCapacity = 3;
        }

        _buckets = new Entry?[NextPrime(initialCapacity)];
        _maxLoadFactor = maxLoadFactor <= 0.0 || maxLoadFactor >= 1.0 ? 0.75 : maxLoadFactor;
    }

    public int Count => _count;

    public bool Add(Video video)
    {
        if (ContainsKey(video.VideoId))
        {
            return false;
        }

        if ((_count + 1) > _buckets.Length * _maxLoadFactor)
        {
            Resize(NextPrime(_buckets.Length * 2));
        }

        int bucket = GetBucket(video.VideoId, _buckets.Length);
        _buckets[bucket] = new Entry(video.VideoId, video, _buckets[bucket]);
        _count++;
        return true;
    }

    public bool TryGetValue(int videoId, out Video? video)
    {
        Entry? current = _buckets[GetBucket(videoId, _buckets.Length)];
        while (current != null)
        {
            if (current.Key == videoId)
            {
                video = current.Value;
                return true;
            }

            current = current.Next;
        }

        video = null;
        return false;
    }

    public bool ContainsKey(int videoId)
    {
        return TryGetValue(videoId, out _);
    }

    public bool Remove(int videoId)
    {
        int bucket = GetBucket(videoId, _buckets.Length);
        Entry? previous = null;
        Entry? current = _buckets[bucket];

        while (current != null)
        {
            if (current.Key == videoId)
            {
                if (previous == null)
                {
                    _buckets[bucket] = current.Next;
                }
                else
                {
                    previous.Next = current.Next;
                }

                _count--;
                return true;
            }

            previous = current;
            current = current.Next;
        }

        return false;
    }

    public Video[] ToArray()
    {
        Video[] all = new Video[_count];
        int index = 0;

        for (int i = 0; i < _buckets.Length; i++)
        {
            Entry? current = _buckets[i];
            while (current != null)
            {
                all[index++] = current.Value;
                current = current.Next;
            }
        }

        return all;
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
                int newBucket = GetBucket(current.Key, newCapacity);
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
