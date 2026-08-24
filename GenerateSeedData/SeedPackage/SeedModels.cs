namespace GenerateSeedData.SeedPackage
{
    /// <summary>
    /// Raw dump of a Data\config.db "ConfigurationGroup" row, written verbatim (including its Id) into
    /// Config/ConfigurationGroup.json so that ConfigurationEntry.ConfigurationGroupId keeps meaning across
    /// providers without needing a name-based re-link at import time.
    /// </summary>
    internal class SeedConfigurationGroup
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Raw dump of a Data\config.db "ConfigurationEntry" row, written verbatim into Config/ConfigurationEntry.json.
    /// </summary>
    internal class SeedConfigurationEntry
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

    /// <summary>
    /// Raw dump of a Data\config.db "MenuItem" row, written verbatim into Config/MenuItem.json.
    /// </summary>
    internal class SeedMenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public int Ordinal { get; set; }
    }

    /// <summary>
    /// The row shape read from Data\emoji.db "Emoji" (includes the binary image), used only while building the
    /// seed package - never serialized to JSON directly (see <see cref="SeedEmoji"/>).
    /// </summary>
    internal class SourceEmoji
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public byte[]? ImageData { get; set; }
    }

    /// <summary>
    /// Metadata-only projection of a Data\emoji.db "Emoji" row written into Emoji/Emoji.json. The binary image
    /// itself is stored as its own zip entry (see Emoji/Images/*) - <see cref="ImageEntry"/> is the zip-relative
    /// path to that entry.
    /// </summary>
    internal class SeedEmoji
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string MimeType { get; set; } = string.Empty;
        public string ImageEntry { get; set; } = string.Empty;
    }

    /// <summary>
    /// Raw dump of a Data\emoji.db "EmojiCategory" row, written verbatim into Emoji/EmojiCategory.json.
    /// </summary>
    internal class SeedEmojiCategory
    {
        public int Id { get; set; }
        public int EmojiId { get; set; }
        public string Category { get; set; } = string.Empty;
    }
}
