using System;
using System.Security.Cryptography;
using System.Text;

namespace AsapWiki.Shared.Classes
{
    public static class Security
    {
        public static string GenerateRandomString()
        {
            using (var crypto = Aes.Create())
            {
                crypto.GenerateKey();
                return Convert.ToBase64String(crypto.Key).Replace("/", "").Replace("=", "").Replace("+", "");
            }
        }

        public static string Sha256(string value)
        {
            using (var crypt = new System.Security.Cryptography.SHA256Managed())
            {
                var hash = new StringBuilder();
                byte[] crypto = crypt.ComputeHash(Encoding.UTF8.GetBytes(value));
                foreach (byte theByte in crypto)
                {
                    hash.Append(theByte.ToString("x2"));
                }
                return hash.ToString();
            }
        }
    }
}
