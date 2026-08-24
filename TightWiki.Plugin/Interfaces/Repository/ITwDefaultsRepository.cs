using TightWiki.Plugin.Models;
using TightWiki.Plugin.Models.Defaults;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    ///  Data access for default values that are used when creating a new wiki, such as default configuration entries, default themes, etc.
    /// </summary>
    public interface ITwDefaultsRepository
    {
        /// <summary>
        /// Returns the distinct set of default configuration groups (name and description) used to seed the database.
        /// </summary>
        Task<List<TwDefaultConfiguration>> GetDefaultConfigurationGroups();

        /// <summary>
        /// Returns all default configuration entries used to seed the database.
        /// </summary>
        Task<List<TwDefaultConfiguration>> GetDefaultConfigurations();

        /// <summary>
        /// Returns all default themes used to seed the database.
        /// </summary>
        Task<List<TwDefaultTheme>> GetDefaultThemes();

        /// <summary>
        /// Returns all default wiki pages belonging to the specified namespace, used to seed the database.
        /// </summary>
        Task<List<TwDefaultWikiPage>> GetDefaultWikiPages(string namespaceName);

        /// <summary>
        /// Returns all default feature templates used to seed the database.
        /// </summary>
        Task<List<TwDefaultFeatureTemplate>> GetDefaultFeatureTemplates();

        /// <summary>
        /// Returns all default page file attachments (e.g. images embedded in a default page's body) belonging to
        /// the specified namespace, used to seed the database. As with <see cref="GetDefaultEmojis"/>, the SQLite
        /// provider returns an empty collection - it gets these "for free" via a full copy of Data\pages.db.
        /// </summary>
        Task<List<TwDefaultPageFileAttachment>> GetDefaultPageFileAttachments(string namespaceName);

        /// <summary>
        /// Returns all default page compilation/view statistics (Statistics.PageStatistics) used to seed the
        /// database, one entry per reference page that actually has a statistics row - not every default wiki
        /// page has one. As with <see cref="GetDefaultPageFileAttachments"/>, the SQLite provider returns an
        /// empty collection - it gets these "for free" via a full copy of Data\statistics.db.
        /// </summary>
        Task<List<TwDefaultPageStatistic>> GetDefaultPageStatistics();

        /// <summary>
        /// Returns all default emoji (metadata only - no image bytes; see <see cref="TwDefaultEmoji.ImageEntry"/>)
        /// used to seed the database. The SQLite provider does not seed emoji through this mechanism (it gets them
        /// "for free" via a full copy of Data\emoji.db) and therefore returns an empty collection; this method
        /// exists for provider-neutral seeding (see Database-Providers-Plan.md chapter 4.6).
        /// </summary>
        Task<List<TwDefaultEmoji>> GetDefaultEmojis();

        /// <summary>
        /// Returns all default emoji-to-category associations used to seed the database. As with
        /// <see cref="GetDefaultEmojis"/>, the SQLite provider returns an empty collection since it does not seed
        /// emoji through this mechanism.
        /// </summary>
        Task<List<TwDefaultEmojiCategory>> GetDefaultEmojiCategories();

        /// <summary>
        /// Returns all default navigation menu items used to seed the database. As with
        /// <see cref="GetDefaultEmojis"/>, the SQLite provider returns an empty collection - it gets its
        /// Config.MenuItem rows "for free" via a full copy of Data\config.db (see Database-Providers-Plan.md
        /// chapter 4.6) and never populates this via ITwDefaultsRepository.
        /// </summary>
        Task<List<TwMenuItem>> GetDefaultMenuItems();

        /// <summary>
        /// Returns the display-only fields of the built-in admin's Users.Profile row used to seed the database
        /// (see <see cref="TwDefaultProfile"/> for exactly which fields and why), or null if the reference data
        /// carries no such row. As with <see cref="GetDefaultEmojis"/>, the SQLite provider returns null - it gets
        /// its admin Users.Profile row "for free" via a full copy of Data\users.db and never populates this via
        /// ITwDefaultsRepository.
        /// </summary>
        Task<TwDefaultProfile?> GetDefaultAdminProfile();
    }
}
