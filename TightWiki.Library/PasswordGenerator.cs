using System.Security.Cryptography;

namespace TightWiki.Library
{
    public static class PasswordGenerator
    {
        private const string Lower = "abcdefghijklmnopqrstuvwxyz";
        private const string Upper = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string Digits = "0123456789";
        private const string Special = "!@#$%^&*()-_=+[]{}|;:,.<>?";

        private static readonly string All = Lower + Upper + Digits + Special;

        public static string Generate(int length = 16)
        {
            if (length < 8)
                throw new ArgumentException("Password length must be at least 8.");

            var password = new char[length];

            // Ensure at least one of each category
            password[0] = GetRandomChar(Lower);
            password[1] = GetRandomChar(Upper);
            password[2] = GetRandomChar(Digits);
            password[3] = GetRandomChar(Special);

            // Fill the rest randomly
            for (int i = 4; i < length; i++)
            {
                password[i] = GetRandomChar(All);
            }

            // Shuffle to avoid predictable positions
            return new string(password.OrderBy(_ => GetRandomInt()).ToArray());
        }

        private static char GetRandomChar(string chars)
        {
            return chars[GetRandomInt(chars.Length)];
        }

        private static int GetRandomInt(int maxExclusive = int.MaxValue)
        {
            return RandomNumberGenerator.GetInt32(maxExclusive);
        }
    }
}
