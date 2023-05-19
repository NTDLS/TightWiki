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
            PageCacheSeconds = performanceConfig.As<int>("Cache: Page Cache Time (Seconds)");
            WritePageStatistics = performanceConfig.As<bool>("Performance: Write Page Statistics");
            CacheMemoryLimitMB = performanceConfig.As<int>("Cache: Cache Memory Limit MB");

            Library.Cache.Clear();

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Customization");
            var htmlConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("HTML Layout");
            var functConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Functionality");
            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            Address = basicConfig.As<string>("Site: Address");
            Name = basicConfig.As<string>("Site: Name");
            Copyright = basicConfig.As<string>("Site: Copyright");

            DefaultEmojiHeight = customizationConfig.As<int>("Images: Default Emoji Height");
            AllowGoogleAuthentication = membershipConfig.As<bool>("Authorization: Allow Google Authentication");
            DefaultTimeZone = customizationConfig.As<string>("Localization: Default TimeZone");
            IncludeWikiDescriptionInMeta = functConfig.As<bool>("Meta: Include wiki Description in Meta");
            IncludeWikiTagsInMeta = functConfig.As<bool>("Meta: Include wiki Tags in Meta");
            EnablePageComments = functConfig.As<bool>("Comments: Enable Page Comments");
            ShowCommentsOnPageFooter = functConfig.As<bool>("Comments: Show Comments on Page Footer");
            IncludeSearchOnNavbar = searchConfig.As<bool>("Include Search on Navbar");
            HTMLHeader = htmlConfig.As<string>("Format: Header");
            HTMLFooter = htmlConfig.As<string>("Format: Footer");
            HTMLPreBody = htmlConfig.As<string>("Format: Pre-Body");
            HTMLPostBody = htmlConfig.As<string>("Format: Post-Body");
            BrandImageSmall = customizationConfig.As<string>("Site: Brand Image (Small)");
            FooterBlurb = customizationConfig.As<string>("Site: FooterBlurb");
            MenuItems = MenuItemRepository.GetAllMenuItems();

            ReloadAllEmojis();
        }
    }
}
