// Binary search tree on username for log-in lookups. We picked BST over AVL here because user count stays small in the demo.
using VideoRentingSystem.Core.Models;

namespace VideoRentingSystem.Core.DataStructures;

public sealed class UserBstIndex
{
    private sealed class Node
    {
        public string Key;
        public User Value;
        public Node? Left;
        public Node? Right;

        public Node(string key, User value)
        {
            Key = key;
            Value = value;
        }
    }

    private Node? _root;
    private int _count;

    public int Count => _count;

    public bool Add(User user)
    {
        if (user == null || string.IsNullOrWhiteSpace(user.Username))
        {
            return false;
        }

        string key = user.Username.ToLowerInvariant();
        bool inserted = false;

        _root = AddRecursive(_root, key, user, ref inserted);
        
        if (inserted)
        {
            _count++;
        }

        return inserted;
    }

    public User? SearchByUsername(string username)
    {
        if (string.IsNullOrWhiteSpace(username))
        {
            return null;
        }

        string key = username.ToLowerInvariant();
        Node? current = _root;

        while (current != null)
        {
            int comparison = string.CompareOrdinal(key, current.Key);

            if (comparison == 0)
            {
                return current.Value;
            }
            else if (comparison < 0)
            {
                current = current.Left;
            }
            else
            {
                current = current.Right;
            }
        }

        return null;
    }

    public User[] ToArray()
    {
        if (_count == 0)
        {
            return [];
        }

        User[] users = new User[_count];
        int index = 0;
        InOrderTraversal(_root, users, ref index);
        return users;
    }

    private Node AddRecursive(Node? node, string key, User user, ref bool inserted)
    {
        if (node == null)
        {
            inserted = true;
            return new Node(key, user);
        }

        int comparison = string.CompareOrdinal(key, node.Key);

        if (comparison < 0)
        {
            node.Left = AddRecursive(node.Left, key, user, ref inserted);
        }
        else if (comparison > 0)
        {
            node.Right = AddRecursive(node.Right, key, user, ref inserted);
        }
        else
        {
            inserted = false; // duplicate username
        }

        return node;
    }

    private void InOrderTraversal(Node? node, User[] array, ref int index)
    {
        if (node != null)
        {
            InOrderTraversal(node.Left, array, ref index);
            array[index++] = node.Value;
            InOrderTraversal(node.Right, array, ref index);
        }
    }
}
