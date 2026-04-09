using VideoRentalSystem.Data;
using VideoRentalSystem.DataStructures;
using VideoRentalSystem.Models;

namespace VideoRentalSystem.Services;

// ties SQL Server file + our linked list so the form code stays smaller
public class VideoStoreService
{
    private readonly RentalDatabase db = new();
    private readonly RentalLinkedList list = new();

    public RentalDatabase Database => db;
    public RentalLinkedList List => list;

    public void Connect(string dbPath)
    {
        db.Open(dbPath);
        ReloadFromDatabase();
    }

    public void ReloadFromDatabase()
    {
        list.Clear();
        foreach (var v in db.LoadAll())
            list.AppendFromDatabase(v); // trust primary keys from the database
    }

    public bool TryAdd(RentalVideo v, out string error)
    {
        error = null;
        if (!db.IsOpen)
        {
            error = "Connect to a database first.";
            return false;
        }

        if (list.ContainsId(v.RentalID))
        {
            error = "That RentalID is already in the list.";
            return false;
        }

        try
        {
            db.Insert(v);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        list.Add(v);
        return true;
    }

    public bool TryRemove(int rentalId, out string error)
    {
        error = null;
        if (!db.IsOpen)
        {
            error = "Connect to a database first.";
            return false;
        }

        try
        {
            db.Delete(rentalId);
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }

        list.RemoveById(rentalId);
        return true;
    }

    public bool TryRentCopy(int rentalId, out string error)
    {
        error = null;
        if (!db.IsOpen)
        {
            error = "Connect to a database first.";
            return false;
        }

        RentalVideo found = null;
        list.Display(v =>
        {
            if (v.RentalID == rentalId) found = v;
        });

        if (found == null)
        {
            error = "No video with that ID.";
            return false;
        }

        if (found.AvailableCopies <= 0)
        {
            error = "No copies left to rent.";
            return false;
        }

        found.AvailableCopies--;
        db.UpdateCopies(rentalId, found.AvailableCopies);
        return true;
    }

    public bool TryReturnCopy(int rentalId, out string error)
    {
        error = null;
        if (!db.IsOpen)
        {
            error = "Connect to a database first.";
            return false;
        }

        RentalVideo found = null;
        list.Display(v =>
        {
            if (v.RentalID == rentalId) found = v;
        });

        if (found == null)
        {
            error = "No video with that ID.";
            return false;
        }

        // TODO: cap returns against a TotalCopies column if we add that later
        found.AvailableCopies++;
        db.UpdateCopies(rentalId, found.AvailableCopies);
        return true;
    }
}
