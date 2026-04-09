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

    // splits multi-word queries into individual terms and returns IDs matching all of them
    public int[] SearchVideoIds(string keyword)
    {
        if (string.IsNullOrWhiteSpace(keyword))
        {
            return [];
        }

        // split the query into individual terms just like titles are split during Add
        string[] queryTerms = keyword.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int validCount = 0;
        for (int i = 0; i < queryTerms.Length; i++)
        {
            queryTerms[i] = NormalizeKeyword(queryTerms[i]);
            if (queryTerms[i].Length >= 2) validCount++;
        }

        if (validCount == 0) return [];

        // collect IDs for the first valid term, then intersect with subsequent terms
        int[]? resultIds = null;
        for (int t = 0; t < queryTerms.Length; t++)
        {
            if (queryTerms[t].Length < 2) continue;

            int[] termIds = SearchSingleKeyword(queryTerms[t]);
            if (termIds.Length == 0) return [];

            if (resultIds == null)
            {
                resultIds = termIds;
            }
            else
            {
                // intersect: keep only IDs present in both arrays
                resultIds = IntersectIds(resultIds, termIds);
                if (resultIds.Length == 0) return [];
            }
        }

        return resultIds ?? [];
    }

    /// <summary>
    /// Fuzzy search: when exact keyword lookup misses, each query term is compared
    /// against every stored keyword via Levenshtein distance. Results are ranked by
    /// how many query terms matched (best-first).
    /// </summary>
    public int[] FuzzySearchVideoIds(string query, int maxDistance = 2)
    {
        if (string.IsNullOrWhiteSpace(query)) return [];

        string[] queryTerms = query.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        int validCount = 0;
        for (int i = 0; i < queryTerms.Length; i++)
        {
            queryTerms[i] = NormalizeKeyword(queryTerms[i]);
            if (queryTerms[i].Length >= 2) validCount++;
        }

        if (validCount == 0) return [];

        // gather all stored keywords from every bucket
        string[] storedKeywords = CollectAllKeywords();
        if (storedKeywords.Length == 0) return [];

        // for each query term, find all stored keywords within maxDistance
        // and collect their video IDs, then score videos by term-match count
        int[] videoScores = new int[0];
        int[] videoIds = new int[0];
        int scoreCount = 0;

        for (int t = 0; t < queryTerms.Length; t++)
        {
            if (queryTerms[t].Length < 2) continue;

            // collect IDs from all close-enough stored keywords for this term
            int[] termIds = new int[0];
            int termIdCount = 0;

            for (int k = 0; k < storedKeywords.Length; k++)
            {
                int dist = LevenshteinDistance(queryTerms[t], storedKeywords[k]);
                // allow proportionally larger distance for longer words
                int threshold = storedKeywords[k].Length <= 3 ? 1 : maxDistance;
                if (dist <= threshold)
                {
                    int[] matched = SearchSingleKeyword(storedKeywords[k]);
                    for (int m = 0; m < matched.Length; m++)
                    {
                        // add to termIds (grow-on-demand)
                        if (termIdCount >= termIds.Length)
                        {
                            int[] bigger = new int[Math.Max(8, termIds.Length * 2)];
                            for (int c = 0; c < termIdCount; c++) bigger[c] = termIds[c];
                            termIds = bigger;
                        }
                        // avoid duplicates
                        bool dup = false;
                        for (int d = 0; d < termIdCount; d++)
                        {
                            if (termIds[d] == matched[m]) { dup = true; break; }
                        }
                        if (!dup) termIds[termIdCount++] = matched[m];
                    }
                }
            }

            // bump the score for each video matched by this term
            for (int i = 0; i < termIdCount; i++)
            {
                int idx = -1;
                for (int s = 0; s < scoreCount; s++)
                {
                    if (videoIds[s] == termIds[i]) { idx = s; break; }
                }
                if (idx >= 0)
                {
                    videoScores[idx]++;
                }
                else
                {
                    if (scoreCount >= videoIds.Length)
                    {
                        int newLen = Math.Max(8, videoIds.Length * 2);
                        int[] bigIds = new int[newLen];
                        int[] bigScores = new int[newLen];
                        for (int c = 0; c < scoreCount; c++)
                        {
                            bigIds[c] = videoIds[c];
                            bigScores[c] = videoScores[c];
                        }
                        videoIds = bigIds;
                        videoScores = bigScores;
                    }
                    videoIds[scoreCount] = termIds[i];
                    videoScores[scoreCount] = 1;
                    scoreCount++;
                }
            }
        }

        if (scoreCount == 0) return [];

        // sort by descending score (most terms matched first)
        for (int i = 0; i < scoreCount - 1; i++)
        {
            for (int j = i + 1; j < scoreCount; j++)
            {
                if (videoScores[j] > videoScores[i])
                {
                    (videoScores[i], videoScores[j]) = (videoScores[j], videoScores[i]);
                    (videoIds[i], videoIds[j]) = (videoIds[j], videoIds[i]);
                }
            }
        }

        int[] result = new int[scoreCount];
        for (int i = 0; i < scoreCount; i++) result[i] = videoIds[i];
        return result;
    }

    // looks up a single normalised keyword in the hash table
    private int[] SearchSingleKeyword(string normalizedKeyword)
    {
        KeywordEntry? entry = FindEntry(normalizedKeyword);
        if (entry == null || entry.Head == null) return [];

        int count = 0;
        for (VideoIdNode? c = entry.Head; c != null; c = c.Next) count++;

        int[] ids = new int[count];
        int index = 0;
        for (VideoIdNode? c = entry.Head; c != null; c = c.Next) ids[index++] = c.VideoId;
        return ids;
    }

    // returns every keyword currently stored in the index
    private string[] CollectAllKeywords()
    {
        string[] buffer = new string[_keywordCount];
        int idx = 0;
        for (int b = 0; b < _buckets.Length; b++)
        {
            for (KeywordEntry? e = _buckets[b]; e != null; e = e.Next)
            {
                if (idx < buffer.Length) buffer[idx++] = e.Keyword;
            }
        }
        if (idx < buffer.Length)
        {
            string[] compact = new string[idx];
            for (int i = 0; i < idx; i++) compact[i] = buffer[i];
            return compact;
        }
        return buffer;
    }

    /// <summary>
    /// Classic dynamic-programming Levenshtein distance — counts the minimum
    /// single-character edits (insert, delete, substitute) to transform one
    /// string into another. Used for fuzzy keyword matching.
    /// </summary>
    private static int LevenshteinDistance(string a, string b)
    {
        if (a.Length == 0) return b.Length;
        if (b.Length == 0) return a.Length;

        // only two rows needed at a time to save memory
        int[] prev = new int[b.Length + 1];
        int[] curr = new int[b.Length + 1];

        for (int j = 0; j <= b.Length; j++) prev[j] = j;

        for (int i = 1; i <= a.Length; i++)
        {
            curr[0] = i;
            for (int j = 1; j <= b.Length; j++)
            {
                int cost = a[i - 1] == b[j - 1] ? 0 : 1;
                int insert = curr[j - 1] + 1;
                int delete = prev[j] + 1;
                int replace = prev[j - 1] + cost;
                curr[j] = Math.Min(insert, Math.Min(delete, replace));
            }
            // swap rows
            (prev, curr) = (curr, prev);
        }

        return prev[b.Length];
    }

    // intersects two sorted-or-unsorted ID arrays, returning only common entries
    private static int[] IntersectIds(int[] a, int[] b)
    {
        int[] temp = new int[Math.Min(a.Length, b.Length)];
        int count = 0;
        for (int i = 0; i < a.Length; i++)
        {
            for (int j = 0; j < b.Length; j++)
            {
                if (a[i] == b[j])
                {
                    temp[count++] = a[i];
                    break;
                }
            }
        }
        if (count == temp.Length) return temp;
        int[] result = new int[count];
        for (int i = 0; i < count; i++) result[i] = temp[i];
        return result;
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
