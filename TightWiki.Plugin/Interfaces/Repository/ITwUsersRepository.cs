using Microsoft.AspNetCore.Identity;
using NTDLS.SqliteDapperWrapper;
using System.Security.Claims;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    /// Data access for users, roles, permissions, and related data.
    /// This includes user profiles, role memberships, account permissions, role permissions, and related data.
    /// </summary>
    public interface ITwUsersRepository
    {
        /// <summary>
        /// SQLite factory used to access the users database.
        /// </summary>
        SqliteManagedFactory UsersFactory { get; }

        /// <summary>
        /// Returns true if the specified user is a member of the specified role.
        /// </summary>
        Task<bool> IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false);

        /// <summary>
        /// Permanently deletes the specified role and all associated memberships and permissions.
        /// </summary>
        Task DeleteRole(int roleId);

        /// <summary>
        /// Creates a new role with the specified name and optional description. Returns true if the role was created successfully.
        /// </summary>
        Task<bool> InsertRole(string name, string? description);

        /// <summary>
        /// Returns true if a role with the specified name already exists.
        /// </summary>
        Task<bool> DoesRoleExist(string name);

        /// <summary>
        /// Returns true if a matching account permission record already exists for the specified user, permission, disposition, namespace, and page.
        /// </summary>
        Task<bool> IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = true);

        /// <summary>
        /// Inserts a new permission record for the specified user, or returns null if the permission already exists.
        /// </summary>
        Task<TwInsertAccountPermissionResult?> InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId);

        /// <summary>
        /// Returns true if a matching role permission record already exists for the specified role, permission, disposition, namespace, and page.
        /// </summary>
        Task<bool> IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = false);

        /// <summary>
        /// Returns roles whose names match the given search text, for use in autocomplete suggestions.
        /// </summary>
        Task<List<TwRole>> AutoCompleteRole(string? searchText);

        /// <summary>
        /// Returns accounts whose names match the given search text, for use in autocomplete suggestions.
        /// </summary>
        Task<List<TwAccountProfile>> AutoCompleteAccount(string? searchText);

        /// <summary>
        /// Adds the specified user to the role identified by name. Returns the result, or null if the operation failed.
        /// </summary>
        Task<TwAddRoleMemberResult?> AddRoleMemberByname(Guid userId, string roleName);

        /// <summary>
        /// Adds the specified user to the role identified by ID. Returns the result, or null if the operation failed.
        /// </summary>
        Task<TwAddRoleMemberResult?> AddRoleMember(Guid userId, int roleId);

        /// <summary>
        /// Adds a membership record associating the specified user with the specified role. Returns the result, or null if the operation failed.
        /// </summary>
        Task<TwAddAccountMembershipResult?> AddAccountMembership(Guid userId, int roleId);

        /// <summary>
        /// Removes the specified user from the specified role.
        /// </summary>
        Task RemoveRoleMember(int roleId, Guid userId);

        /// <summary>
        /// Removes the role permission record with the specified ID.
        /// </summary>
        Task RemoveRolePermission(int id);

        /// <summary>
        /// Removes the account permission record with the specified ID.
        /// </summary>
        Task RemoveAccountPermission(int id);

        /// <summary>
        /// Inserts a new permission record for the specified role. Returns the result, or null if the permission already exists.
        /// </summary>
        Task<TwInsertRolePermissionResult?> InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId);

        /// <summary>
        /// Returns the combined effective permissions for a user, merging their direct account permissions with those inherited from all role memberships.
        /// </summary>
        Task<List<TwApparentPermission>> GetApparentAccountPermissions(Guid userId);

        /// <summary>
        /// Returns the effective permissions for the specified role.
        /// </summary>
        Task<List<TwApparentPermission>> GetApparentRolePermissions(TwRoles role);

        /// <summary>
        /// Returns the effective permissions for the role with the specified name.
        /// </summary>
        Task<List<TwApparentPermission>> GetApparentRolePermissions(string roleName);

        /// <summary>
        /// Returns all available permission disposition types (e.g. Allow, Deny).
        /// </summary>
        Task<List<Models.TwPermissionDisposition>> GetAllPermissionDispositions();

        /// <summary>
        /// Returns all defined permissions in the system.
        /// </summary>
        Task<List<Models.TwPermission>> GetAllPermissions();

        /// <summary>
        /// Returns a paged list of permissions assigned to the specified role, with optional sorting and page size.
        /// </summary>
        Task<List<TwRolePermission>> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Returns a paged list of public user profiles, with optional search filtering and page size.
        /// </summary>
        Task<List<TwAccountProfile>> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null);

        /// <summary>
        /// Removes personally identifiable information from the specified user's profile.
        /// </summary>
        Task AnonymizeProfile(Guid userId);

        /// <summary>
        /// Returns true if the specified user is a member of the Administrators role.
        /// </summary>
        Task<bool> IsUserMemberOfAdministrators(Guid userId);

        /// <summary>
        /// Returns the role with the specified name.
        /// </summary>
        Task<TwRole> GetRoleByName(string name);

        /// <summary>
        /// Returns all roles in the system, with optional sorting.
        /// </summary>
        Task<List<TwRole>> GetAllRoles(string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Returns a paged list of members belonging to the specified role, with optional sorting and page size.
        /// </summary>
        Task<List<TwAccountProfile>> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Returns a paged list of direct account permissions for the specified user, with optional sorting and page size.
        /// </summary>
        Task<List<TwAccountPermission>> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Returns a paged list of role memberships for the specified user, with optional sorting and page size.
        /// </summary>
        Task<List<TwAccountRoleMembership>> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Returns all user accounts in the system.
        /// </summary>
        Task<List<TwAccountProfile>> GetAllUsers();

        /// <summary>
        /// Returns a paged list of all user accounts, with optional sorting and search filtering.
        /// </summary>
        Task<List<TwAccountProfile>> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null);

        /// <summary>
        /// Creates a new user profile record for the specified user ID and account name.
        /// </summary>
        Task CreateProfile(Guid userId, string accountName);

        /// <summary>
        /// Returns true if a user account with the specified email address already exists.
        /// </summary>
        Task<bool> DoesEmailAddressExist(string? emailAddress);

        /// <summary>
        /// Returns true if a user profile with the specified navigation path already exists.
        /// </summary>
        Task<bool> DoesProfileAccountExist(string navigation);

        /// <summary>
        /// Returns basic profile info for the specified user ID, or null if not found.
        /// </summary>
        Task<TwAccountProfile?> GetBasicProfileByUserId(Guid userId);

        /// <summary>
        /// Returns the full account profile for the specified user ID, with optional cache bypass.
        /// </summary>
        Task<TwAccountProfile> GetAccountProfileByUserId(Guid userId, bool forceReCache = false);

        /// <summary>
        /// Updates the user ID associated with the profile at the specified navigation path.
        /// </summary>
        Task SetProfileUserId(string navigation, Guid userId);

        /// <summary>
        /// Returns the user account ID for the profile at the specified navigation path, or null if not found.
        /// </summary>
        Task<Guid?> GetUserAccountIdByNavigation(string navigation);

        /// <summary>
        /// Returns the account profile for the specified navigation path, or null if not found.
        /// </summary>
        Task<TwAccountProfile?> GetAccountProfileByNavigation(string? navigation);

        /// <summary>
        /// Returns the account profile matching the specified account name or email address and password hash, or null if not found.
        /// </summary>
        Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash);

        /// <summary>
        /// Returns the account profile matching the specified account name or email address and plaintext password, or null if not found.
        /// </summary>
        Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password);

        /// <summary>
        /// Returns the avatar image for the profile at the specified navigation path, or null if not found.
        /// </summary>
        Task<TwProfileAvatar?> GetProfileAvatarByNavigation(string navigation);

        /// <summary>
        /// Updates the profile record for the specified user.
        /// </summary>
        Task UpdateProfile(TwAccountProfile item);

        /// <summary>
        /// Updates the avatar image for the specified user.
        /// </summary>
        Task UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType);

        /// <summary>
        /// Returns the current state of the admin default password.
        /// </summary>
        Task<TwAdminPasswordChangeState> AdminPasswordStatus();

        /// <summary>
        /// Clears the admin password change state record.
        /// </summary>
        Task SetAdminPasswordClear();

        /// <summary>
        /// Marks the admin password as having been changed from the default.
        /// </summary>
        Task SetAdminPasswordIsChanged();

        /// <summary>
        /// Resets the admin password change state to the default, triggering a warning in the UI.
        /// </summary>
        Task SetAdminPasswordIsDefault();

        #region Security.

        /// <summary>
        /// Validates encryption configuration and creates the initial admin user on first run.
        /// Adds the first user with the credentials defined in Constants.DEFAULTUSERNAME and Constants.DEFAULTPASSWORD.
        /// </summary>
        void ValidateEncryptionAndCreateAdminUser(UserManager<IdentityUser> userManager);

        /// <summary>
        /// Inserts or updates the claims for the specified Identity user, replacing any existing claims with those provided.
        /// </summary>
        Task UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims);

        #endregion
    }
}