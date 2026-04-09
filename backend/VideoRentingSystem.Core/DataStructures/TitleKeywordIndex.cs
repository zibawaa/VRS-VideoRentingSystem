using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.DataStructures;

public sealed class TitleKeywordIndex
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

    private sealed class KeywordEntry
    {
        public string Keyword;
        public VideoIdNode? Head;
        public KeywordEntry? Next;

        public KeywordEntry(string keyword, int videoId, KeywordEntry? next)
        {
            Keyword = keyword;
            Head = new VideoIdNode(videoId, null);
            Next = next;
        }
    }

    private KeywordEntry?[] _buckets;
    private int _keywordCount;
    private readonly double _maxLoadFactor;

    public TitleKeywordIndex(int initialCapacity = 31, double maxLoadFactor = 0.75)
    {
        if (initialCapacity < 3)
        {
            initialCapacity = 3;
        }

        _buckets = new KeywordEntry?[NextPrime(initialCapacity)];
        _maxLoadFactor = maxLoadFactor <= 0.0 || maxLoadFactor >= 1.0 ? 0.75 : maxLoadFactor;
    }

    public void Add(Video video)
    {
        string[] keywords = GetKeywords(video.Title);
        for (int i = 0; i < keywords.Length; i++)
        {
            AddKeywordVideoId(keywords[i], video.VideoId);
        }
    }

    public void Remove(Video video)
    {
        string[] keywords = GetKeywords(video.Title);
        for (int i = 0; i < keywords.Length; i++)
        {
            RemoveKeywordVideoId(keywords[i], video.VideoId);
        }
    }

    public int[] SearchVideoIds(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        string normalized = NormalizeKeyword(keyword);
        KeywordEntry? entry = FindEntry(normalized);
        if (entry == null || entry.Head == null)
        {
            return [];
        }

        int count = 0;
        for (VideoIdNode? c = entry.Head; c != null; c = c.Next)
        {
            count++;
        }

        int[] ids = new int[count];
        int index = 0;
        for (VideoIdNode? c = entry.Head; c != null; c = c.Next)
        {
            ids[index++] = c.VideoId;
        }

        return ids;
    }

    private void AddKeywordVideoId(string keyword, int videoId)
    {
        KeywordEntry? entry = FindEntry(keyword);
        if (entry == null)
        {
            if ((_keywordCount + 1) > _buckets.Length * _maxLoadFactor)
            {
                Resize(NextPrime(_buckets.Length * 2));
            }

            int bucket = GetBucket(keyword, _buckets.Length);
            _buckets[bucket] = new KeywordEntry(keyword, videoId, _buckets[bucket]);
            _keywordCount++;
            return;
        }

        if (!ContainsVideoId(entry.Head, videoId))
        {
            entry.Head = new VideoIdNode(videoId, entry.Head);
        }
    }

    private void RemoveKeywordVideoId(string keyword, int videoId)
    {
        int bucket = GetBucket(keyword, _buckets.Length);
        KeywordEntry? previousEntry = null;
        KeywordEntry? currentEntry = _buckets[bucket];
        while (currentEntry != null)
        {
            if (string.Equals(currentEntry.Keyword, keyword, StringComparison.Ordinal))
            {
                VideoIdNode? previousNode = null;
                VideoIdNode? currentNode = currentEntry.Head;
                while (currentNode != null)
                {
                    if (currentNode.VideoId == videoId)
                    {
                        if (previousNode == null)
                        {
                            currentEntry.Head = currentNode.Next;
                        }
                        else
                        {
                            previousNode.Next = currentNode.Next;
                        }

                        if (currentEntry.Head == null)
                        {
                            if (previousEntry == null)
                            {
                                _buckets[bucket] = currentEntry.Next;
                            }
                            else
                            {
                                previousEntry.Next = currentEntry.Next;
                            }

                            _keywordCount--;
                        }

                        return;
                    }

                    previousNode = currentNode;
                    currentNode = currentNode.Next;
                }

                return;
            }

            previousEntry = currentEntry;
            currentEntry = currentEntry.Next;
        }
    }

    private KeywordEntry? FindEntry(string keyword)
    {
        int bucket = GetBucket(keyword, _buckets.Length);
        KeywordEntry? current = _buckets[bucket];
        while (current != null)
        {
            if (string.Equals(current.Keyword, keyword, StringComparison.Ordinal))
            {
                return current;
            }

            current = current.Next;
        }

        return null;
    }

    private static bool ContainsVideoId(VideoIdNode? head, int videoId)
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

    private static string[] GetKeywords(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return [];
        }

        string[] raw = title.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (raw.Length == 0)
        {
            return [];
        }

        string[] keywords = new string[raw.Length];
        int count = 0;
        for (int i = 0; i < raw.Length; i++)
        {
            string candidate = NormalizeKeyword(raw[i]);
            if (candidate.Length < 2)
            {
                continue;
            }

            bool exists = false;
            for (int j = 0; j < count; j++)
            {
                if (keywords[j] == candidate)
                {
                    exists = true;
                    break;
                }
            }

            if (!exists)
            {
                keywords[count++] = candidate;
            }
        }

        if (count == 0)
        {
            return [];
        }

        string[] compact = new string[count];
        for (int i = 0; i < count; i++)
        {
            compact[i] = keywords[i];
        }

        return compact;
    }

    private static string NormalizeKeyword(string keyword)
    {
        return keyword.Trim().ToUpperInvariant();
    }

    private static int GetBucket(string key, int length)
    {
        int hash = key.GetHashCode() & int.MaxValue;
        return hash % length;
    }

    private void Resize(int newCapacity)
    {
        KeywordEntry?[] newBuckets = new KeywordEntry?[newCapacity];
        for (int i = 0; i < _buckets.Length; i++)
        {
            KeywordEntry? current = _buckets[i];
            while (current != null)
            {
                KeywordEntry? next = current.Next;
                int newBucket = GetBucket(current.Keyword, newCapacity);
                current.Next = newBuckets[newBucket];
                newBuckets[newBucket] = current;
                current = next;
            }
        }

        _buckets = newBuckets;
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
