namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Represents a user account profile with identity, contact, and personalization information.
    /// </summary>
    /// <remarks>This interface defines the contract for accessing and modifying user profile data, including
    /// identifiers, preferences, and localization settings. Implementations may enforce additional validation or
    /// constraints on property values.</remarks>
    public interface ITwAccountProfile
    {
        /// <summary>
        /// Gets or sets the unique identifier for the user.
        /// </summary>
        public Guid UserId { get; set; }
        /// <summary>
        /// Gets or sets the email address associated with the entity.
        /// </summary>
        public string EmailAddress { get; set; }
        /// <summary>
        /// Gets or sets the name of the account associated with this instance.
        /// </summary>
        public string AccountName { get; set; }
        /// <summary>
        /// Gets or sets the navigation associated with the account. This is a URL friendly version of the account name.
        /// </summary>
        public string Navigation { get; set; }
        /// <summary>
        /// Gets or sets the theme preference for the user.
        /// </summary>
        public string? Theme { get; set; }
        /// <summary>
        /// Gets or sets the time zone identifier associated with the current context.
        /// </summary>
        /// <remarks>The time zone identifier should follow the IANA or Windows time zone naming
        /// conventions, depending on the application's requirements. This property can be used to localize date and
        /// time values for users in different regions.</remarks>
        public string TimeZone { get; set; }
        /// <summary>
        /// Gets or sets the language preference for the user.
        /// </summary>
        public string Language { get; set; }
    }
}
