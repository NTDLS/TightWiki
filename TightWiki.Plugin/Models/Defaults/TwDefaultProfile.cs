namespace TightWiki.Plugin.Models.Defaults
{
    /// <summary>
    /// Represents the display-only fields of the built-in admin's Users.Profile row (Data\users.db's Profile,
    /// matched by Navigation='admin'), used to seed the corresponding Profile row on first run. Deliberately
    /// carries only <see cref="AccountName"/> - the one column actually copied verbatim from the reference; every
    /// other seeded field on that row (UserId, Navigation, CreatedDate, ModifiedDate) is either bound to a
    /// freshly-created ASP.NET Identity user (not portable across environments) or derived at seed time rather
    /// than copied (Navigation is computed via TwNavigation.Clean(AccountName), same as the reference itself
    /// derives it - see EnsureAdminUser in SqlServerDatabaseManager/PostgresDatabaseManager).
    /// </summary>
    public class TwDefaultProfile
    {
        /// <summary>
        /// The admin account's display/login name, as recorded in the reference database (e.g. "Admin", not the
        /// lowercase "admin" its Navigation normalizes to).
        /// </summary>
        public string AccountName { get; set; } = string.Empty;
    }
}
