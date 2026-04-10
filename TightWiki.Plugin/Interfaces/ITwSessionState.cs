using Microsoft.AspNetCore.Http;

namespace TightWiki.Plugin.Interfaces
{
    /// <summary>
    /// Defines the contract for managing and querying the current user's session state, including profile information,
    /// query string parameters, permission checks, and time zone preferences within the application.
    /// </summary>
    /// <remarks>This interface provides methods for checking user permissions in the context of a specific
    /// page, as well as enforcing required permissions. It also exposes properties for accessing the user's profile and
    /// query string data, and methods for handling date and time localization based on user preferences.
    /// Implementations are expected to be used primarily in view components rather than controllers, especially for
    /// permission checks that depend on the current page context.</remarks>
    public interface ITwSessionState
    {
        /// Account profile of the current user. This may be null if the user is not logged in or if the profile information is unavailable.
        public ITwAccountProfile? Profile { get; set; }

        /// The query string parameters of the current request. This may be null if there are no query parameters or if the information is unavailable.
        IQueryCollection? QueryString { get; set; }

        /// <summary>
        /// Returns true if the user holds any of the the given permissions for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public Task<bool> HoldsPermission(TwPermission[] permissions);

        /// <summary>
        /// Returns true if the user holds the given permission for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public Task<bool> HoldsPermission(TwPermission permission);

        /// <summary>
        /// Returns true if the user holds the given permission for given page.
        /// </summary>
        public Task<bool> HoldsPermission(string? givenCanonical, TwPermission permission);

        /// <summary>
        /// Returns true if the user holds any of the given permission for given page.
        /// </summary>
        public Task<bool> HoldsPermission(string? givenCanonical, TwPermission[] permissions);

        /// <summary>
        /// Throws an exception if the user does not hold the given permission for given page.
        /// </summary>
        public Task RequirePermission(string? givenCanonical, TwPermission permission);

        /// <summary>
        /// Throws an exception if the user does not hold any of the given permission for given page.
        /// </summary>
        public Task RequirePermission(string? givenCanonical, TwPermission[] permissions);

        /// <summary>
        /// Throws an exception if the user is not an administrator.
        /// </summary>
        public Task RequireAdminPermission();

        /// <summary>
        /// Converts the specified date and time value to the local time zone of the current context.
        /// </summary>
        /// <remarks>If the Kind property of the input value is DateTimeKind.Utc, the method converts it
        /// to the local time. If the Kind is DateTimeKind.Local, the value is returned unchanged. If the Kind is
        /// DateTimeKind.Unspecified, the method treats the value as a local time.</remarks>
        /// <param name="datetime">The date and time value to convert. The value is interpreted according to its Kind property.</param>
        /// <returns>A DateTime value representing the input date and time adjusted to the local time zone.</returns>
        public DateTime LocalizeDateTime(DateTime datetime);

        /// <summary>
        /// Gets the preferred time zone for the current user or context.
        /// </summary>
        /// <remarks>The preferred time zone is typically determined by user settings or application
        /// configuration. Callers should handle cases where the returned time zone may differ from the system
        /// default.</remarks>
        /// <returns>A <see cref="TimeZoneInfo"/> object representing the preferred time zone. If no preference is set, the
        /// default time zone may be returned.</returns>
        public TimeZoneInfo GetPreferredTimeZone();
    }
}
