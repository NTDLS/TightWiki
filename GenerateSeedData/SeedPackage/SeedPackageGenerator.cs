using NTDLS.SqliteDapperWrapper;
using System.IO.Compression;
using System.Text;
using System.Text.Json;
using TightWiki.Plugin.Models.Defaults;

namespace GenerateSeedData.SeedPackage
{
    /// <summary>
    /// Builds "Seed\tightwiki.seed.zip" - the provider-neutral seed package described in
    /// Database-Providers-Plan.md chapter 4.6b. It is read from Data\*.db (the same live, populated SQLite
    /// databases that <see cref="Program.GenerateDefaultsDatabase"/> reads from), and is meant to be consumed by
    /// TightWiki.Data.EfCore's EfDefaultsRepository when seeding a freshly created MSSQL/Postgres database -
    /// never at SQLite runtime, and never by this repo's ASP.NET Core app.
    ///
    /// Layout inside the zip (one JSON manifest per table, System.Text.Json, indented for readability/diffability):
    ///
    ///   ConfigurationGroup.json           - config.db ConfigurationGroup, raw dump
    ///   ConfigurationEntry.json           - config.db ConfigurationEntry, raw dump
    ///   MenuItem.json                     - config.db MenuItem, raw dump
    ///   Theme.json                        - config.db Theme, raw dump
    ///   FeatureTemplate.json              - pages.db FeatureTemplate joined to Page.Name (PageId is not portable)
    ///   PageStatistics.json                - statistics.db PageStatistics, ATTACHed to pages.db and joined to
    ///                                      Page.Navigation - one entry per reference page that actually has a
    ///                                      statistics row (not every default wiki page does); carries PageId
    ///                                      (portable now that Page.Id is 1:1, see TwDefaultWikiPage.Id) plus
    ///                                      Navigation as a readable fallback key
    ///   DefaultWikiPages/&lt;Namespace&gt;.json - pages.db Page+PageRevision, one file per namespace actually present
    ///                                      in pages.db (Builtin, Include, Sandbox, Wiki Help, and the default/root
    ///                                      "" namespace - the empty string namespace file is named "".json),
    ///                                      matching the ITwDefaultsRepository.GetDefaultWikiPages(namespace)
    ///                                      signature
    ///   Emoji.json                        - emoji.db Emoji metadata (Id, Name, MimeType) + a reference to the
    ///                                      corresponding Emoji/Images/* zip entry - no base64, see below
    ///   EmojiCategory.json                - emoji.db EmojiCategory, raw dump
    ///   Emoji/Images/&lt;Id&gt;&lt;ext&gt;      - the emoji image bytes, stored as their own zip entries rather than
    ///                                      base64-encoded inside Emoji.json (~18 MB of images would otherwise
    ///                                      bloat/slow down JSON parsing for no benefit)
    ///   AdminProfile.json                 - users.db Profile row for the built-in admin (Navigation='admin'),
    ///                                      display fields only (see TwDefaultProfile) - a single object, not an
    ///                                      array, since there is exactly one such row
    /// </summary>
    internal static class SeedPackageGenerator
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

