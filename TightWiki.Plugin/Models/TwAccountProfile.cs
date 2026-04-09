using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// This model combines the elements of an account and a profile into one class.
    /// </summary>
    public partial class TwAccountProfile
        : ITwAccountProfile
    {
        /// <summary>
        /// The user's preferred visual theme, or null to use the system default.
        /// </summary>
        public string? Theme { get; set; }

        /// <summary>
        /// The unique identifier for this user account.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The user's email address.
        /// </summary>
        public string EmailAddress { get; set; } = string.Empty;

        /// <summary>
        /// The user's account name used for login and display.
        /// </summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path for this user's public profile.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The user's first name, or null if not provided.
        /// </summary>
        public string? FirstName { get; set; }

        /// <summary>
        /// The user's last name, or null if not provided.
        /// </summary>
        public string? LastName { get; set; } = string.Empty;

        /// <summary>
        /// The user's preferred time zone identifier.
        /// </summary>
        public string TimeZone { get; set; } = string.Empty;

        /// <summary>
        /// The user's country.
        /// </summary>
        public string Country { get; set; } = string.Empty;

        /// <summary>
        /// The user's preferred language.
        /// </summary>
        public string Language { get; set; } = string.Empty;

        /// <summary>
        /// A short biography or description provided by the user, or null if not provided.
        /// </summary>
        public string? Biography { get; set; } = string.Empty;

        /// <summary>
        /// Indicates whether the user's email address has been confirmed.
        /// </summary>
        public bool EmailConfirmed { get; set; }

        /// <summary>
        /// The user's avatar image as a byte array, or null if no avatar has been uploaded.
        /// </summary>
        public byte[]? Avatar { get; set; }

        /// <summary>
        /// The date and time this account was created.
        /// </summary>
        public DateTime CreatedDate { get; set; }

        /// <summary>
        /// The date and time this account was last modified.
        /// </summary>
        public DateTime ModifiedDate { get; set; }

        /// <summary>
        /// The number of items per page used when paginating lists on this user's profile.
        /// </summary>
        public int PaginationPageSize { get; set; }

        /// <summary>
        /// The total number of pages available when paginating lists on this user's profile.
        /// </summary>
        public int PaginationPageCount { get; set; }
    }
}
