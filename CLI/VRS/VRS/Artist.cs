namespace VRS
{
    public class Artist
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
        public double TotalEarnings { get; set; }
    }
}
