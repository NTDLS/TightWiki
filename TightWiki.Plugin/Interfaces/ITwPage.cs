namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Represents a page within a wiki or content management system, providing access to its metadata, content, and
    /// revision information.
    /// </summary>
    public interface ITwPage
    {
        /// Gets the unique identifier for the page.
        int Id { get; }
        /// Gets the unique identifier for the page's revision.
        int Revision { get; }
        /// Gets the unique identifier for the page's most current revision.
        int MostCurrentRevision { get; }
        /// Gets or sets the name of the page, which serves as its title and identifier within the wiki.
        string Name { get; set; }
        /// Gets the description of the page, providing a brief summary or overview of its content.
        string Description { get; }
        /// Gets the namespace of the page, which categorizes it within the wiki and helps organize content.
        string Namespace { get; }
        /// Gets the title of the page, which is typically displayed as the main heading when viewing the page.
        string Title { get; }
        /// Gets the body content of the page, which contains the main text and information presented to users when they view the page.
        string Body { get; }
        /// Gets the navigation path of the page, which is a URL friendly version of the page name used for routing and linking within the wiki.
        string Navigation { get; }
        /// Gets the date and time when the page was created, providing a timestamp for when the page was first added to the wiki.
        DateTime CreatedDate { get; }
        /// Gets the date and time when the page was last modified, providing a timestamp for the most recent update to the page's content or metadata.
        DateTime ModifiedDate { get; }
        /// Gets the username of the user who last modified the page, allowing for tracking of changes and accountability within the wiki.
        string ModifiedByUserName { get; }
        /// Gets the username of the user who created the page, allowing for tracking of the original author and accountability within the wiki.
        string CreatedByUserName { get; }
        ///  Gets a value indicating whether the page is a historical version, which means it represents a previous revision of the page rather than the most current version.
        bool IsHistoricalVersion { get; }
        /// Gets a value indicating whether the page is a real page, which means it exists in the wiki and is not a placeholder or deleted page.
        bool Exists { get; }
        /// Gets the total number of times the page has been viewed, providing insight into its popularity and user engagement within the wiki.
        int TotalViewCount { get; set; }
    }
}
