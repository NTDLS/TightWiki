using System.Diagnostics;
using TightWiki.Caching;
using TightWiki.Configuration;

namespace TightWiki.Repository
{
    public class GlobalConfiguration
    {
        public static void ReloadEmojis()
        {
            WikiCache.ClearCategory(WikiCache.Category.Emoji);
            GlobalSettings.Emojis = EmojiRepository.GetAllEmojis();
        }

        public static void ReloadEverything()
        {
            WikiCache.Clear();

            GlobalSettings.IsDebug = Debugger.IsAttached;

            var performanceConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Performance", false);
            GlobalSettings.PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            GlobalSettings.RecordCompilationMetrics = performanceConfig.Value<bool>("Record Compilation Metrics");
            GlobalSettings.CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            WikiCache.Initialize(GlobalSettings.CacheMemoryLimitMB);

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Customization");
            var htmlConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("HTML Layout");
            var functionalityConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Functionality");
            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
            var searchConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Search");

            GlobalSettings.Address = basicConfig?.Value<string>("Address") ?? string.Empty;
            GlobalSettings.Name = basicConfig?.Value<string>("Name") ?? string.Empty;
            GlobalSettings.Copyright = basicConfig?.Value<string>("Copyright") ?? string.Empty;

            var themeName = customizationConfig.Value("Theme", "Light");

            GlobalSettings.FixedMenuPosition = customizationConfig.Value("Fixed Header Menu Position", false);
            GlobalSettings.AllowSignup = membershipConfig.Value("Allow Signup", false);
            GlobalSettings.DefaultProfileRecentlyModifiedCount = performanceConfig.Value<int>("Default Profile Recently Modified Count");
            GlobalSettings.SystemTheme = ConfigurationRepository.GetAllThemes().Single(o => o.Name == themeName);
            GlobalSettings.DefaultEmojiHeight = customizationConfig.Value<int>("Default Emoji Height");
            GlobalSettings.AllowGoogleAuthentication = membershipConfig.Value<bool>("Allow Google Authentication");
            GlobalSettings.DefaultTimeZone = customizationConfig?.Value<string>("Default TimeZone") ?? string.Empty;
            GlobalSettings.IncludeWikiDescriptionInMeta = functionalityConfig.Value<bool>("Include wiki Description in Meta");
            GlobalSettings.IncludeWikiTagsInMeta = functionalityConfig.Value<bool>("Include wiki Tags in Meta");
            GlobalSettings.EnablePageComments = functionalityConfig.Value<bool>("Enable Page Comments");
            GlobalSettings.ShowCommentsOnPageFooter = functionalityConfig.Value<bool>("Show Comments on Page Footer");
            GlobalSettings.ShowLastModifiedOnPageFooter = functionalityConfig.Value<bool>("Show Last Modified on Page Footer");
            GlobalSettings.IncludeSearchOnNavbar = searchConfig.Value<bool>("Include Search on Navbar");
            GlobalSettings.HTMLHeader = htmlConfig?.Value<string>("Header") ?? string.Empty;
            GlobalSettings.HTMLFooter = htmlConfig?.Value<string>("Footer") ?? string.Empty;
            GlobalSettings.HTMLPreBody = htmlConfig?.Value<string>("Pre-Body") ?? string.Empty;
            GlobalSettings.HTMLPostBody = htmlConfig?.Value<string>("Post-Body") ?? string.Empty;
            GlobalSettings.BrandImageSmall = customizationConfig?.Value<string>("Brand Image (Small)") ?? string.Empty;
            GlobalSettings.FooterBlurb = customizationConfig?.Value<string>("FooterBlurb") ?? string.Empty;
            GlobalSettings.MenuItems = ConfigurationRepository.GetAllMenuItems();

            ReloadEmojis();
        }

    }
}
