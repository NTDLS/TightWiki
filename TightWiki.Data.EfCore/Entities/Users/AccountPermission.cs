namespace TightWiki.Data.EfCore.Entities.Users
{
    /// <summary>
    /// An account-level permission record (Users.AccountPermission) - grants or denies a specific permission to
    /// an individual user, optionally scoped to a namespace or page.
    /// </summary>
    public class AccountPermission
    {
        /// <summary>
        /// The unique identifier for this account permission record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the user this permission record applies to. References <see cref="Users.Profile"/>
        /// (Profile.UserId), which in turn matches AspNetUsers.Id.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The identifier of the permission being granted or denied.
        /// </summary>
        public int PermissionId { get; set; }

        /// <summary>
        /// The namespace this permission is scoped to, or null if it applies across all namespaces.
        /// </summary>
        public string? Namespace { get; set; }

        /// <summary>
        /// The page identifier this permission is scoped to (a literal page id or the "*" wildcard), or null if
        /// it applies across all pages.
        /// </summary>
        public string? PageId { get; set; }

        /// <summary>
        /// The identifier of the disposition (allow/deny) applied to this permission record.
        /// </summary>
        public int PermissionDispositionId { get; set; }

        /// <summary>
        /// The profile of the user this permission record applies to.
        /// </summary>
        public Profile Profile { get; set; } = null!;

        /// <summary>
        /// The permission being granted or denied.
        /// </summary>
        public Permission Permission { get; set; } = null!;

        /// <summary>
        /// The disposition (allow/deny) applied to this permission record.
        /// </summary>
        public PermissionDisposition PermissionDisposition { get; set; } = null!;
    }
}
