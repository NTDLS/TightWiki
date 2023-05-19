using System.Collections.Generic;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;

namespace TightWiki.Shared
{
    public static class GlobalSettings
    {
        public static List<Emoji> Emojis { get; set; } = new();
        public static string BrandImageSmall { get; set; }
        public static string Name { get; set; }
        public static string FooterBlurb { get; set; }
        public static string Copyright { get; set; }
        public static List<MenuItem> MenuItems { get; set; }
        public static string HTMLHeader { get; set; }
        public static string HTMLFooter { get; set; }
        public static string HTMLPreBody { get; set; }
        public static string HTMLPostBody { get; set; }
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
        public static string DefaultTimeZone { get; set; }
        public static string Address { get; set; }
        public static int DefaultEmojiHeight { get; set; }
        public static bool AllowGoogleAuthentication { get; set; }

        public static void ReloadAllEmojis()
        {
            Emojis = EmojiRepository.GetAllEmojis();
        }

        public static void PreloadSingletons()
        {
            var performanceConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Performance", false);
            PageCacheSeconds = performanceConfig.As<int>("Page Cache Time (Seconds)");
            WritePageStatistics = performanceConfig.As<bool>("Performance: Write Page Statistics");
            CacheMemoryLimitMB = performanceConfig.As<int>("Cache Memory Limit MB");

            Library.Cache.Clear();

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Customization");
            var htmlConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("HTML Layout");
            var functConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Functionality");
            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            Address = basicConfig.As<string>("Address");
            Name = basicConfig.As<string>("Name");
            Copyright = basicConfig.As<string>("Copyright");

            DefaultProfileRecentlyModifiedCount = performanceConfig.As<int>("Profile: Default Profile Recently Modified Count");
            DefaultEmojiHeight = customizationConfig.As<int>("Default Emoji Height");
            AllowGoogleAuthentication = membershipConfig.As<bool>("Allow Google Authentication");
            DefaultTimeZone = customizationConfig.As<string>("Default TimeZone");
            IncludeWikiDescriptionInMeta = functConfig.As<bool>("Include wiki Description in Meta");
            IncludeWikiTagsInMeta = functConfig.As<bool>("Include wiki Tags in Meta");
            EnablePageComments = functConfig.As<bool>("Enable Page Comments");
            ShowCommentsOnPageFooter = functConfig.As<bool>("Show Comments on Page Footer");
            ShowLastModifiedOnPageFooter = functConfig.As<bool>("Show Last Modified on Page Footer");
            IncludeSearchOnNavbar = searchConfig.As<bool>("Include Search on Navbar");
            HTMLHeader = htmlConfig.As<string>("Header");
            HTMLFooter = htmlConfig.As<string>("Footer");
            HTMLPreBody = htmlConfig.As<string>("Pre-Body");
            HTMLPostBody = htmlConfig.As<string>("Post-Body");
            BrandImageSmall = customizationConfig.As<string>("Brand Image (Small)");
            FooterBlurb = customizationConfig.As<string>("FooterBlurb");
            MenuItems = MenuItemRepository.GetAllMenuItems();

            ReloadAllEmojis();
        }
    }
}
