﻿using Microsoft.AspNetCore.Http;
using System.IO.Compression;

namespace TightWiki.Library
{
    public static class Utility
    {
        public static int PadVersionString(string versionString, int padLength = 3)
            => int.Parse(string.Join("", versionString.Split('.').Select(x => x.Trim().PadLeft(padLength, '0'))));

        public static string SanitizeAccountName(string fileName, char[]? extraInvalidCharacters = null)
        {
            // Get array of invalid characters for file names
            var invalidChars = Path.GetInvalidFileNameChars().ToList();

            if (extraInvalidCharacters != null)
            {
                invalidChars.AddRange(extraInvalidCharacters);
            }

            foreach (char invalidChar in invalidChars)
            {
                fileName = fileName.Replace(invalidChar, '_');
            }

            return fileName.Replace("__", "_").Replace("__", "_");
        }

        /// <summary>
        /// Take a height and width and enforces a max on both dimensions while maintaining the ratio.
        /// </summary>
        public static (int Width, int Height) ScaleToMaxOf(int originalWidth, int originalHeight, int maxSize)
        {
            // Calculate aspect ratio
            float aspectRatio = (float)originalWidth / originalHeight;

            // Determine new dimensions based on the larger dimension
            int newWidth, newHeight;
            if (originalWidth > originalHeight)
            {
                // Scale down the width to the maxSize and calculate the height
                newWidth = maxSize;
                newHeight = (int)(maxSize / aspectRatio);
            }
            else
            {
                // Scale down the height to the maxSize and calculate the width
                newHeight = maxSize;
                newWidth = (int)(maxSize * aspectRatio);
            }

            return (newWidth, newHeight);
        }

        public static List<string> SplitToTokens(string? tokenString)
        {
            var tokens = (tokenString ?? string.Empty).Trim().ToLowerInvariant()
                .Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            return tokens;
        }

        public static string GetMimeType(string fileName)
        {
            MimeTypes.TryGetContentType(fileName, out var contentType);
            return contentType ?? "application/octet-stream";
        }

        public static byte[] ConvertHttpFileToBytes(IFormFile image)
        {
            using var stream = image.OpenReadStream();
            using BinaryReader reader = new BinaryReader(stream);
            return reader.ReadBytes((int)image.Length);
        }

        public static byte[] Compress(byte[]? data)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }

            using var compressedStream = new MemoryStream();
            using (var compressor = new GZipStream(compressedStream, CompressionMode.Compress))
            {
                compressor.Write(data, 0, data.Length);
            }
            return compressedStream.ToArray();
        }

        public static byte[] Decompress(byte[] data)
        {
            if (data == null)
            {
                return Array.Empty<byte>();
            }

            using var compressedStream = new MemoryStream(data);
            using var decompressor = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            decompressor.CopyTo(decompressedStream);
            return decompressedStream.ToArray();
        }
    }
}
