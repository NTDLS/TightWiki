using TightWiki.Data.EfCore.Entities.Statistics;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Entities.Pages
{
    /// <summary>
    /// A wiki page (Pages.Page). <see cref="Name"/> is already the fully-qualified name (including the
    /// "Namespace :: Title" prefix where applicable) - <see cref="Namespace"/> is a redundant, persisted copy
    /// of the prefix used for filtering.
    /// </summary>
    public class Page
    {
        /// <summary>
        /// The unique identifier for this page.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The fully-qualified, unique, case-insensitive name of this page (includes the namespace prefix).
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The namespace prefix of <see cref="Name"/>, persisted separately for filtering. Empty string for
        /// pages with no namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The unique, case-insensitive, URL-safe navigation path used to locate this page.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// A short description of the page content.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The current revision number of this page.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// The identifier of the user who originally created this page. Value-equal to (but not a formal foreign
        /// key against) <see cref="Users.Profile.UserId"/> - see <see cref="CreatedByUser"/>.
        /// </summary>
        public Guid CreatedByUserId { get; set; }

        /// <summary>
        /// The date and time this page was originally created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The identifier of the user who last modified this page. Value-equal to (but not a formal foreign key
        /// against) <see cref="Users.Profile.UserId"/> - see <see cref="ModifiedByUser"/>.
        /// </summary>
        public Guid ModifiedByUserId { get; set; }

        /// <summary>
        /// The date and time this page was last modified.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The profile of the user who originally created this page (cross-schema navigation to Users.Profile,
        /// via <see cref="CreatedByUserId"/>). Optional - the raw SQL this navigation mirrors (e.g.
        /// GetAllPagesPaged.sql) always LEFT OUTER JOINs Profile, and application code never enforces that a
        /// matching profile exists.
        /// </summary>
        public Profile? CreatedByUser { get; set; }

        /// <summary>
        /// The profile of the user who last modified this page (cross-schema navigation to Users.Profile, via
        /// <see cref="ModifiedByUserId"/>). Optional - see <see cref="CreatedByUser"/>.
        /// </summary>
        public Profile? ModifiedByUser { get; set; }

        /// <summary>
        /// The compilation/view statistics for this page (cross-schema navigation to Statistics.PageStatistics).
        /// Optional - a page has no row here until it is first compiled (see
        /// StatisticsRepository.MergePageCompilationStatistics).
        /// </summary>
        public PageStatistic? PageStatistic { get; set; }

        /// <summary>
        /// The feature templates (help/markup examples) associated with this page.
        /// </summary>
        public ICollection<FeatureTemplate> FeatureTemplates { get; set; } = [];

        /// <summary>
        /// The comments posted on this page.
        /// </summary>
        public ICollection<PageComment> PageComments { get; set; } = [];

        /// <summary>
        /// The files attached to this page.
        /// </summary>
        public ICollection<PageFile> PageFiles { get; set; } = [];

        /// <summary>
        /// The processing instructions declared by this page's markup.
        /// </summary>
        public ICollection<PageProcessingInstruction> PageProcessingInstructions { get; set; } = [];

        /// <summary>
        /// The outgoing page references (links) that originate from this page.
        /// </summary>
        public ICollection<PageReference> PageReferencePages { get; set; } = [];

        /// <summary>
        /// The incoming page references (links) from other pages that target this page.
        /// </summary>
        public ICollection<PageReference> PageReferenceReferencesPages { get; set; } = [];

        /// <summary>
        /// The file attachments associated with revisions of this page.
        /// </summary>
        public ICollection<PageRevisionAttachment> PageRevisionAttachments { get; set; } = [];

        /// <summary>
        /// The tags associated with this page.
        /// </summary>
        public ICollection<PageTag> PageTags { get; set; } = [];

        /// <summary>
        /// The search tokens extracted from this page's content.
        /// </summary>
        public ICollection<PageToken> PageTokens { get; set; } = [];
    }
}
