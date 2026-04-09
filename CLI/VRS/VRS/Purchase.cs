using System;

namespace VRS
{
    public class Purchase
    {
        public int PurchaseID { get; set; }
        public int FanID { get; set; }
        public int VideoID { get; set; }
        public DateTime PurchaseDate { get; set; }
        public double Price { get; set; }
    }
}
