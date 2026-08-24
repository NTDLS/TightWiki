using System.IO.Compression;
using System.Text.Json;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using TightWiki.Plugin.Models.Defaults;

namespace TightWiki.Data.EfCore.Seeding
{
    /// <summary>
    /// Reads "Seed\tightwiki.seed.zip" - the provider-neutral seed package produced by GenerateSeedData's
    /// SeedPackageGenerator (see Database-Providers-Plan.md chapter 4.6b) - and exposes it through the shared
    /// <see cref="ITwDefaultsRepository"/> contract so that MSSQL/Postgres driver projects can seed a freshly
    /// created, otherwise-empty database without ever touching SQLite at runtime.
    /// </summary>
    /// <remarks>
    /// Deliberately depends on nothing beyond <see cref="System.IO.Compression"/> and
    /// <see cref="System.Text.Json"/> (both part of the BCL) plus TightWiki.Plugin (for the shared
    /// interface/model types) - see the "no SQLite in TightWiki.Data.EfCore" rule in Database-Providers-Plan.md
    /// chapters 1, 4.6b and 9. Not thread-safe for concurrent calls against the same instance (mirrors
    /// <see cref="ZipArchive"/> itself not being safe for concurrent entry reads) - seeding a freshly created
    /// database is inherently a single, sequential pass, so this is not a practical limitation.
    /// </remarks>
    public sealed class EfDefaultsRepository : ITwDefaultsRepository, IDisposable
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly string _seedPackagePath;
        private ZipArchive? _archive;

        /// <summary>
        /// The conventional, non-configurable location of the seed package - "Seed\tightwiki.seed.zip" next to
        /// the running application - mirroring how <see cref="TightWiki.Library.PluginLoader"/> locates
        /// "Plugins\*.dll" via <see cref="Environment.CurrentDirectory"/> rather than a new configuration key
        /// (see Database-Providers-Plan.md chapter 4.6b: "bez nového konfiguračního klíče").
        /// </summary>
        public static string DefaultSeedPackagePath
            => Path.Combine(Environment.CurrentDirectory, "Seed", "tightwiki.seed.zip");

        /// <param name="seedPackagePath">
        /// Optional override of the seed package path (primarily for tests/tooling). When omitted, resolves to
        /// <see cref="DefaultSeedPackagePath"/>.
        /// </param>
        public EfDefaultsRepository(string? seedPackagePath = null)
        {
            _seedPackagePath = seedPackagePath ?? DefaultSeedPackagePath;
        }

        private ZipArchive Archive => _archive ??= ZipFile.OpenRead(_seedPackagePath);

        /// <inheritdoc/>
        public async Task<List<TwDefaultConfiguration>> GetDefaultConfigurationGroups()
        {
            var groups = await ReadJsonEntryAsync<List<SeedConfigurationGroup>>("ConfigurationGroup.json");

            return groups.Select(g => new TwDefaultConfiguration
            {
                ConfigurationGroupId = g.Id,
                ConfigurationGroupName = g.Name,
                ConfigurationGroupDescription = g.Description ?? string.Empty,
            }).ToList();
        }

        /// <inheritdoc/>
        public async Task<List<TwDefaultConfiguration>> GetDefaultConfigurations()
        {
            //ConfigurationGroup.json and ConfigurationEntry.json are normalized (one row per group / one row per
            //entry, linked by ConfigurationGroupId) - unlike the flattened SQLite DefaultConfiguration table that
            //DefaultsRepository.GetDefaultConfigurations queries, so the join has to happen here in memory.
            var groups = await ReadJsonEntryAsync<List<SeedConfigurationGroup>>("ConfigurationGroup.json");
            var entries = await ReadJsonEntryAsync<List<SeedConfigurationEntry>>("ConfigurationEntry.json");
            var groupsById = groups.ToDictionary(g => g.Id);

            return entries.Select(e =>
            {
                var group = groupsById[e.ConfigurationGroupId];

                return new TwDefaultConfiguration
                {
                    ConfigurationGroupId = group.Id,
                    ConfigurationEntryId = e.Id,
                    ConfigurationGroupName = group.Name,
                    ConfigurationEntryName = e.Name,
                    Value = e.Value ?? string.Empty,
                    DataTypeId = e.DataTypeId,
                    ConfigurationGroupDescription = group.Description ?? string.Empty,
                    ConfigurationEntryDescription = e.Description ?? string.Empty,
                    IsEncrypted = e.IsEncrypted,
                    IsRequired = e.IsRequired,
                };
            }).ToList();
        }

        /// <inheritdoc/>
        public Task<List<TwDefaultTheme>> GetDefaultThemes()
            => ReadJsonEntryAsync<List<TwDefaultTheme>>("Theme.json");

        /// <inheritdoc/>
        public async Task<List<TwDefaultWikiPage>> GetDefaultWikiPages(string namespaceName)
        {
            var entryName = $"DefaultWikiPages/{namespaceName}.json";
            var entry = Archive.GetEntry(entryName);
            if (entry == null)
            {
                //Mirrors GetDefaultWikiPages.sql's "WHERE Namespace = @Namespace" behavior of returning an empty
                //result set for a namespace with no matching rows, rather than throwing.
                return [];
            }

            await using var stream = entry.Open();
            var result = await JsonSerializer.DeserializeAsync<List<TwDefaultWikiPage>>(stream, JsonOptions);
            return result ?? [];
        }

