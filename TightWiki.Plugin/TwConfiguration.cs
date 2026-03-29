using TightWiki.Plugin.Models;

namespace TightWiki.Plugin
{
    public class TwConfiguration
    {
        public readonly string[] AllowableImageTypes = ["image/png", "image/jpeg", "image/bmp", "image/gif", "image/tiff"];

        /// <summary>
        /// The base path of the wiki, used for generating links.
        /// </summary>
        public string BasePath { get; set; } = string.Empty;
        public Theme SystemTheme { get; set; } = new();
        public bool IsDebug { get; set; }
        public bool AllowSignup { get; set; }
        public bool EnableLDAPAuthentication { get; set; }

        public List<Emoji> Emojis { get; set; } = new();
        public string BrandImageSmall { get; set; } = string.Empty;

        /// <summary>
        /// Name of the site.
        /// </summary>
        public string Name { get; set; } = string.Empty;
        public string FooterBlurb { get; set; } = string.Empty;
        public string Copyright { get; set; } = string.Empty;
        public List<MenuItem> MenuItems { get; set; } = new();
        public string HTMLHeader { get; set; } = string.Empty;
        public string HTMLFooter { get; set; } = string.Empty;
        public string HTMLPreBody { get; set; } = string.Empty;
        public string HTMLPostBody { get; set; } = string.Empty;
        public bool IncludeWikiDescriptionInMeta { get; set; }
        public bool IncludeWikiTagsInMeta { get; set; }
        public bool EnablePageComments { get; set; }
        public bool EnablePublicProfiles { get; set; }
        public bool FixedMenuPosition { get; set; }
        public bool ShowCommentsOnPageFooter { get; set; }
        public bool IncludeSearchOnNavbar { get; set; }
        public int PageCacheSeconds { get; set; }
        public int PaginationSize { get; set; }
        public int CacheMemoryLimitMB { get; set; }
        public int DefaultProfileRecentlyModifiedCount { get; set; }
        public bool PreLoadAnimatedEmojis { get; set; } = true;
        public bool RecordCompilationMetrics { get; set; }
        public bool ShowLastModifiedOnPageFooter { get; set; }
        public bool ShowChangeSummaryWhenEditing { get; set; }
        public bool RequireChangeSummaryWhenEditing { get; set; }
        public string DefaultTimeZone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public int DefaultEmojiHeight { get; set; }
        public int MaxAvatarFileSize { get; set; } = 1048576;
        public int MaxAttachmentFileSize { get; set; } = 5242880;
        public int MaxEmojiFileSize { get; set; } = 524288;
    }
}
