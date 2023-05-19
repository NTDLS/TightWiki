using System.Collections.Generic;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Repository;

namespace TightWiki.Shared
{
    public static class Global
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
        public static bool WritePageStatistics { get; set; }
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
            WritePageStatistics = performanceConfig.As<bool>("Write Page Statistics");
            CacheMemoryLimitMB = performanceConfig.As<int>("Cache Memory Limit MB");

            Library.Cache.Clear();

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var htmlConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("HTML Layout");
            var functConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Functionality");
            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");

            DefaultEmojiHeight = basicConfig.As<int>("Default Emoji Height");
            Address = basicConfig.As<string>("Address");
            AllowGoogleAuthentication = membershipConfig.As<bool>("Allow Google Authentication");
            DefaultTimeZone = basicConfig.As<string>("Default TimeZone");
            IncludeWikiDescriptionInMeta = functConfig.As<bool>("Include wiki Description in Meta");
            IncludeWikiTagsInMeta = functConfig.As<bool>("Include wiki Tags in Meta");
            EnablePageComments = functConfig.As<bool>("Enable Page Comments");
            ShowCommentsOnPageFooter = functConfig.As<bool>("Show Comments on Page Footer");
            IncludeSearchOnNavbar = functConfig.As<bool>("Include Search on Navbar");
            HTMLHeader = htmlConfig.As<string>("Header");
            HTMLFooter = htmlConfig.As<string>("Footer");
            HTMLPreBody = htmlConfig.As<string>("Pre-Body");
            HTMLPostBody = htmlConfig.As<string>("Post-Body");
            BrandImageSmall = basicConfig.As<string>("Brand Image (Small)");
            Name = basicConfig.As<string>("Name");
            FooterBlurb = basicConfig.As<string>("FooterBlurb");
            Copyright = basicConfig.As<string>("Copyright");
            MenuItems = MenuItemRepository.GetAllMenuItems();

            ReloadAllEmojis();
        }
    }
}