        /// <inheritdoc/>
        public Task<List<TwDefaultFeatureTemplate>> GetDefaultFeatureTemplates()
            => ReadJsonEntryAsync<List<TwDefaultFeatureTemplate>>("FeatureTemplate.json");

        /// <inheritdoc/>
        public async Task<List<TwDefaultPageFileAttachment>> GetDefaultPageFileAttachments(string namespaceName)
        {
            var entryName = $"DefaultPageFileAttachments/{namespaceName}.json";
            var entry = Archive.GetEntry(entryName);
            if (entry == null)
            {
                //Mirrors GetDefaultWikiPages's "no matching namespace" handling - an empty result, not an error.
                return [];
            }

            await using var stream = entry.Open();
            var result = await JsonSerializer.DeserializeAsync<List<TwDefaultPageFileAttachment>>(stream, JsonOptions);
            return result ?? [];
        }

        /// <inheritdoc/>
        public Task<List<TwDefaultPageStatistic>> GetDefaultPageStatistics()
            => ReadJsonEntryAsync<List<TwDefaultPageStatistic>>("PageStatistics.json");

        /// <inheritdoc/>
        public Task<List<TwDefaultEmoji>> GetDefaultEmojis()
            => ReadJsonEntryAsync<List<TwDefaultEmoji>>("Emoji.json");

        /// <inheritdoc/>
        public Task<List<TwDefaultEmojiCategory>> GetDefaultEmojiCategories()
            => ReadJsonEntryAsync<List<TwDefaultEmojiCategory>>("EmojiCategory.json");

        /// <inheritdoc/>
        public Task<List<TwMenuItem>> GetDefaultMenuItems()
            => ReadJsonEntryAsync<List<TwMenuItem>>("MenuItem.json");

        /// <summary>
        /// Reads the raw image bytes for a <see cref="TwDefaultEmoji"/> returned by <see cref="GetDefaultEmojis"/>,
        /// via its <see cref="TwDefaultEmoji.ImageEntry"/> zip-relative path (e.g. "Emoji/Images/12.png").
        /// </summary>
        /// <remarks>
        /// Intentionally not part of <see cref="ITwDefaultsRepository"/> - that shared contract keeps
        /// <see cref="TwDefaultEmoji"/> metadata-only by design (see its own doc comment), so that callers who
        /// only need the metadata don't pay for reading ~18 MB of images they don't want. Seeding code that does
        /// need the bytes calls this once per emoji, after reading the metadata via
        /// <see cref="GetDefaultEmojis"/>.
        /// </remarks>
        public async Task<byte[]> ReadEmojiImageBytes(string imageEntry)
        {
            var entry = Archive.GetEntry(imageEntry)
                ?? throw new FileNotFoundException(
                    $"Seed package '{_seedPackagePath}' does not contain image entry '{imageEntry}'.", imageEntry);

            await using var stream = entry.Open();
            using var buffer = new MemoryStream();
            await stream.CopyToAsync(buffer);
            return buffer.ToArray();
        }

        private async Task<T> ReadJsonEntryAsync<T>(string entryName)
        {
            var entry = Archive.GetEntry(entryName)
                ?? throw new FileNotFoundException(
                    $"Seed package '{_seedPackagePath}' does not contain the expected entry '{entryName}'.", entryName);

            await using var stream = entry.Open();
            var result = await JsonSerializer.DeserializeAsync<T>(stream, JsonOptions);
            return result ?? throw new InvalidDataException(
                $"Seed package '{_seedPackagePath}' entry '{entryName}' deserialized to null.");
        }

        /// <summary>
        /// Closes the underlying <see cref="ZipArchive"/>, if it was ever opened.
        /// </summary>
        public void Dispose() => _archive?.Dispose();

        /// <summary>
        /// Mirrors a "Data\config.db ConfigurationGroup" row as written verbatim (including its Id) into
        /// "ConfigurationGroup.json" by GenerateSeedData.SeedPackage.SeedPackageGenerator - keep this shape in
        /// sync with GenerateSeedData.SeedPackage.SeedConfigurationGroup.
        /// </summary>
        private sealed class SeedConfigurationGroup
        {
            public int Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
        }

        /// <summary>
        /// Mirrors a "Data\config.db ConfigurationEntry" row as written verbatim into "ConfigurationEntry.json" by
        /// GenerateSeedData.SeedPackage.SeedPackageGenerator - keep this shape in sync with
        /// GenerateSeedData.SeedPackage.SeedConfigurationEntry.
        /// </summary>
        private sealed class SeedConfigurationEntry
        {
            public int Id { get; set; }
            public int ConfigurationGroupId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Value { get; set; }
            public int DataTypeId { get; set; }
            public string? Description { get; set; }
            public bool IsEncrypted { get; set; }
            public bool IsRequired { get; set; }
        }
    }
}
