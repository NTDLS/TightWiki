namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents a default wiki page used to seed the database with built-in content on first run,
    /// such as help pages, include pages, and other pages required for the wiki's initial operation.
    /// </summary>
    public class TwDefaultWikiPage
    {
        /// <summary>
        /// The primary key of the source page in the reference SQLite database (Data\pages.db's Page.Id). Carried
        /// through the seed pipeline so that provider-neutral seeding can reproduce the same Id as the SQLite
        /// reference (e.g. for Id-order-dependent behavior such as "Similar"/"Backlinks"/"Related" page listings).
        /// Not used by the SQLite provider itself (DatabaseManager.cs assigns Id via its own existing-page lookup).
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The full name of the page, including namespace prefix if applicable.
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The namespace this page belongs to, or an empty string if it has no namespace.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path used to locate this page.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// A short description of the page content, used in search results and meta tags.
        /// </summary>
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// The creation date/time of the source page in the reference SQLite database. Carried through the seed
        /// pipeline for provider-neutral seeding parity (e.g. "recently created" listings); not used by the
        /// SQLite provider itself, which assigns its own timestamp at seed time.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The last-modified date/time of the source page's own Page row (Data\pages.db Page.ModifiedDate) in the
        /// reference SQLite database. Carried through the seed pipeline for provider-neutral seeding parity; not
        /// used by the SQLite provider itself, which assigns its own timestamp at seed time. Distinct from
        /// <see cref="RevisionModifiedDate"/> - the reference database's Page.ModifiedDate and
        /// PageRevision.ModifiedDate columns routinely diverge (e.g. an unrelated metadata refresh can touch
        /// Page.ModifiedDate without editing the page's content) - see <see cref="RevisionModifiedDate"/>'s own
        /// doc comment.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The last-modified date/time of the source page's current PageRevision row (Data\pages.db
        /// PageRevision.ModifiedDate, for the revision matching <see cref="Revision"/>) in the reference SQLite
        /// database. Carried through the seed pipeline for provider-neutral seeding parity - "recently
        /// modified"/"recently created" listings are ordered/displayed off of this column (see
        /// GetTopRecentlyModifiedPagesInfo.sql's own <c>PR.ModifiedDate</c>), not <see cref="ModifiedDate"/>; not
        /// used by the SQLite provider itself, which assigns its own timestamp at seed time.
        /// </summary>
        public DateTime RevisionModifiedDate { get; set; }

        /// <summary>
        /// The revision number of this default page.
        /// </summary>
        public int Revision { get; set; }

        /// <summary>
        /// A hash of the page content used to detect whether the default content has changed.
        /// </summary>
        public int DataHash { get; set; }

        /// <summary>
        /// The raw wiki markup body of this default page.
        /// </summary>
        public string Body { get; set; } = string.Empty;
    }
}