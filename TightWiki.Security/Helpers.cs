﻿using System.Security.Cryptography;
using System.Text;

namespace TightWiki.Security
{
    public static class Helpers
    {
        private static string? _machineKey;
        public static string MachineKey
            => _machineKey ??= Sha1(Environment.MachineName);

        public static string GenerateRandomString(int maxLength = 10)
        {
            using var crypto = Aes.Create();
            crypto.GenerateKey();
            var result = Convert.ToBase64String(crypto.Key).Replace("/", "").Replace("=", "").Replace("+", "");

            if (result.Length > maxLength)
            {
                result = result.Substring(0, maxLength);
            }

            return result.ToUpperInvariant();
        }

        public static int Crc32(string text)
            => (int)(new Crc32()).Get(Encoding.Unicode.GetBytes(text));

        public static int Crc32(byte[] bytes)
            => (int)(new Crc32()).Get(bytes);

        public static string Sha1(string text)
            => BitConverter.ToString(SHA1.HashData(Encoding.Unicode.GetBytes(text))).Replace("-", "");

        public static string Sha256(string value)
        {
            var hash = new StringBuilder();
            byte[] crypto = SHA256.HashData(Encoding.UTF8.GetBytes(value));
            foreach (byte theByte in crypto)
            {
                hash.Append(theByte.ToString("x2"));
            }
            return hash.ToString();
        }

        public static string EncryptString(string key, string plainText)
        {
            using var aes = Aes.Create();
            byte[] iv = new byte[16];
            byte[] keyBytes = SHA256.HashData(Encoding.Unicode.GetBytes(key));
            byte[] vector = (byte[])keyBytes.Clone();

            for (int i = 0; i < 16; i++)
            {
                iv[i] = vector[i];
            }

            aes.Key = keyBytes;
            aes.IV = iv;

            var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

            using var memoryStream = new MemoryStream();
            using var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write);
            using (var streamWriter = new StreamWriter(cryptoStream))
            {
                streamWriter.Write(plainText);
            }

            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public static string DecryptString(string key, string cipherText)
        {
            byte[] buffer = Convert.FromBase64String(cipherText);

            using var aes = Aes.Create();
            byte[] iv = new byte[16];
            byte[] keyBytes = SHA256.HashData(Encoding.Unicode.GetBytes(key));
            byte[] vector = (byte[])keyBytes.Clone();

            for (int i = 0; i < 16; i++)
            {
                iv[i] = vector[i];
            }

            aes.Key = keyBytes;
            aes.IV = iv;

            var cryptoTransform = aes.CreateDecryptor(aes.Key, aes.IV);

            using var memoryStream = new MemoryStream(buffer);
            using var cryptoStream = new CryptoStream(memoryStream, cryptoTransform, CryptoStreamMode.Read);
            using var streamReader = new StreamReader(cryptoStream);
            return streamReader.ReadToEnd();
        }
    }
}
