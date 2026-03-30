using Microsoft.Extensions.Configuration;
using System.Diagnostics;
using TightWiki.Plugin;
using TightWiki.Plugin.Caching;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository.Helpers
{
    public class TwConfigurationManager
    {
        private readonly DatabaseManager _databaseManager;
        public TwConfiguration Configuration { get; private set; }

        public TwConfigurationManager(IConfiguration configuration, DatabaseManager databaseManager)
        {
            _databaseManager = databaseManager;
            Configuration = new TwConfiguration()
            {
                BasePath = configuration.GetValue<string?>("BasePath") ?? string.Empty
            };

            ReloadAll().Wait();
        }

        public async Task ReloadAll()
        {
            TwCache.Clear();

            Configuration.IsDebug = Debugger.IsAttached;

            var performanceConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Performance);
            Configuration.PageCacheSeconds = performanceConfig.Value<int>("Page Cache Time (Seconds)");
            Configuration.RecordCompilationMetrics = performanceConfig.Value<bool>("Record Compilation Metrics");
            Configuration.CacheMemoryLimitMB = performanceConfig.Value<int>("Cache Memory Limit MB");

            TwCache.Initialize(Configuration.CacheMemoryLimitMB, TimeSpan.FromSeconds(Configuration.PageCacheSeconds));

            var basicConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Basic);
            var customizationConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Customization);
            var htmlConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.HTMLLayout);
            var functionalityConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Functionality);
            var membershipConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);
            var searchConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Search);
            var filesAndAttachmentsConfig = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.FilesAndAttachments);
            var ldapAuthentication = await _databaseManager.ConfigurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.LDAPAuthentication);
            Configuration.EnableLDAPAuthentication = ldapAuthentication.Value("LDAP : Enable LDAP Authentication", false);

            Configuration.Address = basicConfig?.Value<string>("Address") ?? string.Empty;
            Configuration.Name = basicConfig?.Value<string>("Name") ?? string.Empty;
            Configuration.Copyright = basicConfig?.Value<string>("Copyright") ?? string.Empty;

            var themeName = customizationConfig.Value("Theme", "Light");

            Configuration.FixedMenuPosition = customizationConfig.Value("Fixed Header Menu Position", false);
            Configuration.AllowSignup = membershipConfig.Value("Allow Signup", false);
            Configuration.DefaultProfileRecentlyModifiedCount = performanceConfig.Value<int>("Default Profile Recently Modified Count");
            Configuration.PreLoadAnimatedEmojis = performanceConfig.Value<bool>("Pre-Load Animated Emojis");
            Configuration.SystemTheme = (await _databaseManager.ConfigurationRepository.GetAllThemes()).Single(o => o.Name == themeName);
            Configuration.DefaultEmojiHeight = customizationConfig.Value<int>("Default Emoji Height");
            Configuration.PaginationSize = customizationConfig.Value<int>("Pagination Size");

            Configuration.DefaultTimeZone = customizationConfig?.Value<string>("Default TimeZone") ?? string.Empty;
            Configuration.IncludeWikiDescriptionInMeta = functionalityConfig.Value<bool>("Include wiki Description in Meta");
            Configuration.IncludeWikiTagsInMeta = functionalityConfig.Value<bool>("Include wiki Tags in Meta");
            Configuration.EnablePageComments = functionalityConfig.Value<bool>("Enable Page Comments");
            Configuration.EnablePublicProfiles = functionalityConfig.Value<bool>("Enable Public Profiles");
            Configuration.ShowCommentsOnPageFooter = functionalityConfig.Value<bool>("Show Comments on Page Footer");
            Configuration.ShowChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Show Change Summary when Editing");
            Configuration.RequireChangeSummaryWhenEditing = functionalityConfig.Value<bool>("Require Change Summary when Editing");
            Configuration.ShowLastModifiedOnPageFooter = functionalityConfig.Value<bool>("Show Last Modified on Page Footer");
            Configuration.IncludeSearchOnNavbar = searchConfig.Value<bool>("Include Search on Navbar");
            Configuration.HTMLHeader = htmlConfig?.Value<string>("Header") ?? string.Empty;
            Configuration.HTMLFooter = htmlConfig?.Value<string>("Footer") ?? string.Empty;
            Configuration.HTMLPreBody = htmlConfig?.Value<string>("Pre-Body") ?? string.Empty;
            Configuration.HTMLPostBody = htmlConfig?.Value<string>("Post-Body") ?? string.Empty;
            Configuration.BrandImageSmall = customizationConfig?.Value<string>("Brand Image (Small)") ?? string.Empty;
            Configuration.FooterBlurb = customizationConfig?.Value<string>("FooterBlurb") ?? string.Empty;
            Configuration.MaxAvatarFileSize = filesAndAttachmentsConfig.Value<int>("Max Avatar File Size");
            Configuration.MaxAttachmentFileSize = filesAndAttachmentsConfig.Value<int>("Max Attachment File Size");
            Configuration.MaxEmojiFileSize = filesAndAttachmentsConfig.Value<int>("Max Emoji File Size");

            await ReloadMenu();
            await ReloadEmojis();
        }

        public async Task ReloadMenu()
        {
           Configuration.MenuItems = await _databaseManager.ConfigurationRepository.GetAllMenuItems();
        }

        public async Task ReloadEmojis()
        {
            Configuration.Emojis = await _databaseManager.EmojiRepository.ReloadEmojis(
                Configuration.PreLoadAnimatedEmojis, Configuration.DefaultEmojiHeight);
        }
    }
}
