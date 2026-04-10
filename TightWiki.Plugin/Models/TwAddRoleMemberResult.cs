namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents the result of adding a user to a role, including details about the created role membership and the
    /// user added.
    /// </summary>
    public class TwAddRoleMemberResult
    {
        /// <summary>
        /// The unique identifier of the role membership record that was created.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The unique identifier of the user who was added to the role.
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// The URL-safe navigation path for the user's public profile.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// The account name of the user who was added to the role.
        /// </summary>
        public string AccountName { get; set; } = string.Empty;

        /// <summary>
        /// The email address of the user who was added to the role.
        /// </summary>
        public string EmailAddress { get; set; } = string.Empty;

        /// <summary>
        /// The first name of the user who was added to the role.
        /// </summary>
        public string FirstName { get; set; } = string.Empty;

        /// <summary>
        /// The last name of the user who was added to the role.
        /// </summary>
        public string LastName { get; set; } = string.Empty;
    }
}