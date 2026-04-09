namespace VRS
{
    public class Admin
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string Salt { get; set; }
    }
}
