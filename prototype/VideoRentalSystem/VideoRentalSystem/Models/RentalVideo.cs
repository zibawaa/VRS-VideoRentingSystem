namespace VideoRentalSystem.Models;

// one rental row — matches the sqlite table columns
public class RentalVideo
{
    public int RentalID { get; set; }
    public string Title { get; set; } = "";
    public string Genre { get; set; } = "";
    public string Director { get; set; } = "";
    public int Year { get; set; }
    public decimal RentalPrice { get; set; }
    public int AvailableCopies { get; set; }
}
