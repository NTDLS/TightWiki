using Microsoft.AspNetCore.Http;

namespace TightWiki.Plugin.Interfaces
{
    public interface ISessionState
    {
        public IAccountProfile? Profile { get; set; }

        IQueryCollection? QueryString { get; set; }

        /// <summary>
        /// Returns true if the user holds any of the the given permissions for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public Task<bool> HoldsPermission(WikiPermission[] permissions);

        /// <summary>
        /// Returns true if the user holds the given permission for the current page.
        /// This is only applicable after SetPageId() has been called, to this is intended to be used in views NOT controllers.
        /// </summary>
        public Task<bool> HoldsPermission(WikiPermission permission);

        /// <summary>
        /// Returns true if the user holds the given permission for given page.
        /// </summary>
        public Task<bool> HoldsPermission(string? givenCanonical, WikiPermission permission);

        /// <summary>
        /// Returns true if the user holds any of the given permission for given page.
        /// </summary>
        public Task<bool> HoldsPermission(string? givenCanonical, WikiPermission[] permissions);

        /// <summary>
        /// Throws an exception if the user does not hold the given permission for given page.
        /// </summary>
        public Task RequirePermission(string? givenCanonical, WikiPermission permission);

        /// Throws an exception if the user does not hold any of the given permission for given page.
        /// </summary>
        public Task RequirePermission(string? givenCanonical, WikiPermission[] permissions);

        /// <summary>
        /// Throws an exception if the user is not an administrator.
        /// </summary>
        public Task RequireAdminPermission();

        public DateTime LocalizeDateTime(DateTime datetime);
        public TimeZoneInfo GetPreferredTimeZone();
    }
}
