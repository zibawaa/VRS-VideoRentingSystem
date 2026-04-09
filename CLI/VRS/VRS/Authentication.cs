using System;
using System.Security.Cryptography;
using System.Text;

namespace VRS
{
    public static class Authentication
    {
        public static string GenerateSalt()
        {
            byte[] saltBytes = new byte[16];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        public static string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var combined = Encoding.UTF8.GetBytes(password + salt);
                var hash = sha256.ComputeHash(combined);
                return Convert.ToBase64String(hash);
            }
        }

        public static bool VerifyPassword(string enteredPassword, string storedHash, string storedSalt)
        {
            string hashOfInput = HashPassword(enteredPassword, storedSalt);
            return hashOfInput == storedHash;
        }
    }
}
