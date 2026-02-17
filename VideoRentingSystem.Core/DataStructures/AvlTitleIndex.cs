using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.DataStructures;

public sealed class AvlTitleIndex
{
    private sealed class VideoChainNode
    {
        public Video Value;
        public VideoChainNode? Next;

        public VideoChainNode(Video value, VideoChainNode? next)
        {
            Value = value;
            Next = next;
        }
    }

    private sealed class Node
    {
        public string Key;
        public Node? Left;
        public Node? Right;
        public int Height;
        public VideoChainNode? Videos;
        public int VideoCount;

        public Node(string key, Video video)
        {
            Key = key;
            Height = 1;
            Videos = new VideoChainNode(video, null);
            VideoCount = 1;
        }
    }

    private Node? _root;
    private int _totalCount;

    public int Count => _totalCount;

    public bool Add(Video video)
    {
        string key = Normalize(video.Title);
        bool inserted = false;
        _root = Insert(_root, key, video, ref inserted);
        if (inserted)
        {
            _totalCount++;
        }

        return inserted;
    }

    public bool Remove(Video video)
    {
        string key = Normalize(video.Title);
        bool removed = false;
        _root = Remove(_root, key, video.VideoId, ref removed);
        if (removed)
        {
            _totalCount--;
        }

        return removed;
    }

    public Video[] SearchByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return [];
        }

        Node? node = FindNode(_root, Normalize(title));
        if (node == null || node.Videos == null)
        {
            return [];
        }

        Video[] matches = new Video[node.VideoCount];
        int index = 0;
        VideoChainNode? current = node.Videos;
        while (current != null)
        {
            matches[index++] = current.Value;
            current = current.Next;
        }

        return matches;
    }

    public Video[] InOrderTraversal()
    {
        if (_totalCount == 0)
        {
            return [];
        }

        Video[] result = new Video[_totalCount];
        int index = 0;
        FillInOrder(_root, result, ref index);
        return result;
    }

    private static string Normalize(string title)
    {
        return title.Trim().ToUpperInvariant();
    }

    private static Node? FindNode(Node? node, string key)
    {
        while (node != null)
        {
            int cmp = string.CompareOrdinal(key, node.Key);
            if (cmp == 0)
            {
                return node;
            }

            node = cmp < 0 ? node.Left : node.Right;
        }

        return null;
    }

    private static void FillInOrder(Node? node, Video[] output, ref int index)
    {
        if (node == null)
        {
            return;
        }

        FillInOrder(node.Left, output, ref index);

        VideoChainNode? current = node.Videos;
        while (current != null)
        {
            output[index++] = current.Value;
            current = current.Next;
        }

        FillInOrder(node.Right, output, ref index);
    }

    private static Node Insert(Node? node, string key, Video video, ref bool inserted)
    {
        if (node == null)
        {
            inserted = true;
            return new Node(key, video);
        }

        int cmp = string.CompareOrdinal(key, node.Key);
        if (cmp < 0)
        {
            node.Left = Insert(node.Left, key, video, ref inserted);
        }
        else if (cmp > 0)
        {
            node.Right = Insert(node.Right, key, video, ref inserted);
        }
        else
        {
            node.Videos = new VideoChainNode(video, node.Videos);
            node.VideoCount++;
            inserted = true;
            return node;
        }

        return Rebalance(node);
    }

    private static Node? Remove(Node? node, string key, int videoId, ref bool removed)
    {
        if (node == null)
        {
            return null;
        }

        int cmp = string.CompareOrdinal(key, node.Key);
        if (cmp < 0)
        {
            node.Left = Remove(node.Left, key, videoId, ref removed);
        }
        else if (cmp > 0)
        {
            node.Right = Remove(node.Right, key, videoId, ref removed);
        }
        else
        {
            bool chainRemoved = RemoveFromChain(node, videoId);
            if (!chainRemoved)
            {
                return node;
            }

            removed = true;
            if (node.VideoCount > 0)
            {
                return node;
            }

            if (node.Left == null)
            {
                return node.Right;
            }

            if (node.Right == null)
            {
                return node.Left;
            }

            Node successor = MinNode(node.Right);
            node.Key = successor.Key;
            node.Videos = successor.Videos;
            node.VideoCount = successor.VideoCount;

            bool unused = false;
            node.Right = RemoveNodeByKey(node.Right, successor.Key, ref unused);
        }

        return Rebalance(node);
    }

    private static Node? RemoveNodeByKey(Node? node, string key, ref bool removed)
    {
        if (node == null)
        {
            return null;
        }

        int cmp = string.CompareOrdinal(key, node.Key);
        if (cmp < 0)
        {
            node.Left = RemoveNodeByKey(node.Left, key, ref removed);
        }
        else if (cmp > 0)
        {
            node.Right = RemoveNodeByKey(node.Right, key, ref removed);
        }
        else
        {
            removed = true;

            if (node.Left == null)
            {
                return node.Right;
            }

            if (node.Right == null)
            {
                return node.Left;
            }

            Node successor = MinNode(node.Right);
            node.Key = successor.Key;
            node.Videos = successor.Videos;
            node.VideoCount = successor.VideoCount;

            bool childRemoved = false;
            node.Right = RemoveNodeByKey(node.Right, successor.Key, ref childRemoved);
        }

        return Rebalance(node);
    }

    private static bool RemoveFromChain(Node node, int videoId)
    {
        VideoChainNode? current = node.Videos;
        VideoChainNode? previous = null;
        while (current != null)
        {
            if (current.Value.VideoId == videoId)
            {
                if (previous == null)
                {
                    node.Videos = current.Next;
                }
                else
                {
                    previous.Next = current.Next;
                }

                node.VideoCount--;
                return true;
            }

            previous = current;
            current = current.Next;
        }

        return false;
    }

    private static Node MinNode(Node node)
    {
        Node current = node;
        while (current.Left != null)
        {
            current = current.Left;
        }

        return current;
    }

    private static Node Rebalance(Node node)
    {
        UpdateHeight(node);
        int balance = GetBalance(node);

        if (balance > 1)
        {
            if (GetBalance(node.Left!) < 0)
            {
                node.Left = RotateLeft(node.Left!);
            }

            return RotateRight(node);
        }

        if (balance < -1)
        {
            if (GetBalance(node.Right!) > 0)
            {
                node.Right = RotateRight(node.Right!);
            }

            return RotateLeft(node);
        }

        return node;
    }

    private static Node RotateRight(Node y)
    {
        Node x = y.Left!;
        Node? transfer = x.Right;

        x.Right = y;
        y.Left = transfer;

        UpdateHeight(y);
        UpdateHeight(x);

        return x;
    }

    private static Node RotateLeft(Node x)
    {
        Node y = x.Right!;
        Node? transfer = y.Left;

        y.Left = x;
        x.Right = transfer;

        UpdateHeight(x);
        UpdateHeight(y);

        return y;
    }

    private static void UpdateHeight(Node node)
    {
        node.Height = 1 + Math.Max(Height(node.Left), Height(node.Right));
    }

    private static int Height(Node? node) => node?.Height ?? 0;

    private static int GetBalance(Node? node)
    {
        if (node == null)
        {
            return 0;
        }

        return Height(node.Left) - Height(node.Right);
    }
}