        public static void Generate(string dbPath, string seedZipPath)
        {
            File.Delete(seedZipPath);

            using var configDb = new SqliteManagedInstance(Path.Combine(dbPath, "config.db"));
            using var pagesDb = new SqliteManagedInstance(Path.Combine(dbPath, "pages.db"));
            using var emojiDb = new SqliteManagedInstance(Path.Combine(dbPath, "emoji.db"));
            using var statisticsDb = new SqliteManagedInstance(Path.Combine(dbPath, "statistics.db"));
            using var usersDb = new SqliteManagedInstance(Path.Combine(dbPath, "users.db"));

            using var zipStream = new FileStream(seedZipPath, FileMode.CreateNew, FileAccess.Write);
            using var archive = new ZipArchive(zipStream, ZipArchiveMode.Create);

            Console.WriteLine("Generating: Seed\\tightwiki.seed.zip");

            Console.WriteLine("  Adding: ConfigurationGroup.json");
            var configurationGroups = configDb.Query<SeedConfigurationGroup>(@"Scripts\Seed\GetConfigurationGroups.sql");
            WriteJsonEntry(archive, "ConfigurationGroup.json", configurationGroups);

            Console.WriteLine("  Adding: ConfigurationEntry.json");
            var configurationEntries = configDb.Query<SeedConfigurationEntry>(@"Scripts\Seed\GetConfigurationEntries.sql");
            WriteJsonEntry(archive, "ConfigurationEntry.json", configurationEntries);

            Console.WriteLine("  Adding: MenuItem.json");
            var menuItems = configDb.Query<SeedMenuItem>(@"Scripts\Seed\GetMenuItems.sql");
            WriteJsonEntry(archive, "MenuItem.json", menuItems);

            Console.WriteLine("  Adding: Theme.json");
            var themes = configDb.Query<TwDefaultTheme>(@"Scripts\GetDefaultThemes.sql");
            WriteJsonEntry(archive, "Theme.json", themes);

            Console.WriteLine("  Adding: FeatureTemplate.json");
            var featureTemplates = pagesDb.Query<TwDefaultFeatureTemplate>(@"Scripts\GetFeatureTemplates.sql");
            WriteJsonEntry(archive, "FeatureTemplate.json", featureTemplates);

            var wikiPages = pagesDb.Query<TwDefaultWikiPage>(@"Scripts\GetDefaultDefaultWikiPages.sql");
            foreach (var namespaceGroup in wikiPages.GroupBy(p => p.Namespace).OrderBy(g => g.Key))
            {
                string entryName = $"DefaultWikiPages/{namespaceGroup.Key}.json";
                Console.WriteLine($"  Adding: {entryName}");
                WriteJsonEntry(archive, entryName, namespaceGroup.ToList());
            }

            //Images/files attached to default wiki pages (e.g. the "MC Music.png" on Home, the logo on Wiki
            //About). Raw bytes are embedded as base64 directly in the JSON manifest - unlike Emoji/Images/* below,
            //the total volume here is a few hundred KB (not ~18 MB), so a separate binary zip entry per file
            //would be needless ceremony.
            var pageFileAttachments = pagesDb.Query<TwDefaultPageFileAttachment>(@"Scripts\GetDefaultPageFileAttachments.sql");
            foreach (var namespaceGroup in pageFileAttachments.GroupBy(a => a.Namespace).OrderBy(g => g.Key))
            {
                string entryName = $"DefaultPageFileAttachments/{namespaceGroup.Key}.json";
                Console.WriteLine($"  Adding: {entryName}");
                WriteJsonEntry(archive, entryName, namespaceGroup.ToList());
            }

            //statistics.db is a separate physical SQLite file from pages.db, so the join to Page.Navigation needs
            //a real cross-database ATTACH (statistics.db as the primary connection, pages.db attached as
            //"pages_db") - the same mechanism TightWiki.Repository.StatisticsRepository.GetPageStatisticsPaged
            //uses at runtime (see its "o.Attach(\"pages.db\", \"pages_db\")"), not a plain in-process join like
            //FeatureTemplate/PageFileAttachments above (whose source tables both live inside pages.db already).
            Console.WriteLine("  Adding: PageStatistics.json");
            using (statisticsDb.Attach("pages.db", "pages_db"))
            {
                var pageStatistics = statisticsDb.Query<TwDefaultPageStatistic>(@"Scripts\GetDefaultPageStatistics.sql");
                WriteJsonEntry(archive, "PageStatistics.json", pageStatistics);
            }

            Console.WriteLine("  Adding: EmojiCategory.json");
            var emojiCategories = emojiDb.Query<SeedEmojiCategory>(@"Scripts\Seed\GetEmojiCategories.sql");
            WriteJsonEntry(archive, "EmojiCategory.json", emojiCategories);

            Console.WriteLine("  Adding: Emoji.json + Emoji/Images/*");
            var sourceEmojis = emojiDb.Query<SourceEmoji>(@"Scripts\Seed\GetEmoji.sql");
            var emojiManifest = new List<SeedEmoji>();
            foreach (var emoji in sourceEmojis)
            {
                //emoji.db stores Emoji.ImageData GZip-compressed (see TightWiki.Library.Utility.Compress/Decompress,
                //which the runtime FileController calls before handing bytes to MagickImage) - not raw image bytes.
                //Decompress here so the zip entry contains the actual image, then derive the extension from the
                //real (decompressed) byte signature rather than trusting the MimeType column blindly.
                var imageData = DecompressIfGZip(emoji.ImageData ?? []);
                string extension = GetExtensionFromContent(imageData) ?? GetExtensionForMimeType(emoji.MimeType);
                string imageEntryName = $"Emoji/Images/{emoji.Id}{extension}";

                var imageEntry = archive.CreateEntry(imageEntryName, CompressionLevel.Optimal);
                using (var entryStream = imageEntry.Open())
                {
                    entryStream.Write(imageData, 0, imageData.Length);
                }

                emojiManifest.Add(new SeedEmoji
                {
                    Id = emoji.Id,
                    Name = emoji.Name,
                    MimeType = emoji.MimeType,
                    ImageEntry = imageEntryName,
                });
            }
            WriteJsonEntry(archive, "Emoji.json", emojiManifest);

            Console.WriteLine($"  {emojiManifest.Count} emoji image(s) embedded.");

            Console.WriteLine("  Adding: AdminProfile.json");
            var adminProfile = usersDb.Query<TwDefaultProfile>(@"Scripts\GetDefaultAdminProfile.sql").FirstOrDefault();
            WriteJsonEntry(archive, "AdminProfile.json", adminProfile);
        }

