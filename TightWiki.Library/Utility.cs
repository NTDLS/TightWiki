using Microsoft.AspNetCore.Http;
using System.Globalization;
using System.IO.Compression;
using System.Text;

namespace TightWiki.Library
{
    public static class Utility
    {
        public static readonly char[] UnsafePageNameCharacters = ['/', '\\', '<', '>'];

        public static bool PageNameContainsUnsafeCharacters(string input)
            => input.IndexOfAny(UnsafePageNameCharacters) >= 0;

        public static int CountOccurrencesOf(string input, string substring)
            => input.Split(substring, StringSplitOptions.None).Length - 1;

        public static int PadVersionString(string versionString, int padLength = 3)
            => int.Parse("1" + string.Join("", versionString.Split('.').Select(x => x.Trim().PadLeft(padLength, '0'))));

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

        public static List<string> SplitToTokens(string? tokenString)
        {
            var tokens = (tokenString ?? string.Empty).Trim().ToLowerInvariant()
                .Split([' ', ',', '\t'], StringSplitOptions.RemoveEmptyEntries).Distinct().ToList();
            return tokens;
        }

        /// <summary>
        /// Splits text into search tokens: runs of letters and digits, without accents, with the original case.
        /// Everything else (whitespace, punctuation, symbols) separates tokens and is never part of one, so "wiki," yields "wiki".
        /// Apostrophes are dropped without splitting, so "don't" yields "dont".
        /// This is used for both indexing pages and parsing search text so that the two always agree.
        /// </summary>
        public static List<string> TokenizeText(string? text)
        {
            var tokens = new List<string>();
            var current = new StringBuilder();

            foreach (var c in FoldDiacritics(text))
            {
                if (char.IsLetterOrDigit(c))
                {
                    current.Append(c);
                }
                else if (c != '\'' && c != '’') //Apostrophes are dropped, not separators.
                {
                    if (current.Length > 0)
                    {
                        tokens.Add(current.ToString());
                        current.Clear();
                    }
                }
            }

            if (current.Length > 0)
            {
                tokens.Add(current.ToString());
            }

            return tokens;
        }

        /// <summary>
        /// Removes accents and other diacritical marks so that "café" and "cafe" compare as equal (e.g. é -> e, ñ -> n).
        /// Letters that do not decompose into a base letter plus a mark are mapped explicitly (e.g. ß -> ss, ø -> o).
        /// The result is not lower-cased.
        /// </summary>
        public static string FoldDiacritics(string? text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            //Fast path, plain ASCII has nothing to fold.
            if (text.All(c => c < 0x80))
            {
                return text;
            }

            var sb = new StringBuilder(text.Length);
            foreach (var c in text.Normalize(NormalizationForm.FormD))
            {
                switch (c)
                {
                    case 'ß': sb.Append("ss"); continue;
                    case 'æ': sb.Append("ae"); continue;
                    case 'Æ': sb.Append("AE"); continue;
                    case 'œ': sb.Append("oe"); continue;
                    case 'Œ': sb.Append("OE"); continue;
                    case 'ø': sb.Append('o'); continue;
                    case 'Ø': sb.Append('O'); continue;
                    case 'đ': sb.Append('d'); continue;
                    case 'Đ': sb.Append('D'); continue;
                    case 'ł': sb.Append('l'); continue;
                    case 'Ł': sb.Append('L'); continue;
                }

                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }

            return sb.ToString().Normalize(NormalizationForm.FormC);
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
