using System.Security.Cryptography;

namespace TightWiki.Library.Security
{
    /// <summary>
    /// Generates personal API tokens and hashes them for storage. Only the hash of a token is ever stored.
    /// </summary>
    public static class ApiTokenUtility
    {
        private const int TokenByteCount = 32;

        /// <summary>
        /// Generates a new random token as a lowercase hex string (64 characters).
        /// </summary>
        public static string GenerateToken()
            => Convert.ToHexStringLower(RandomNumberGenerator.GetBytes(TokenByteCount));

        /// <summary>
        /// Returns the SHA-256 hash of the token as a lowercase hex string.
        /// </summary>
        public static string HashToken(string token)
            => SecurityUtility.Sha256(token);
    }
}
