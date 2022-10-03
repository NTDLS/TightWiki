using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace AsapWiki.Shared.Library
{
    public static class Security
    {
        public static string MachineKey
        {
            get
            {
                return Sha1(Environment.MachineName);
            }
        }

        public static string GenerateRandomString()
        {
            using (var crypto = Aes.Create())
            {
                crypto.GenerateKey();
                return Convert.ToBase64String(crypto.Key).Replace("/", "").Replace("=", "").Replace("+", "");
            }
        }

        public static string Sha1(string text)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(text);
            SHA1CryptoServiceProvider cryptoTransformSHA1 = new SHA1CryptoServiceProvider();
            string hash = BitConverter.ToString(cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "");
            return hash;
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

        public static byte[] Encrypt(byte[] data, byte[] key)
        {
            using (var aesAlg = Aes.Create())
            {
                aesAlg.Mode = CipherMode.CBC;
                using (var encryptor = aesAlg.CreateEncryptor(key, aesAlg.IV))
                using (var msEncrypt = new MemoryStream())
                {
                    msEncrypt.Write(aesAlg.IV, 0, aesAlg.IV.Length);

                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        csEncrypt.Write(data, 0, data.Length);
                    }

                    return msEncrypt.ToArray();
                }
            }
        }

        public static byte[] Decrypt(byte[] encrypted, byte[] key)
        {
            var iv = new byte[16];
            Buffer.BlockCopy(encrypted, 0, iv, 0, iv.Length);
            using (var aesAlg = Aes.Create())
            {
                aesAlg.Mode = CipherMode.CBC;
                using (var decryptor = aesAlg.CreateDecryptor(key, iv))
                using (var msDecrypt = new MemoryStream(encrypted, iv.Length, encrypted.Length - iv.Length))
                using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                using (var resultStream = new MemoryStream())
                {
                    csDecrypt.CopyTo(resultStream);
                    return resultStream.ToArray();
                }
            }
        }
    }
}
        