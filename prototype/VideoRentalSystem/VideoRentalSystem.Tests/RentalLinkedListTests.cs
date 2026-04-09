using Microsoft.VisualStudio.TestTools.UnitTesting;
using VideoRentalSystem.DataStructures;
using VideoRentalSystem.Models;

namespace VideoRentalSystem.Tests;

[TestClass]
public class RentalLinkedListTests
{
    private static RentalVideo V(int id, string title)
    {
        return new RentalVideo
        {
            RentalID = id,
            Title = title,
            Genre = "Test",
            Director = "Tester",
            Year = 2000,
            RentalPrice = 1.50m,
            AvailableCopies = 2
        };
    }

    [TestMethod]
    public void Add_IncreasesCount()
    {
        var list = new RentalLinkedList();
        Assert.AreEqual(0, list.Count);

        bool ok = list.Add(V(1, "Alpha"));
        Assert.IsTrue(ok);
        Assert.AreEqual(1, list.Count);

        list.Add(V(2, "Beta"));
        Assert.AreEqual(2, list.Count);
    }

    [TestMethod]
    public void Remove_RemovesById()
    {
        var list = new RentalLinkedList();
        list.Add(V(10, "One"));
        list.Add(V(11, "Two"));

        bool removed = list.RemoveById(10);
        Assert.IsTrue(removed);
        Assert.AreEqual(1, list.Count);
        Assert.IsFalse(list.ContainsId(10));
        Assert.IsTrue(list.ContainsId(11));
    }

    [TestMethod]
    public void Remove_EmptyList_ReturnsFalse()
    {
        var list = new RentalLinkedList();
        bool removed = list.RemoveById(99);
        Assert.IsFalse(removed);
        Assert.AreEqual(0, list.Count);
    }

    [TestMethod]
    public void Search_Found_IgnoresCase()
    {
        var list = new RentalLinkedList();
        list.Add(V(3, "Matrix"));

        var hit = list.SearchByTitle("matrix");
        Assert.IsNotNull(hit);
        Assert.AreEqual(3, hit.RentalID);
    }

    [TestMethod]
    public void Search_NotFound_ReturnsNull()
    {
        var list = new RentalLinkedList();
        list.Add(V(4, "Other"));

        var hit = list.SearchByTitle("Nope");
        Assert.IsNull(hit);
    }

    [TestMethod]
    public void Add_DuplicateId_ReturnsFalse()
    {
        var list = new RentalLinkedList();
        Assert.IsTrue(list.Add(V(5, "First")));
        bool second = list.Add(V(5, "Same id"));
        Assert.IsFalse(second);
        Assert.AreEqual(1, list.Count);
    }

    [TestMethod]
    public void Search_BlankTitle_ReturnsNull()
    {
        var list = new RentalLinkedList();
        list.Add(V(6, "X"));
        Assert.IsNull(list.SearchByTitle(""));
        Assert.IsNull(list.SearchByTitle("   "));
    }
}
