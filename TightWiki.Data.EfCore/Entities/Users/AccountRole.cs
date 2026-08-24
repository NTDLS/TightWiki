namespace TightWiki.Data.EfCore.Entities.Users
{
    /// <summary>
    /// A role membership assignment for a user (Users.AccountRole) - grants a user membership in a <see
    /// cref="Users.Role"/>.
    /// </summary>
    public class AccountRole
    {
        /// <summary>
        /// The unique identifier for this role membership record.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The identifier of the user this role membership belongs to. References <see cref="Users.Profile"/>
        /// (Profile.UserId), which in turn matches AspNetUsers.Id.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The identifier of the role assigned to the user.
        /// </summary>
        public int RoleId { get; set; }

        /// <summary>
        /// The profile of the user this role membership belongs to.
        /// </summary>
        public Profile Profile { get; set; } = null!;

        /// <summary>
        /// The role assigned to the user.
        /// </summary>
        public Role Role { get; set; } = null!;
    }
}
