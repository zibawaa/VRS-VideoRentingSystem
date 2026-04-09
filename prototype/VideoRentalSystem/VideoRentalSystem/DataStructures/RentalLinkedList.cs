using VideoRentalSystem.Models;

namespace VideoRentalSystem.DataStructures;

// singly linked list node — no List<T> used anywhere in this class
public class VideoNode
{
    public RentalVideo Data { get; set; }
    public VideoNode Next { get; set; }
}

// custom singly linked list — module requirement (no BCL List/Dictionary inside here)
public class RentalLinkedList
{
    private VideoNode head;
    private int count;

    public int Count => count;

    // Time Complexity: O(n) — have to walk to the end to append in order
    public bool Add(RentalVideo video)
    {
        if (ContainsId(video.RentalID))
            return false; // duplicate id not allowed

        var node = new VideoNode { Data = video };

        if (head == null)
        {
            head = node;
        }
        else
        {
            VideoNode cur = head;
            while (cur.Next != null)
                cur = cur.Next;
            cur.Next = node;
        }

        count++;
        return true;
    }

    // Time Complexity: O(n) — same as Add tail walk, but skips duplicate check (used when refilling from DB)
    public void AppendFromDatabase(RentalVideo video)
    {
        var node = new VideoNode { Data = video };

        if (head == null)
        {
            head = node;
        }
        else
        {
            VideoNode cur = head;
            while (cur.Next != null)
                cur = cur.Next;
            cur.Next = node;
        }

        count++;
    }

    // Time Complexity: O(n) — scan for matching id
    public bool ContainsId(int rentalId)
    {
        VideoNode cur = head;
        while (cur != null)
        {
            if (cur.Data.RentalID == rentalId)
                return true;
            cur = cur.Next;
        }

        return false;
    }

    // Time Complexity: O(n) — worst case remove last node, need to find previous
    public bool RemoveById(int rentalId)
    {
        if (head == null)
            return false;

        if (head.Data.RentalID == rentalId)
        {
            head = head.Next;
            count--;
            return true;
        }

        VideoNode prev = head;
        while (prev.Next != null)
        {
            if (prev.Next.Data.RentalID == rentalId)
            {
                prev.Next = prev.Next.Next;
                count--;
                return true;
            }

            prev = prev.Next;
        }

        return false;
    }

    // Time Complexity: O(n) — linear search by title (case insensitive)
    public RentalVideo SearchByTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
            return null;

        string want = title.Trim();
        VideoNode cur = head;
        while (cur != null)
        {
            if (string.Equals(cur.Data.Title, want, StringComparison.OrdinalIgnoreCase))
                return cur.Data;
            cur = cur.Next;
        }

        return null;
    }

    // Time Complexity: O(n) — visit every node once
    public void Display(Action<RentalVideo> visitEach)
    {
        VideoNode cur = head;
        while (cur != null)
        {
            visitEach(cur.Data);
            cur = cur.Next;
        }
    }

    // Time Complexity: O(n) — two passes: count then fill (still linear, no List<T>)
    public RentalVideo[] ToArray()
    {
        if (count == 0)
            return Array.Empty<RentalVideo>();

        var arr = new RentalVideo[count];
        int i = 0;
        VideoNode cur = head;
        while (cur != null)
        {
            arr[i++] = cur.Data;
            cur = cur.Next;
        }

        return arr;
    }

    // Time Complexity: O(n) — free every node reference
    public void Clear()
    {
        head = null;
        count = 0;
    }
}
