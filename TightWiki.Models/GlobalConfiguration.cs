﻿using TightWiki.Library;
using TightWiki.Models.DataModels;

namespace TightWiki.Configuration
{
    public static class GlobalConfiguration
    {
        public static Theme SystemTheme { get; set; } = new();
        public static bool IsDebug { get; set; }
        public static bool AllowSignup { get; set; }
        public static List<Emoji> Emojis { get; set; } = new();
        public static string BrandImageSmall { get; set; } = string.Empty;
        public static string Name { get; set; } = string.Empty;
        public static string FooterBlurb { get; set; } = string.Empty;
        public static string Copyright { get; set; } = string.Empty;
        public static List<MenuItem> MenuItems { get; set; } = new();
        public static string HTMLHeader { get; set; } = string.Empty;
        public static string HTMLFooter { get; set; } = string.Empty;
        public static string HTMLPreBody { get; set; } = string.Empty;
        public static string HTMLPostBody { get; set; } = string.Empty;
        public static bool IncludeWikiDescriptionInMeta { get; set; }
        public static bool IncludeWikiTagsInMeta { get; set; }
        public static bool EnablePageComments { get; set; }
        public static bool FixedMenuPosition { get; set; }
        public static bool ShowCommentsOnPageFooter { get; set; }
        public static bool IncludeSearchOnNavbar { get; set; }
        public static int PageCacheSeconds { get; set; }
        public static int CacheMemoryLimitMB { get; set; }
        public static int DefaultProfileRecentlyModifiedCount { get; set; }
        public static bool RecordCompilationMetrics { get; set; }
        public static bool ShowLastModifiedOnPageFooter { get; set; }
        public static string DefaultTimeZone { get; set; } = string.Empty;
        public static string Address { get; set; } = string.Empty;
        public static int DefaultEmojiHeight { get; set; }
        public static bool AllowGoogleAuthentication { get; set; }
    }
}
