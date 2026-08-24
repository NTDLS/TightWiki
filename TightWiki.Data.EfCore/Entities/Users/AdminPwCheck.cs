namespace TightWiki.Data.EfCore.Entities.Users
{
    /// <summary>
    /// A single-row status flag tracking whether the built-in Administrator account's password has been changed
    /// from its initial default value (Users.AdminPwCheck). The table has no primary key and holds at most one
    /// row - see SetAdminPasswordIsChanged.sql, SetAdminPasswordIsDefault.sql, and SetAdminPasswordClear.sql in
    /// TightWiki.Repository for how the row is written.
    /// </summary>
    public class AdminPwCheck
    {
        /// <summary>
        /// 1 if the admin password has been changed from its default, 0 if it is still the default. The row (and
        /// therefore this value) may also be entirely absent once the check has been cleared.
        /// </summary>
        public int? Value { get; set; }
    }
}
