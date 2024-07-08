using System.Diagnostics;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;

namespace TightWiki
{
    public static class GlobalSettings
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
            PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            WritePageStatistics = performanceConfig.Value<bool>("Write Page Statistics");
            CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Customization");
            var htmlConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("HTML Layout");
            var functionalityConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Functionality");
            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            Address = basicConfig?.Value<string>("Address") ?? string.Empty;
            Name = basicConfig?.Value<string>("Name") ?? string.Empty;
            Copyright = basicConfig?.Value<string>("Copyright") ?? string.Empty;

            var themeName = customizationConfig.Value("Theme", "Light");

            FixedMenuPosition = customizationConfig.Value("Fixed Header Menu Position", false);
            AllowSignup = membershipConfig.Value("Allow Signup", false);
            DefaultProfileRecentlyModifiedCount = performanceConfig.Value<int>("Default Profile Recently Modified Count");
            SystemTheme = ConfigurationRepository.GetAllThemes().Single(o => o.Name == themeName);
            DefaultEmojiHeight = customizationConfig.Value<int>("Default Emoji Height");
            AllowGoogleAuthentication = membershipConfig.Value<bool>("Allow Google Authentication");
            DefaultTimeZone = customizationConfig?.Value<string>("Default TimeZone") ?? string.Empty;
            IncludeWikiDescriptionInMeta = functionalityConfig.Value<bool>("Include wiki Description in Meta");
            IncludeWikiTagsInMeta = functionalityConfig.Value<bool>("Include wiki Tags in Meta");
            EnablePageComments = functionalityConfig.Value<bool>("Enable Page Comments");
            ShowCommentsOnPageFooter = functionalityConfig.Value<bool>("Show Comments on Page Footer");
            ShowLastModifiedOnPageFooter = functionalityConfig.Value<bool>("Show Last Modified on Page Footer");
            IncludeSearchOnNavbar = searchConfig.Value<bool>("Include Search on Navbar");
            HTMLHeader = htmlConfig?.Value<string>("Header") ?? string.Empty;
            HTMLFooter = htmlConfig?.Value<string>("Footer") ?? string.Empty;
            HTMLPreBody = htmlConfig?.Value<string>("Pre-Body") ?? string.Empty;
            HTMLPostBody = htmlConfig?.Value<string>("Post-Body") ?? string.Empty;
            BrandImageSmall = customizationConfig?.Value<string>("Brand Image (Small)") ?? string.Empty;
            FooterBlurb = customizationConfig?.Value<string>("FooterBlurb") ?? string.Empty;
            MenuItems = ConfigurationRepository.GetAllMenuItems();

            ReloadEmojis();
        }
    }
}
