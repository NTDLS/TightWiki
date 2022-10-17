using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace TightWiki.Shared.Library
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

        public static string GenerateRandomString(int maxLength = 10)
        {
            using (var crypto = Aes.Create())
            {
                crypto.GenerateKey();
                var result = Convert.ToBase64String(crypto.Key).Replace("/", "").Replace("=", "").Replace("+", "");

                if (result.Length > maxLength)
                {
                    result = result.Substring(0, maxLength);
                }

                return result.ToUpper();
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

        public static string EncryptString(string key, string plainText)
        {
            using (var hashstring = new SHA256Managed())
            using (Aes aes = Aes.Create())
            {
                byte[] iv = new byte[16];
                byte[] keyBytes = hashstring.ComputeHash(Encoding.Unicode.GetBytes(key));
                byte[] vector = (byte[])keyBytes.Clone();

                for (int i = 0; i < 16; i++)
                {
                    iv[i] = vector[i];
                }

                aes.Key = keyBytes;
                aes.IV = iv;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream())
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                {
                    using (StreamWriter streamWriter = new StreamWriter(cryptoStream))
                    {
                        streamWriter.Write(plainText);
                    }

                    return Convert.ToBase64String(memoryStream.ToArray());
                }
            }
        }

        public static string DecryptString(string key, string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            using (var hashstring = new SHA256Managed())
            using (Aes aes = Aes.Create())
            {
                byte[] iv = new byte[16];
                byte[] keyBytes = hashstring.ComputeHash(Encoding.Unicode.GetBytes(key));
                byte[] vector = (byte[])keyBytes.Clone();

                for (int i = 0; i < 16; i++)
                {
                    iv[i] = vector[i];
                }

                aes.Key = keyBytes;
                aes.IV = iv;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (MemoryStream memoryStream = new MemoryStream(buffer))
                using (CryptoStream cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                using (StreamReader streamReader = new StreamReader(cryptoStream))
                {
                    return streamReader.ReadToEnd();
                }
            }
        }
    }
}
