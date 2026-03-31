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
        SqliteManagedFactory UsersFactory { get; }

        Task<bool> IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false);
        Task DeleteRole(int roleId);
        Task<bool> InsertRole(string name, string? description);
        Task<bool> DoesRoleExist(string name);
        Task<bool> IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = true);
        Task<TwInsertAccountPermissionResult?> InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId);
        Task<bool> IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = false);
        Task<List<TwRole>> AutoCompleteRole(string? searchText);
        Task<List<TwAccountProfile>> AutoCompleteAccount(string? searchText);
        Task<TwAddRoleMemberResult?> AddRoleMemberByname(Guid userId, string roleName);
        Task<TwAddRoleMemberResult?> AddRoleMember(Guid userId, int roleId);
        Task<TwAddAccountMembershipResult?> AddAccountMembership(Guid userId, int roleId);
        Task RemoveRoleMember(int roleId, Guid userId);
        Task RemoveRolePermission(int id);
        Task RemoveAccountPermission(int id);
        Task<TwInsertRolePermissionResult?> InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId);
        /// <summary>
        /// Gets the apparent account permissions for a user combined with the permissions of all roles that user is a member of.
        /// </summary>
        Task<List<TwApparentPermission>> GetApparentAccountPermissions(Guid userId);
        Task<List<TwApparentPermission>> GetApparentRolePermissions(WikiRoles role);
        Task<List<TwApparentPermission>> GetApparentRolePermissions(string roleName);
        Task<List<TwPermissionDisposition>> GetAllPermissionDispositions();
        Task<List<TwPermission>> GetAllPermissions();
        Task<List<TwRolePermission>> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        Task<List<TwAccountProfile>> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null);
        Task AnonymizeProfile(Guid userId);
        Task<bool> IsUserMemberOfAdministrators(Guid userId);
        Task<TwRole> GetRoleByName(string name);
        Task<List<TwRole>> GetAllRoles(string? orderBy = null, string? orderByDirection = null);
        Task<List<TwAccountProfile>> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        Task<List<TwAccountPermission>> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        Task<List<TwAccountRoleMembership>> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        Task<List<TwAccountProfile>> GetAllUsers();
        Task<List<TwAccountProfile>> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null);
        Task CreateProfile(Guid userId, string accountName);
        Task<bool> DoesEmailAddressExist(string? emailAddress);
        Task<bool> DoesProfileAccountExist(string navigation);
        Task<TwAccountProfile?> GetBasicProfileByUserId(Guid userId);
        Task<TwAccountProfile> GetAccountProfileByUserId(Guid userId, bool forceReCache = false);
        Task SetProfileUserId(string navigation, Guid userId);
        Task<Guid?> GetUserAccountIdByNavigation(string navigation);
        Task<TwAccountProfile> GetAccountProfileByNavigation(string? navigation);
        Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPasswordHash(string accountNameOrEmail, string passwordHash);
        Task<TwAccountProfile?> GetProfileByAccountNameOrEmailAndPassword(string accountNameOrEmail, string password);
        Task<TwProfileAvatar?> GetProfileAvatarByNavigation(string navigation);
        Task UpdateProfile(TwAccountProfile item);
        Task UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType);
        Task<WikiAdminPasswordChangeState> AdminPasswordStatus();
        Task SetAdminPasswordClear();
        Task SetAdminPasswordIsChanged();
        Task SetAdminPasswordIsDefault();

        #region Security.

        /// <summary>
        /// Detect whether this is the first time the WIKI has ever been run and do some initialization.
        /// Adds the first user with the email and password contained in Constants.DEFAULTUSERNAME and Constants.DEFAULTPASSWORD
        /// </summary>
        void ValidateEncryptionAndCreateAdminUser(UserManager<IdentityUser> userManager);
        Task UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims);

        #endregion
    }
}
