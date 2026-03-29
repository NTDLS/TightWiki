using System.Diagnostics;
using TightWiki.Caching;
using TightWiki.Models;
using TightWiki.Repository;
using static TightWiki.Library.Constants;

namespace TightWiki
{
    public static class WikiConfigurationManager
    {
        public static async Task<TightWikiConfiguration> Create(IConfiguration configuration)
        {
            var instance = new TightWikiConfiguration();

            instance.BasePath = configuration.GetValue<string?>("BasePath") ?? string.Empty;

            await ReloadAll(instance);
            return instance;
        }

        public static async Task ReloadAll(TightWikiConfiguration instance)
        {
            WikiCache.Clear();

            instance.IsDebug = Debugger.IsAttached;

            var performanceConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Performance);
            instance.PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            instance.RecordCompilationMetrics = performanceConfig.Value<bool>("Record Compilation Metrics");
            instance.CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            WikiCache.Initialize(instance.CacheMemoryLimitMB, TimeSpan.FromSeconds(instance.PageCacheSeconds));

            var basicConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Basic);
            var customizationConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Customization);
            var htmlConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.HTMLLayout);
            var functionalityConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Functionality);
            var membershipConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);
            var searchConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Search);
            var filesAndAttachmentsConfig = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.FilesAndAttachments);
            var ldapAuthentication = await ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.LDAPAuthentication);
            instance.EnableLDAPAuthentication = ldapAuthentication.Value("LDAP : Enable LDAP Authentication", false);

            instance.Address = basicConfig?.Value<string>("Address") ?? string.Empty;
            instance.Name = basicConfig?.Value<string>("Name") ?? string.Empty;
            instance.Copyright = basicConfig?.Value<string>("Copyright") ?? string.Empty;

            var themeName = customizationConfig.Value("Theme", "Light");

            instance.FixedMenuPosition = customizationConfig.Value("Fixed Header Menu Position", false);
            instance.AllowSignup = membershipConfig.Value("Allow Signup", false);
            instance.DefaultProfileRecentlyModifiedCount = performanceConfig.Value<int>("Default Profile Recently Modified Count");
            instance.PreLoadAnimatedEmojis = performanceConfig.Value<bool>("Pre-Load Animated Emojis");
            instance.SystemTheme = (await ConfigurationRepository.GetAllThemes()).Single(o => o.Name == themeName);
            instance.DefaultEmojiHeight = customizationConfig.Value<int>("Default Emoji Height");
            instance.PaginationSize = customizationConfig.Value<int>("Pagination Size");

            instance.DefaultTimeZone = customizationConfig?.Value<string>("Default TimeZone") ?? string.Empty;
            instance.IncludeWikiDescriptionInMeta = functionalityConfig.Value<bool>("Include wiki Description in Meta");
            instance.IncludeWikiTagsInMeta = functionalityConfig.Value<bool>("Include wiki Tags in Meta");
            instance.EnablePageComments = functionalityConfig.Value<bool>("Enable Page Comments");
            instance.EnablePublicProfiles = functionalityConfig.Value<bool>("Enable Public Profiles");
            instance.ShowCommentsOnPageFooter = functionalityConfig.Value<bool>("Show Comments on Page Footer");
            instance.ShowChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Show Change Summary when Editing");
            instance.RequireChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Require Change Summary when Editing");
            instance.ShowLastModifiedOnPageFooter = functionalityConfig.Value<bool>("Show Last Modified on Page Footer");
            instance.IncludeSearchOnNavbar = searchConfig.Value<bool>("Include Search on Navbar");
            instance.HTMLHeader = htmlConfig?.Value<string>("Header") ?? string.Empty;
            instance.HTMLFooter = htmlConfig?.Value<string>("Footer") ?? string.Empty;
            instance.HTMLPreBody = htmlConfig?.Value<string>("Pre-Body") ?? string.Empty;
            instance.HTMLPostBody = htmlConfig?.Value<string>("Post-Body") ?? string.Empty;
            instance.BrandImageSmall = customizationConfig?.Value<string>("Brand Image (Small)") ?? string.Empty;
            instance.FooterBlurb = customizationConfig?.Value<string>("FooterBlurb") ?? string.Empty;
            instance.MaxAvatarFileSize = filesAndAttachmentsConfig.Value<int>("Max Avatar File Size");
            instance.MaxAttachmentFileSize = filesAndAttachmentsConfig.Value<int>("Max Attachment File Size");
            instance.MaxEmojiFileSize = filesAndAttachmentsConfig.Value<int>("Max Emoji File Size");

            await ReloadMenu(instance);
            await ReloadEmojis(instance);
        }

        public static async Task ReloadMenu(TightWikiConfiguration instance)
        {
            instance.MenuItems = await ConfigurationRepository.GetAllMenuItems();
        }

        public static async Task ReloadEmojis(TightWikiConfiguration instance)
        {
            instance.Emojis = await ConfigurationRepository.ReloadEmojis(instance.PreLoadAnimatedEmojis, instance.DefaultEmojiHeight);
        }
    }
}
