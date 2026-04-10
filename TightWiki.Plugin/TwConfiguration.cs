using TightWiki.Plugin.Models;

namespace TightWiki.Plugin
{
    /// <summary>
    /// Represents the configuration settings for a wiki instance, including site appearance, authentication, feature
    /// toggles, and operational limits.
    /// </summary>
    public class TwConfiguration
    {
        /// <summary>
        /// The set of MIME types that are permitted for image uploads.
        /// </summary>
        public readonly string[] AllowableImageTypes = ["image/png", "image/jpeg", "image/bmp", "image/gif", "image/tiff"];

        /// <summary>
        /// The base path of the wiki, used for generating links.
        /// </summary>
        public string BasePath { get; set; } = string.Empty;

        /// <summary>
        /// The default visual theme applied to the wiki UI.
        /// </summary>
        public TwTheme SystemTheme { get; set; } = new();

        /// <summary>
        /// Indicates whether the wiki is running in debug mode.
        /// </summary>
        public bool IsDebug { get; set; }

        /// <summary>
        /// Indicates whether new user self-registration is permitted.
        /// </summary>
        public bool AllowSignup { get; set; }

        /// <summary>
        /// Indicates whether LDAP authentication is enabled.
        /// </summary>
        public bool EnableLDAPAuthentication { get; set; }

        /// <summary>
        /// The full list of emojis available for use in wiki content.
        /// </summary>
        public List<TwEmoji> Emojis { get; set; } = new();

        /// <summary>
        /// The small brand image displayed in the navigation bar, as a base64 string or URL.
        /// </summary>
        public string BrandImageSmall { get; set; } = string.Empty;

        /// <summary>
        /// The display name of the wiki site.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// A short blurb displayed in the site footer.
        /// </summary>
        public string FooterBlurb { get; set; } = string.Empty;

        /// <summary>
        /// The copyright notice displayed in the site footer.
        /// </summary>
        public string Copyright { get; set; } = string.Empty;

        /// <summary>
        /// The list of navigation menu items displayed in the site header.
        /// </summary>
        public List<TwMenuItem> MenuItems { get; set; } = new();

        /// <summary>
        /// Custom HTML injected into the page header.
        /// </summary>
        public string HTMLHeader { get; set; } = string.Empty;

        /// <summary>
        /// Custom HTML injected into the page footer.
        /// </summary>
        public string HTMLFooter { get; set; } = string.Empty;

        /// <summary>
        /// Custom HTML injected immediately before the page body content.
        /// </summary>
        public string HTMLPreBody { get; set; } = string.Empty;

        /// <summary>
        /// Custom HTML injected immediately after the page body content.
        /// </summary>
        public string HTMLPostBody { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the wiki page description is included in HTML meta tags.
        /// </summary>
        public bool IncludeWikiDescriptionInMeta { get; set; }

        /// <summary>
        /// Indicates whether wiki page tags are included in HTML meta tags.
        /// </summary>
        public bool IncludeWikiTagsInMeta { get; set; }

        /// <summary>
        /// Indicates whether user comments are enabled on wiki pages.
        /// </summary>
        public bool EnablePageComments { get; set; }

        /// <summary>
        /// Indicates whether public user profile pages are enabled.
        /// </summary>
        public bool EnablePublicProfiles { get; set; }

        /// <summary>
        /// Indicates whether the navigation menu is fixed to the top of the viewport on scroll.
        /// </summary>
        public bool FixedMenuPosition { get; set; }

        /// <summary>
        /// Indicates whether the comment section is displayed in the page footer.
        /// </summary>
        public bool ShowCommentsOnPageFooter { get; set; }

        /// <summary>
        /// Indicates whether a search input is included in the navigation bar.
        /// </summary>
        public bool IncludeSearchOnNavbar { get; set; }

        /// <summary>
        /// The number of seconds rendered pages are cached before being re-processed.
        /// </summary>
        public int PageCacheSeconds { get; set; }

        /// <summary>
        /// The number of items displayed per page in paginated lists.
        /// </summary>
        public int PaginationSize { get; set; }

        /// <summary>
        /// The maximum amount of memory in megabytes allocated for the page cache.
        /// </summary>
        public int CacheMemoryLimitMB { get; set; }

        /// <summary>
        /// The number of recently modified pages shown on a user's public profile.
        /// </summary>
        public int DefaultProfileRecentlyModifiedCount { get; set; }

        /// <summary>
        /// Indicates whether animated emojis are preloaded into memory at startup.
        /// </summary>
        public bool PreLoadAnimatedEmojis { get; set; } = true;

        /// <summary>
        /// Indicates whether page compilation metrics such as render time and match counts are recorded.
        /// </summary>
        public bool RecordCompilationMetrics { get; set; }

        /// <summary>
        /// Indicates whether the last-modified date and user are shown in the page footer.
        /// </summary>
        public bool ShowLastModifiedOnPageFooter { get; set; }

        /// <summary>
        /// Indicates whether a change summary field is shown when a user edits a page.
        /// </summary>
        public bool ShowChangeSummaryWhenEditing { get; set; }

        /// <summary>
        /// Indicates whether a change summary is required before a page edit can be saved.
        /// </summary>
        public bool RequireChangeSummaryWhenEditing { get; set; }

        /// <summary>
        /// The default time zone applied to users who have not set a personal time zone preference.
        /// </summary>
        public string DefaultTimeZone { get; set; } = string.Empty;

        /// <summary>
        /// The base URL or address of the wiki, used for generating absolute links.
        /// </summary>
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// The default display height in pixels for emoji images.
        /// </summary>
        public int DefaultEmojiHeight { get; set; }

        /// <summary>
        /// The maximum allowed file size in bytes for user avatar uploads. Defaults to 1MB.
        /// </summary>
        public int MaxAvatarFileSize { get; set; } = 1048576;

        /// <summary>
        /// The maximum allowed file size in bytes for page file attachments. Defaults to 5MB.
        /// </summary>
        public int MaxAttachmentFileSize { get; set; } = 5242880;

        /// <summary>
        /// The maximum allowed file size in bytes for emoji image uploads. Defaults to 512KB.
        /// </summary>
        public int MaxEmojiFileSize { get; set; } = 524288;
    }
}