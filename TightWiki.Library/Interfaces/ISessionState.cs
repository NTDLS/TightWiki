using Microsoft.AspNetCore.Http;
using static TightWiki.Library.Constants;

namespace TightWiki.Library.Interfaces
{
    public interface ISessionState
    {
        public IAccountProfile? Profile { get; set; }

        IQueryCollection? QueryString { get; set; }

        /// <summary>
        /// Returns true if the user holds any of the the given permissions for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public bool HoldsPermission(Permission[] permissions);

        /// <summary>
        /// Returns true if the user holds the given permission for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public bool HoldsPermission(Permission permission);

        /// <summary>
        /// Returns true if the user holds the given permission for given page.
        /// </summary>
        public bool HoldsPermission(string? givenCanonical, Permission permission);

        /// <summary>
        /// Returns true if the user holds any of the given permission for given page.
        /// </summary>
        public bool HoldsPermission(string? givenCanonical, Permission[] permissions);

        /// <summary>
        /// Throws an exception if the user does not hold the given permission for given page.
        /// </summary>
        public void RequirePermission(string? givenCanonical, Permission permission);

        /// Throws an exception if the user does not hold any of the given permission for given page.
        /// </summary>
        public void RequirePermission(string? givenCanonical, Permission[] permissions);

        /// <summary>
        /// Throws an exception if the user is not an administrator.
        /// </summary>
        public void RequireAdminPermission();

        public DateTime LocalizeDateTime(DateTime datetime);
        public TimeZoneInfo GetPreferredTimeZone();
    }
}
