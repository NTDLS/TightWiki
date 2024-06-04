using System.Diagnostics;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace TightWiki
{
    public static class GlobalSettings
    {
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
        public static bool ShowCommentsOnPageFooter { get; set; }
        public static bool IncludeSearchOnNavbar { get; set; }
        public static int PageCacheSeconds { get; set; }
        public static int CacheMemoryLimitMB { get; set; }
        public static int DefaultProfileRecentlyModifiedCount { get; set; }
        public static bool WritePageStatistics { get; set; }
        public static bool ShowLastModifiedOnPageFooter { get; set; }
        public static string DefaultTimeZone { get; set; } = string.Empty;
        public static string Address { get; set; } = string.Empty;
        public static int DefaultEmojiHeight { get; set; }
        public static bool AllowGoogleAuthentication { get; set; }

        public static void ReloadEmojis()
        {
            WikiCache.ClearCategory(WikiCache.Category.Emoji);
            Emojis = EmojiRepository.GetAllEmojis();
        }

        public static void ReloadEverything()
        {
            WikiCache.Clear();

            IsDebug = Debugger.IsAttached;

            var performanceConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Performance");
            PageCacheSeconds = performanceConfig.As<int>("Page Cache Time (Seconds)");
            WritePageStatistics = performanceConfig.As<bool>("Write Page Statistics");
            CacheMemoryLimitMB = performanceConfig.As<int>("Cache Memory Limit MB");

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Customization");
            var htmlConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("HTML Layout");
            var functionalityConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Functionality");
            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            Address = basicConfig?.As<string>("Address") ?? string.Empty;
            Name = basicConfig?.As<string>("Name") ?? string.Empty;
            Copyright = basicConfig?.As<string>("Copyright") ?? string.Empty;

            AllowSignup = membershipConfig.As<bool>("Allow Signup", false);
            DefaultProfileRecentlyModifiedCount = performanceConfig.As<int>("Default Profile Recently Modified Count");
            DefaultEmojiHeight = customizationConfig.As<int>("Default Emoji Height");
            AllowGoogleAuthentication = membershipConfig.As<bool>("Allow Google Authentication");
            DefaultTimeZone = customizationConfig?.As<string>("Default TimeZone") ?? string.Empty;
            IncludeWikiDescriptionInMeta = functionalityConfig.As<bool>("Include wiki Description in Meta");
            IncludeWikiTagsInMeta = functionalityConfig.As<bool>("Include wiki Tags in Meta");
            EnablePageComments = functionalityConfig.As<bool>("Enable Page Comments");
            ShowCommentsOnPageFooter = functionalityConfig.As<bool>("Show Comments on Page Footer");
            ShowLastModifiedOnPageFooter = functionalityConfig.As<bool>("Show Last Modified on Page Footer");
            IncludeSearchOnNavbar = searchConfig.As<bool>("Include Search on Navbar");
            HTMLHeader = htmlConfig?.As<string>("Header") ?? string.Empty;
            HTMLFooter = htmlConfig?.As<string>("Footer") ?? string.Empty;
            HTMLPreBody = htmlConfig?.As<string>("Pre-Body") ?? string.Empty;
            HTMLPostBody = htmlConfig?.As<string>("Post-Body") ?? string.Empty;
            BrandImageSmall = customizationConfig?.As<string>("Brand Image (Small)") ?? string.Empty;
            FooterBlurb = customizationConfig?.As<string>("FooterBlurb") ?? string.Empty;
            MenuItems = ConfigurationRepository.GetAllMenuItems();

            ReloadEmojis();
        }
    }
}
