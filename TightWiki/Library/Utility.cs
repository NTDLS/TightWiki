﻿using Microsoft.AspNetCore.StaticFiles;
using System.IO.Compression;

namespace TightWiki.Library
{
    public static class Utility
    {
        /// <summary>
        /// Take a height and width and enforces a max on both dimensions while maintaining the ratio.
        /// </summary>
        /// <param name="originalWidth"></param>
        /// <param name="originalHeight"></param>
        /// <param name="maxSize"></param>
        /// <returns></returns>
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
            var tokens = (tokenString ?? string.Empty).Trim().ToLower()
                .Split(new char[] { ' ', ',', '\t' }, StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            return tokens;
        }

        public static int SimpleChecksum(string input)
        {
            int checksum = 0;
            foreach (char c in input)
            {
                checksum += (c << 3) + c;
            }

            return checksum % int.MaxValue;
        }


        public static int SimpleChecksum(byte[] input)
        {
            int checksum = 0;
            foreach (char c in input)
            {
                checksum += (c << 3) + c;
            }

            return checksum % int.MaxValue;
        }

        public static string GetMimeType(string fileName)
        {
            string? contentType;
            new FileExtensionContentTypeProvider().TryGetContentType(fileName, out contentType);
            return contentType ?? "application/octet-stream";
        }

        public static byte[] ConvertHttpFileToBytes(IFormFile image)
        {
            using var stream = image.OpenReadStream();
            using BinaryReader reader = new BinaryReader(stream);
            byte[] imageBytes = reader.ReadBytes((int)image.Length);
            return imageBytes;
        }

        public static T ConvertTo<T>(string? value, T defaultValue)
            => ConvertTo<T>(value) ?? defaultValue;

        public static T? ConvertTo<T>(string? value)
        {
            if (value == null)
            {
                return default;
            }

            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Error converting value [{value}] to integer.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(float))
            {
                if (float.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Error converting value [{value}] to float.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(double))
            {
                if (double.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Error converting value [{value}] to double.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(bool))
            {
                value = value.ToLower();

                if (value.All(char.IsNumber))
                {
                    if (int.Parse(value) != 0)
                        value = "true";
                    else
                        value = "false";
                }

                if (bool.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Error converting value [{value}] to boolean.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else
            {
                throw new Exception($"Unsupported conversion type.");
            }
        }

        public static byte[] Compress(byte[]? data)
        {
            if (data == null)
            {
                return new byte[0];
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
                return new byte[0];
            }

            using var compressedStream = new MemoryStream(data);
            using var decompressor = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            decompressor.CopyTo(decompressedStream);
            return decompressedStream.ToArray();
        }
    }
}