        /// <summary>
        /// Serializes <paramref name="data"/> as indented JSON into a new zip entry named <paramref name="entryName"/>.
        /// </summary>
        private static void WriteJsonEntry<T>(ZipArchive archive, string entryName, T data)
        {
            var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
            using var entryStream = entry.Open();
            using var writer = new StreamWriter(entryStream, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            writer.Write(JsonSerializer.Serialize(data, JsonOptions));
        }

        /// <summary>
        /// Maps an emoji's stored MIME type to a file extension for its Emoji/Images/* zip entry. Used only as a
        /// fallback when <see cref="GetExtensionFromContent"/> can't recognize the actual bytes (e.g. SVG, which
        /// has no fixed binary signature). Falls back to ".bin" for anything unrecognized rather than failing the
        /// whole export over one odd row.
        /// </summary>
        private static string GetExtensionForMimeType(string mimeType) => mimeType.Trim().ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/gif" => ".gif",
            "image/jpeg" or "image/jpg" => ".jpg",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            "image/bmp" => ".bmp",
            _ => ".bin",
        };

        /// <summary>
        /// emoji.db's Emoji.ImageData column stores images GZip-compressed - see
        /// TightWiki.Library.Utility.Compress/Decompress, which the runtime's FileController calls on every
        /// ImageData read before decoding it as an image. Detects the GZip magic number (1F 8B) and decompresses
        /// so the Emoji/Images/* zip entry holds the actual image bytes instead of the compressed container.
        /// Data that isn't GZip-compressed (magic number doesn't match) is returned unchanged.
        /// </summary>
        private static byte[] DecompressIfGZip(byte[] data)
        {
            if (data.Length < 2 || data[0] != 0x1F || data[1] != 0x8B)
            {
                return data;
            }

            using var compressedStream = new MemoryStream(data);
            using var decompressor = new GZipStream(compressedStream, CompressionMode.Decompress);
            using var decompressedStream = new MemoryStream();
            decompressor.CopyTo(decompressedStream);
            return decompressedStream.ToArray();
        }

        /// <summary>
        /// Derives a file extension from an image's actual byte signature (magic number) rather than trusting the
        /// MimeType column blindly. Returns null when the content doesn't match a known binary signature (e.g.
        /// SVG, which is text-based) so the caller can fall back to <see cref="GetExtensionForMimeType"/>.
        /// </summary>
        private static string? GetExtensionFromContent(byte[] data)
        {
            if (data.Length >= 8 && data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47
                && data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            {
                return ".png";
            }

            if (data.Length >= 6 && data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38)
            {
                return ".gif";
            }

            if (data.Length >= 3 && data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            {
                return ".jpg";
            }

            if (data.Length >= 12 && data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46
                && data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            {
                return ".webp";
            }

            if (data.Length >= 2 && data[0] == 0x42 && data[1] == 0x4D)
            {
                return ".bmp";
            }

            return null;
        }
    }
}
