using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using TightWiki.Library.Caching;
using TightWiki.Plugin;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository.Helpers
{
    public class WikiConfigurationManager
    {
        private readonly DatabaseManager _databaseManager;
        public TwConfiguration WikiConfiguration { get; private set; }

        public WikiConfigurationManager(IConfiguration configuration, DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
            WikiConfiguration = new TwConfiguration()
            {
                BasePath = configuration.GetValue<string?>("BasePath") ?? string.Empty
            };

            ReloadAll().Wait();
        }

        public async Task ReloadAll()
        {
            MemCache.Clear();

            WikiConfiguration.IsDebug = Debugger.IsAttached;

            var performanceConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Performance);
            WikiConfiguration.PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            WikiConfiguration.RecordCompilationMetrics = performanceConfig.Value<bool>("Record Compilation Metrics");
            WikiConfiguration.CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            MemCache.Initialize(WikiConfiguration.CacheMemoryLimitMB, TimeSpan.FromSeconds(WikiConfiguration.PageCacheSeconds));

            var basicConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Basic);
            var customizationConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Customization);
            var htmlConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.HTMLLayout);
            var functionalityConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Functionality);
            var membershipConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Membership);
            var searchConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Search);
            var filesAndAttachmentsConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.FilesAndAttachments);
            var ldapAuthentication = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.LDAPAuthentication);
            WikiConfiguration.EnableLDAPAuthentication = ldapAuthentication.Value("LDAP : Enable LDAP Authentication", false);

            WikiConfiguration.Address = basicConfig?.Value<string>("Address") ?? string.Empty;
            WikiConfiguration.Name = basicConfig?.Value<string>("Name") ?? string.Empty;
            WikiConfiguration.Copyright = basicConfig?.Value<string>("Copyright") ?? string.Empty;

            var themeName = customizationConfig.Value("Theme", "Light");

            WikiConfiguration.FixedMenuPosition = customizationConfig.Value("Fixed Header Menu Position", false);
            WikiConfiguration.AllowSignup = membershipConfig.Value("Allow Signup", false);
            WikiConfiguration.DefaultProfileRecentlyModifiedCount = performanceConfig.Value<int>("Default Profile Recently Modified Count");
            WikiConfiguration.PreLoadAnimatedEmojis = performanceConfig.Value<bool>("Pre-Load Animated Emojis");
            WikiConfiguration.SystemTheme = (await _databaseManager.ConfigurationRepository.GetAllThemes()).Single(o => o.Name == themeName);
            WikiConfiguration.DefaultEmojiHeight = customizationConfig.Value<int>("Default Emoji Height");
            WikiConfiguration.PaginationSize = customizationConfig.Value<int>("Pagination Size");

            WikiConfiguration.DefaultTimeZone = customizationConfig?.Value<string>("Default TimeZone") ?? string.Empty;
            WikiConfiguration.IncludeWikiDescriptionInMeta = functionalityConfig.Value<bool>("Include wiki Description in Meta");
            WikiConfiguration.IncludeWikiTagsInMeta = functionalityConfig.Value<bool>("Include wiki Tags in Meta");
            WikiConfiguration.EnablePageComments = functionalityConfig.Value<bool>("Enable Page Comments");
            WikiConfiguration.EnablePublicProfiles = functionalityConfig.Value<bool>("Enable Public Profiles");
            WikiConfiguration.ShowCommentsOnPageFooter = functionalityConfig.Value<bool>("Show Comments on Page Footer");
            WikiConfiguration.ShowChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Show Change Summary when Editing");
            WikiConfiguration.RequireChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Require Change Summary when Editing");
            WikiConfiguration.ShowLastModifiedOnPageFooter = functionalityConfig.Value<bool>("Show Last Modified on Page Footer");
            WikiConfiguration.IncludeSearchOnNavbar = searchConfig.Value<bool>("Include Search on Navbar");
            WikiConfiguration.HTMLHeader = htmlConfig?.Value<string>("Header") ?? string.Empty;
            WikiConfiguration.HTMLFooter = htmlConfig?.Value<string>("Footer") ?? string.Empty;
            WikiConfiguration.HTMLPreBody = htmlConfig?.Value<string>("Pre-Body") ?? string.Empty;
            WikiConfiguration.HTMLPostBody = htmlConfig?.Value<string>("Post-Body") ?? string.Empty;
            WikiConfiguration.BrandImageSmall = customizationConfig?.Value<string>("Brand Image (Small)") ?? string.Empty;
            WikiConfiguration.FooterBlurb = customizationConfig?.Value<string>("FooterBlurb") ?? string.Empty;
            WikiConfiguration.MaxAvatarFileSize = filesAndAttachmentsConfig.Value<int>("Max Avatar File Size");
            WikiConfiguration.MaxAttachmentFileSize = filesAndAttachmentsConfig.Value<int>("Max Attachment File Size");
            WikiConfiguration.MaxEmojiFileSize = filesAndAttachmentsConfig.Value<int>("Max Emoji File Size");

            await ReloadMenu();
            await ReloadEmojis();
        }

        public async Task ReloadMenu()
        {
            WikiConfiguration.MenuItems = await _databaseManager.ConfigurationRepository.GetAllMenuItems();
        }

        public async Task ReloadEmojis()
        {
            WikiConfiguration.Emojis = await _databaseManager.EmojiRepository.ReloadEmojis(
                WikiConfiguration.PreLoadAnimatedEmojis, WikiConfiguration.DefaultEmojiHeight);
        }
    }
}
