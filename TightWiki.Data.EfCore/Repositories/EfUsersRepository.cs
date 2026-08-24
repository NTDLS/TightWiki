using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;
using UsersEntities = TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Repositories
{
    /// <summary>
    /// Provider-agnostic (SQL Server/Postgres, per Database-Providers-Plan.md chapter 3) LINQ-over-EF-Core
    /// implementation of <see cref="ITwUsersRepository"/>. Lives in the shared <c>TightWiki.Data.EfCore</c>
    /// project rather than a per-provider driver project, for the same reason as <see cref="EfConfigurationRepository"/>/
    /// <see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/<see cref="EfStatisticsRepository"/>/
    /// <see cref="EfPageRepository"/> (see those classes' doc comments): plain LINQ against
    /// <see cref="TightWikiDbContext"/> needs no provider-specific code here at all. Originally landed as a
    /// <c>SqlServerUsersRepository</c> stub under <c>TightWiki.Data.EfCore.SqlServer/Repositories/</c> in
    /// phase 2a.1; moved here (still a stub) in phase 2b.1.
    /// </summary>
    /// <remarks>
    /// <para>
    /// 12 of 49 members (the role CRUD/membership category - <see cref="IsAccountAMemberOfRole"/>,
    /// <see cref="DeleteRole"/>, <see cref="InsertRole"/>, <see cref="DoesRoleExist"/>, <see cref="AutoCompleteRole"/>,
    /// <see cref="AddRoleMemberByname"/>, <see cref="AddRoleMember"/>, <see cref="AddAccountMembership"/>,
    /// <see cref="RemoveRoleMember"/>, <see cref="GetRoleByName"/>, <see cref="GetAllRoles"/>, and
    /// <see cref="GetRoleMembersPaged"/>) were implemented for real in phase 2b.9 - see each member's own doc
    /// comment for which SQLite script it mirrors. A further 14 members (the permissions category - <see
    /// cref="IsAccountPermissionDefined"/>, <see cref="InsertAccountPermission"/>, <see cref="IsRolePermissionDefined"/>,
    /// <see cref="RemoveRolePermission"/>, <see cref="RemoveAccountPermission"/>, <see cref="InsertRolePermission"/>,
    /// <see cref="GetApparentAccountPermissions"/>, both <see cref="GetApparentRolePermissions(TwRoles)"/>/
    /// <see cref="GetApparentRolePermissions(string)"/> overloads, <see cref="GetAllPermissionDispositions"/>,
    /// <see cref="GetAllPermissions"/>, <see cref="GetRolePermissionsPaged"/>, <see cref="GetAccountPermissionsPaged"/>,
    /// and <see cref="GetAccountRoleMembershipPaged"/>) were implemented for real in phase 2b.10 - see each
    /// member's own doc comment for which SQLite script it mirrors, and <see cref="ResolveResourceNameAsync"/>'s
    /// remarks for the shared "compute a permission's effective resource name" logic reused across several of them.
    /// A further 17 members (the user-profile category - <see cref="AutoCompleteAccount"/>, <see
    /// cref="GetAllPublicProfilesPaged"/>, <see cref="AnonymizeProfile"/>, <see
    /// cref="IsUserMemberOfAdministrators"/>, <see cref="GetAllUsers"/>, <see cref="GetAllUsersPaged"/>, <see
    /// cref="CreateProfile"/>, <see cref="DoesEmailAddressExist"/>, <see cref="DoesProfileAccountExist"/>, <see
    /// cref="GetBasicProfileByUserId"/>, <see cref="GetAccountProfileByUserId"/>, <see cref="SetProfileUserId"/>,
    /// <see cref="GetUserAccountIdByNavigation"/>, <see cref="GetAccountProfileByNavigation"/>, <see
    /// cref="UpdateProfile"/>, <see cref="UpdateProfileAvatar"/>, and <see cref="GetProfileAvatarByNavigation"/>)
    /// were implemented for real in phase 2b.11 - see each member's own doc comment for which SQLite script it
    /// mirrors, and <see cref="GetAllAccountUserRowsAsync"/>'s/<see cref="BuildFullAccountProfileAsync"/>'s
    /// remarks for the shared cross-<see cref="ApplicationDbContext"/> logic reused across several of them. 4 more
    /// members (the admin default-password state machine - <see cref="AdminPasswordStatus"/>, <see
    /// cref="SetAdminPasswordClear"/>, <see cref="SetAdminPasswordIsChanged"/>, <see
    /// cref="SetAdminPasswordIsDefault"/>) were implemented for real in phase 2b.12.
    /// <c>GetProfileByAccountNameOrEmailAndPasswordHash</c>/<c>GetProfileByAccountNameOrEmailAndPassword</c>, also
    /// originally scoped to phase 2b.12, were removed from <see cref="ITwUsersRepository"/> entirely rather than
    /// implemented: their reference SQL script was confirmed dead/broken code (columns that don't exist on this
    /// schema, unreachable by any caller in the solution, predating the ASP.NET Identity migration) - see this
    /// project's <c>CLAUDE.md</c>/commit history for the escalation writeup. The remaining 2 members - <see
    /// cref="ValidateEncryptionAndCreateAdminUser"/>/<see cref="UpsertUserClaims"/> (initial admin-user
    /// bootstrapping, called from <c>TightWiki/Program.cs</c>'s post-<c>builder.Build()</c> scope with a
    /// DI-resolved <see cref="UserManager{TUser}"/> passed in directly by the caller - unlike every other member
    /// in this class, neither needs a constructor-injected <see cref="UserManager{TUser}"/> factory of its own) -
    /// were implemented for real in phase 2b.13; see each member's own doc comment, including two confirmed,
    /// long-standing reference bugs (both introduced together in commit <c>e5b230aa</c>, "Cleanup.", and never
    /// touched since) that are deliberately not reproduced.
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/>/<see cref="Func{ApplicationDbContext}"/> pair rather than an
    /// injected context instance, mirroring <see cref="EfConfigurationRepository"/> (see that class's doc comment)
    /// - <see cref="SqlServer.SqlServerDatabaseManager"/> passes its own <c>CreateDbContext</c>/
    /// <c>CreateApplicationDbContext</c> method groups in as those two delegates. Also takes an
    /// <see cref="ITwConfigurationRepository"/> directly (not another <see cref="Func{TResult}"/>), added in phase
    /// 2b.9 (the phase 2b.1 skeleton did not have it) - mirroring the SQLite reference constructor's own
    /// <c>ConfigurationRepository configurationRepository</c> parameter and the same pattern already used by
    /// <see cref="EfPageRepository"/>/<see cref="EfLoggingRepository"/>/<see cref="EfEmojiRepository"/>/
    /// <see cref="EfStatisticsRepository"/> - <see cref="GetRoleMembersPaged"/> is the first 2b.9 member that needs
    /// it, to read the "Pagination Size" customization setting (see its own doc comment for the reference's own
    /// quirk of always reading this setting regardless of the caller-supplied <c>pageSize</c> argument).
    /// </para>
    /// </remarks>
    public sealed class EfUsersRepository : ITwUsersRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly Func<ApplicationDbContext> _createIdentityContext;
        private readonly ITwConfigurationRepository _configurationRepository;

        public EfUsersRepository(Func<TightWikiDbContext> createContext, Func<ApplicationDbContext> createIdentityContext,
            ITwConfigurationRepository configurationRepository)
        {
            _createContext = createContext;
            _createIdentityContext = createIdentityContext;
            _configurationRepository = configurationRepository;
        }

        /// <summary>
        /// Mirrors IsAccountAMemberOfRole.sql: whether an Users.AccountRole row exists linking
        /// <paramref name="userId"/> to <paramref name="roleId"/>. Cached under
        /// <see cref="MemCache.Category.Security"/>, same cache key shape (userId + roleId) as the SQLite
        /// reference.
        /// </summary>
        public async Task<bool> IsAccountAMemberOfRole(Guid userId, int roleId, bool forceReCache = false)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security, [userId, roleId]);

            return await MemCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
            {
                using var context = _createContext();
                return await context.AccountRoles.AnyAsync(ar => ar.UserId == userId && ar.RoleId == roleId);
            });
        }

        /// <summary>
        /// Mirrors DeleteRole.sql: permanently deletes the role matching <paramref name="roleId"/> together with
        /// every Users.AccountRole/Users.RolePermission row referencing it - but only if that role exists and is
        /// not <see cref="UsersEntities.Role.IsBuiltIn"/> (a silent no-op otherwise, same guard the reference
        /// applies independently to each of its three <c>DELETE</c> statements via a repeated
        /// <c>NOT IN (SELECT ... WHERE IsBuiltIn = 1)</c> subquery). Deletes the two child tables first via LINQ
        /// bulk <c>ExecuteDeleteAsync</c> (fully provider-portable, no raw SQL needed - same idiom as
        /// <see cref="EfEmojiRepository.DeleteEmojiById"/>), then the role itself - required ordering under the
        /// consolidated schema's real FK constraints (<see cref="Configurations.Users.AccountRoleConfiguration"/>/
        /// <see cref="Configurations.Users.RolePermissionConfiguration"/> both declare a real, not
        /// cascade-deleting, foreign key to Users.Role), unlike the SQLite reference where the three statements'
        /// relative order is only cosmetic. Not reproduced: the reference's guard clause is technically also
        /// satisfied (vacuously) when <paramref name="roleId"/> does not exist as a row at all, which would let it
        /// delete orphaned Users.AccountRole/Users.RolePermission rows left over from some earlier, already-broken
        /// state - a corner case with no equivalent under the new schema's real FK constraints (no such orphans can
        /// exist in the first place), so this checks "role exists and is not built-in" directly instead.
        /// </summary>
        public async Task DeleteRole(int roleId)
        {
            using var context = _createContext();

            var isDeletable = await context.Roles.AnyAsync(r => r.Id == roleId && !r.IsBuiltIn);
            if (!isDeletable)
            {
                return;
            }

            await context.AccountRoles.Where(ar => ar.RoleId == roleId).ExecuteDeleteAsync();
            await context.RolePermissions.Where(rp => rp.RoleId == roleId).ExecuteDeleteAsync();
            await context.Roles.Where(r => r.Id == roleId).ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors InsertRole.sql: inserts a new, non-built-in Users.Role row (<see cref="UsersEntities.Role.IsBuiltIn"/>
        /// hardcoded to <see langword="false"/>, same literal <c>0</c> the reference script inserts - there is no
        /// UI path to create a built-in role).
        /// </summary>
        /// <remarks>
        /// Returns whether the insert actually affected a row, <b>not</b> reproduced from the SQLite reference's
        /// own return value: <c>UsersFactory.ExecuteScalarAsync&lt;bool?&gt;("InsertRole.sql", param) ?? false</c>
        /// always evaluates to <see langword="false"/>, insert success or not - confirmed by probing
        /// <c>Microsoft.Data.Sqlite</c>'s <c>DbCommand.ExecuteScalar()</c> directly against an equivalent
        /// <c>INSERT ... SELECT</c> statement with no <c>RETURNING</c>/result set: it returns <see langword="null"/>
        /// regardless of whether the row was inserted, which the reference's own <c>?? false</c> then silently
        /// turns into "failed". This is dead code, not a load-bearing quirk: the only caller
        /// (<c>AdminSecurityController.AddRole</c>) does <c>await usersRepository.InsertRole(...)</c> and discards
        /// the result entirely, so fixing the return value here changes no observable application behavior - it
        /// only makes this member actually satisfy its own interface doc comment ("Returns true if the role was
        /// created successfully").
        /// </remarks>
        public async Task<bool> InsertRole(string name, string? description)
        {
            using var context = _createContext();

            context.Roles.Add(new UsersEntities.Role
            {
                Name = name,
                Description = description,
                IsBuiltIn = false,
            });

            return await context.SaveChangesAsync() > 0;
        }

        /// <summary>
        /// Mirrors DoesRoleExist.sql: whether a Users.Role row with the exact <paramref name="name"/> exists.
        /// Case sensitivity for the comparison is entirely determined by the DB-level collation on
        /// <see cref="UsersEntities.Role.Name"/> (<see cref="Configurations.Users.RoleConfiguration"/>: SQLite
        /// keeps the reference's <c>COLLATE NOCASE</c>; other providers fall back to the database's own default
        /// collation - see <see cref="TightWikiDbContext.StripNonSqliteNoCaseCollation"/>), same as every other
        /// plain <c>==</c>/<c>Contains</c> comparison against this column in this class - no client-side
        /// <see cref="StringComparer"/> is needed here because this filter is translated to SQL and evaluated by
        /// the database, not materialized into memory first (contrast <see cref="GetRoleMembersPaged"/>'s
        /// in-memory sort, which does need one).
        /// </summary>
        public async Task<bool> DoesRoleExist(string name)
        {
            using var context = _createContext();
            return await context.Roles.AnyAsync(r => r.Name == name);
        }

        /// <summary>
        /// Mirrors IsAccountPermissionDefined.sql: whether a Users.AccountPermission row already exists for
        /// <paramref name="userId"/>/<paramref name="permissionId"/>/<paramref name="permissionDispositionId"/>
        /// that already "covers" the requested <paramref name="ns"/>/<paramref name="pageId"/> scope. Not a plain
        /// equality match - the reference's own <c>([Namespace] = @Namespace OR [Namespace] IS NULL) AND (PageId =
        /// @PageId OR PageId IS NULL)</c> clause, worked through SQL's three-valued NULL logic by hand, reduces
        /// exactly to <c>(row.Namespace IS NULL OR row.Namespace = @Namespace) AND (row.PageId IS NULL OR row.PageId
        /// = @PageId)</c> - i.e. an existing row that is broader (global, both columns NULL) than the requested
        /// scope counts as "already defined" too, not just an exact scope match (this is what lets the caller,
        /// <c>AdminSecurityController.AddAccountPermission</c>, skip inserting a redundant namespace/page-scoped
        /// permission when a global one already grants the same permission+disposition). Cached under
        /// <see cref="MemCache.Category.Security"/>, same cache key shape as the SQLite reference. Not reproduced:
        /// <paramref name="permissionDispositionId"/> is a stringified integer (Users.PermissionDisposition.Id) in
        /// both this signature and the reference's own - SQLite's NUMERIC affinity coerces the bound TEXT parameter
        /// for the <c>PermissionDispositionId = @PermissionDispositionId</c> comparison against the INTEGER column;
        /// here it is parsed explicitly instead. A non-numeric <paramref name="permissionDispositionId"/> (never
        /// produced by the only caller, which passes a Users.PermissionDisposition.Id selected from a dropdown)
        /// is treated as "not defined" (<see langword="false"/>) rather than throwing, since no disposition id is
        /// itself a valid integer format.
        /// </summary>
        public async Task<bool> IsAccountPermissionDefined(Guid userId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = true)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security, [userId, permissionId, permissionDispositionId, ns, pageId]);

            return await MemCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
            {
                if (!int.TryParse(permissionDispositionId, out var dispositionId))
                {
                    return false;
                }

                using var context = _createContext();
                return await context.AccountPermissions.AnyAsync(ap =>
                    ap.UserId == userId &&
                    ap.PermissionId == permissionId &&
                    ap.PermissionDispositionId == dispositionId &&
                    (ap.Namespace == null || ap.Namespace == ns) &&
                    (ap.PageId == null || ap.PageId == pageId));
            });
        }

        /// <summary>
        /// Mirrors InsertAccountPermission.sql: inserts a new Users.AccountPermission row for <paramref
        /// name="userId"/>, then returns it joined back to its Permission/PermissionDisposition names and a
        /// computed <see cref="TwInsertAccountPermissionResult.ResourceName"/> (see <see
        /// cref="ResolveResourceNameAsync"/>'s remarks for the exact "Namespace wins, then literal PageId, then
        /// '*' wildcard, then the referenced Pages.Page.Name" precedence, mirrored from the reference script's own
        /// <c>CASE</c> expression). Not reproduced: the reference's own follow-up <c>SELECT</c> re-reads the new
        /// row via three real <c>INNER JOIN</c>s (Permission/PermissionDisposition) plus a <c>LEFT OUTER JOIN</c>
        /// (pages_db.Page) - under the consolidated schema's real, always-enforced FK constraints (<see
        /// cref="Configurations.Users.AccountPermissionConfiguration"/>), a bad <paramref name="permissionId"/>/
        /// <paramref name="permissionDisposition"/>/<paramref name="userId"/> would throw a <see
        /// cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> on <c>SaveChangesAsync</c> rather than silently
        /// falling out of an <c>INNER JOIN</c>, so - consistent with <see cref="InsertRoleMemberAndBuildResultAsync"/>'s
        /// reasoning - this pre-checks Permission/PermissionDisposition/Profile existence first and returns <see
        /// langword="null"/> instead of letting that exception propagate.
        /// </summary>
        public async Task<TwInsertAccountPermissionResult?> InsertAccountPermission(Guid userId, int permissionId, string permissionDisposition, string? ns, string? pageId)
        {
            if (!int.TryParse(permissionDisposition, out var permissionDispositionId))
            {
                return null;
            }

            using var context = _createContext();

            var permissionName = await context.Permissions.Where(p => p.Id == permissionId).Select(p => p.Name).FirstOrDefaultAsync();
            if (permissionName == null)
            {
                return null;
            }

            var dispositionName = await context.PermissionDispositions.Where(pd => pd.Id == permissionDispositionId).Select(pd => pd.Name).FirstOrDefaultAsync();
            if (dispositionName == null)
            {
                return null;
            }

            if (!await context.Profiles.AnyAsync(p => p.UserId == userId))
            {
                return null;
            }

            var accountPermission = new UsersEntities.AccountPermission
            {
                UserId = userId,
                PermissionId = permissionId,
                Namespace = ns,
                PageId = pageId,
                PermissionDispositionId = permissionDispositionId,
            };

            context.AccountPermissions.Add(accountPermission);
            await context.SaveChangesAsync();

            return new TwInsertAccountPermissionResult
            {
                Id = accountPermission.Id,
                Permission = permissionName,
                PermissionDisposition = dispositionName,
                Namespace = ns,
                PageId = pageId,
                ResourceName = await ResolveResourceNameAsync(context, ns, pageId),
            };
        }

        /// <summary>
        /// Mirrors IsRolePermissionDefined.sql: same "exact or broader existing scope already covers the request"
        /// semantics as <see cref="IsAccountPermissionDefined"/>, applied to Users.RolePermission instead of
        /// Users.AccountPermission - see that member's remarks for the full three-valued-logic derivation and the
        /// <paramref name="permissionDispositionId"/> string-to-int handling.
        /// </summary>
        public async Task<bool> IsRolePermissionDefined(int roleId, int permissionId, string permissionDispositionId, string? ns, string? pageId, bool forceReCache = false)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security, [roleId, permissionId, permissionDispositionId, ns, pageId]);

            return await MemCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
            {
                if (!int.TryParse(permissionDispositionId, out var dispositionId))
                {
                    return false;
                }

                using var context = _createContext();
                return await context.RolePermissions.AnyAsync(rp =>
                    rp.RoleId == roleId &&
                    rp.PermissionId == permissionId &&
                    rp.PermissionDispositionId == dispositionId &&
                    (rp.Namespace == null || rp.Namespace == ns) &&
                    (rp.PageId == null || rp.PageId == pageId));
            });
        }

        /// <summary>
        /// Mirrors AutoCompleteRole.sql: Users.Role rows whose <see cref="UsersEntities.Role.Name"/> contains
        /// <paramref name="searchText"/> (an empty string, matching everything, if null - same as the reference's
        /// <c>searchText ?? string.Empty</c>), ordered by Name, capped at 25 rows. No caching, matching the SQLite
        /// reference. <see cref="TwRole.Description"/>/<see cref="TwRole.IsBuiltIn"/> are deliberately left unset
        /// here, same as the reference script's own column list (it selects only Id/Name) - this is the same
        /// "reference selects fewer columns than the target model has properties for" idiom already documented on
        /// <see cref="EfPageRepository.GetTopRecentlyModifiedPagesInfoByUserId"/>, with no observable behavioral
        /// impact since the only consumer of this member is an autocomplete dropdown that only reads Name.
        /// </summary>
        public async Task<List<TwRole>> AutoCompleteRole(string? searchText)
        {
            using var context = _createContext();
            var text = searchText ?? string.Empty;

            return await context.Roles
                .Where(r => r.Name.Contains(text))
                .OrderBy(r => r.Name)
                .Take(25)
                .Select(r => new TwRole
                {
                    Id = r.Id,
                    Name = r.Name,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors AutoCompleteAccount.sql: Users.Profile rows with a matching AspNetUsers row (the reference's
        /// own <c>INNER JOIN AspNetUsers</c> - a Profile with no matching Identity user is excluded entirely)
        /// whose AccountName or Email contains <paramref name="searchText"/> (an empty string, matching
        /// everything, if null - same as the reference's own <c>searchText ?? string.Empty</c>), ordered by
        /// AccountName, capped at 25 rows. Only Email is pulled from the separate <see cref="ApplicationDbContext"/>
        /// - unlike <see cref="GetAllAccountUserRowsAsync"/> (shared by <see cref="GetAllUsers"/>/<see
        /// cref="GetAllUsersPaged"/>/<see cref="GetAllPublicProfilesPaged"/>), this deliberately skips fetching
        /// claims, since this runs once per autocomplete keystroke and the reference script's own column list
        /// (UserId/AccountName/EmailAddress) never needed them either. No caching, matching the SQLite reference.
        /// Every other <see cref="TwAccountProfile"/> field is deliberately left unset here, same as the
        /// reference script's own column list - the same "reference selects fewer columns than the target model
        /// has properties for" idiom as <see cref="AutoCompleteRole"/>.
        /// </summary>
        public async Task<List<TwAccountProfile>> AutoCompleteAccount(string? searchText)
        {
            var text = searchText ?? string.Empty;

            using var context = _createContext();
            var profiles = await context.Profiles
                .Select(p => new { p.UserId, AccountName = p.AccountName ?? string.Empty })
                .ToListAsync();

            using var identityContext = _createIdentityContext();
            var identityUserIds = profiles.Select(p => p.UserId.ToString()).ToList();

            var emailsByUserId = await identityContext.Users
                .Where(u => identityUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email })
                .ToDictionaryAsync(u => u.Id, u => u.Email, StringComparer.OrdinalIgnoreCase);

            return profiles
                .Where(p => emailsByUserId.ContainsKey(p.UserId.ToString()))
                .Select(p => new { p.UserId, p.AccountName, Email = emailsByUserId[p.UserId.ToString()] })
                .Where(p => (p.Email?.Contains(text, StringComparison.OrdinalIgnoreCase) ?? false)
                    || p.AccountName.Contains(text, StringComparison.OrdinalIgnoreCase))
                .OrderBy(p => p.AccountName, StringComparer.Ordinal)
                .Take(25)
                .Select(p => new TwAccountProfile
                {
                    UserId = p.UserId,
                    AccountName = p.AccountName,
                    EmailAddress = p.Email ?? string.Empty,
                })
                .ToList();
        }

        /// <summary>
        /// Mirrors AddRoleMemberByName.sql: resolves <paramref name="roleName"/> to a Users.Role.Id, then delegates
        /// to the same insert-and-build-result logic as <see cref="AddRoleMember"/> (see
        /// <see cref="InsertRoleMemberAndBuildResultAsync"/>'s remarks for how a missing role/profile/identity row
        /// is handled). Not reproduced: the reference resolves the role name via an uncorrelated
        /// <c>(SELECT R.Id FROM Role R WHERE R.Name = @RoleName)</c> subquery directly inside the <c>INSERT</c>
        /// statement - if no role matches, that subquery evaluates to <see langword="NULL"/>, which the reference's
        /// real <c>RoleId INTEGER NOT NULL</c> column constraint then turns into a thrown "NOT NULL constraint
        /// failed" exception (confirmed by inspection of Scripts/Initialization/Versions/2.26.0/^004^Users^AccountRole.sql -
        /// there is no application-level guard against this anywhere in the reference). This resolves the role
        /// name first and returns <see langword="null"/> instead of letting an unhandled constraint-violation
        /// exception propagate, consistent with the "returns the result, or null if the operation failed" contract
        /// already documented on this interface member and with how <see cref="InsertRoleMemberAndBuildResultAsync"/>
        /// already handles every other "the row this insert would reference does not exist" case.
        /// </summary>
        public async Task<TwAddRoleMemberResult?> AddRoleMemberByname(Guid userId, string roleName)
            => await InsertRoleMemberAndBuildResultAsync(userId, roleId: null, roleName: roleName);

        /// <summary>
        /// Mirrors AddRoleMember.sql: inserts a new Users.AccountRole row linking <paramref name="userId"/> to
        /// <paramref name="roleId"/>, then returns the newly-added member's profile/identity info. See
        /// <see cref="InsertRoleMemberAndBuildResultAsync"/>'s remarks for the full behavior, including how a
        /// missing role/profile/identity row is handled.
        /// </summary>
        public async Task<TwAddRoleMemberResult?> AddRoleMember(Guid userId, int roleId)
            => await InsertRoleMemberAndBuildResultAsync(userId, roleId, roleName: null);

        /// <summary>
        /// Shared implementation behind <see cref="AddRoleMember"/>/<see cref="AddRoleMemberByname"/>: resolves
        /// <paramref name="roleId"/> directly, or <paramref name="roleName"/> to a Users.Role.Id (exactly one of
        /// the two is supplied by the two public callers), inserts the Users.AccountRole row, then builds the
        /// result by reading Users.Profile (same <see cref="TightWikiDbContext"/>) plus Email/EmailConfirmed and
        /// the "firstname"/"lastname" AspNetUserClaims claims (a second, separate <see cref="ApplicationDbContext"/>
        /// - Identity tables cannot be reached via a LINQ join from <see cref="TightWikiDbContext"/>, see
        /// <see cref="EfConfigurationRepository.GetWikiDatabaseMetrics"/>'s doc comment for the same two-context
        /// split applied to a simpler case). Returns <see langword="null"/> - rather than letting a real foreign
        /// key violation propagate as an unhandled <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> -
        /// when the role or the user's Users.Profile row does not exist, or (mirroring the reference's own
        /// <c>INNER JOIN AspNetUsers</c>) when no matching Identity user is found for <paramref name="userId"/>.
        /// This is a deliberate divergence from the SQLite reference, which has no such guard and would either
        /// throw (missing role, a real <c>NOT NULL</c> violation both here and there) or - if SQLite foreign key
        /// enforcement happens to be off, unlike the consolidated schema's real, always-enforced FK constraints,
        /// see <see cref="Configurations.Users.AccountRoleConfiguration"/> - silently insert an orphaned row and
        /// then return <see langword="null"/> anyway once the follow-up <c>SELECT</c>'s <c>INNER JOIN</c> to
        /// Profile/AspNetUsers fails to match it; pre-checking here reaches the same "null on a bad foreign key"
        /// outcome deterministically, regardless of provider or pragma state.
        /// </summary>
        private async Task<TwAddRoleMemberResult?> InsertRoleMemberAndBuildResultAsync(Guid userId, int? roleId, string? roleName)
        {
            using var context = _createContext();

            int resolvedRoleId;
            if (roleId.HasValue)
            {
                resolvedRoleId = roleId.Value;
                if (!await context.Roles.AnyAsync(r => r.Id == resolvedRoleId))
                {
                    return null;
                }
            }
            else
            {
                var foundRoleId = await context.Roles.Where(r => r.Name == roleName).Select(r => (int?)r.Id).FirstOrDefaultAsync();
                if (foundRoleId == null)
                {
                    return null;
                }
                resolvedRoleId = foundRoleId.Value;
            }

            var profile = await context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
            if (profile == null)
            {
                return null;
            }

            var accountRole = new UsersEntities.AccountRole { UserId = userId, RoleId = resolvedRoleId };
            context.AccountRoles.Add(accountRole);
            await context.SaveChangesAsync();

            using var identityContext = _createIdentityContext();
            var identityUserId = userId.ToString();

            var identity = await identityContext.Users
                .Where(u => u.Id == identityUserId)
                .Select(u => new { u.Email })
                .FirstOrDefaultAsync();

            if (identity == null)
            {
                return null;
            }

            var firstName = await identityContext.UserClaims
                .Where(c => c.UserId == identityUserId && c.ClaimType == "firstname")
                .Select(c => c.ClaimValue)
                .FirstOrDefaultAsync();

            var lastName = await identityContext.UserClaims
                .Where(c => c.UserId == identityUserId && c.ClaimType == "lastname")
                .Select(c => c.ClaimValue)
                .FirstOrDefaultAsync();

            return new TwAddRoleMemberResult
            {
                Id = accountRole.Id,
                UserId = userId,
                Navigation = profile.Navigation ?? string.Empty,
                AccountName = profile.AccountName ?? string.Empty,
                EmailAddress = identity.Email ?? string.Empty,
                FirstName = firstName ?? string.Empty,
                LastName = lastName ?? string.Empty,
            };
        }

        /// <summary>
        /// Mirrors AddAccountMembership.sql: inserts a new Users.AccountRole row linking <paramref name="userId"/>
        /// to <paramref name="roleId"/>, then returns its generated Id together with the role's Name - unlike
        /// <see cref="AddRoleMember"/>/<see cref="AddRoleMemberByname"/>, the reference's own follow-up
        /// <c>SELECT</c> only joins Users.Role (not Profile/AspNetUsers), so no cross-<see cref="ApplicationDbContext"/>
        /// lookup is needed here. Returns <see langword="null"/> when the role or the user's Users.Profile row does
        /// not exist - see <see cref="InsertRoleMemberAndBuildResultAsync"/>'s remarks for why this pre-checks
        /// rather than letting a real foreign key violation propagate (the same reasoning applies here even though
        /// the reference's own follow-up <c>SELECT</c> for this particular script does not itself re-check
        /// Profile/AspNetUsers).
        /// </summary>
        public async Task<TwAddAccountMembershipResult?> AddAccountMembership(Guid userId, int roleId)
        {
            using var context = _createContext();

            var role = await context.Roles.FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null)
            {
                return null;
            }

            if (!await context.Profiles.AnyAsync(p => p.UserId == userId))
            {
                return null;
            }

            var accountRole = new UsersEntities.AccountRole { UserId = userId, RoleId = roleId };
            context.AccountRoles.Add(accountRole);
            await context.SaveChangesAsync();

            return new TwAddAccountMembershipResult
            {
                Id = accountRole.Id,
                Name = role.Name,
            };
        }

        /// <summary>
        /// Mirrors RemoveRoleMember.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - fully provider-portable,
        /// no raw SQL needed, same idiom used throughout <see cref="EfPageRepository"/>.
        /// </summary>
        public async Task RemoveRoleMember(int roleId, Guid userId)
        {
            using var context = _createContext();
            await context.AccountRoles.Where(ar => ar.RoleId == roleId && ar.UserId == userId).ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors RemoveRolePermission.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - same idiom as
        /// <see cref="RemoveRoleMember"/>.
        /// </summary>
        public async Task RemoveRolePermission(int id)
        {
            using var context = _createContext();
            await context.RolePermissions.Where(rp => rp.Id == id).ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors RemoveAccountPermission.sql via EF Core's LINQ bulk <c>ExecuteDeleteAsync</c> - same idiom as
        /// <see cref="RemoveRoleMember"/>.
        /// </summary>
        public async Task RemoveAccountPermission(int id)
        {
            using var context = _createContext();
            await context.AccountPermissions.Where(ap => ap.Id == id).ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors InsertRolePermission.sql: inserts a new Users.RolePermission row for <paramref name="roleId"/>,
        /// then returns it joined back to its Permission/PermissionDisposition names and a computed <see
        /// cref="TwInsertRolePermissionResult.ResourceName"/> - structurally identical to <see
        /// cref="InsertAccountPermission"/> (see that member's remarks for the FK pre-check reasoning and <see
        /// cref="ResolveResourceNameAsync"/>'s remarks for the resource-name precedence), except the pre-checked
        /// foreign key is Users.Role rather than Users.Profile.
        /// </summary>
        public async Task<TwInsertRolePermissionResult?> InsertRolePermission(int roleId, int permissionId, string permissionDisposition, string? ns, string? pageId)
        {
            if (!int.TryParse(permissionDisposition, out var permissionDispositionId))
            {
                return null;
            }

            using var context = _createContext();

            var permissionName = await context.Permissions.Where(p => p.Id == permissionId).Select(p => p.Name).FirstOrDefaultAsync();
            if (permissionName == null)
            {
                return null;
            }

            var dispositionName = await context.PermissionDispositions.Where(pd => pd.Id == permissionDispositionId).Select(pd => pd.Name).FirstOrDefaultAsync();
            if (dispositionName == null)
            {
                return null;
            }

            if (!await context.Roles.AnyAsync(r => r.Id == roleId))
            {
                return null;
            }

            var rolePermission = new UsersEntities.RolePermission
            {
                RoleId = roleId,
                PermissionId = permissionId,
                Namespace = ns,
                PageId = pageId,
                PermissionDispositionId = permissionDispositionId,
            };

            context.RolePermissions.Add(rolePermission);
            await context.SaveChangesAsync();

            return new TwInsertRolePermissionResult
            {
                Id = rolePermission.Id,
                Permission = permissionName,
                PermissionDisposition = dispositionName,
                Namespace = ns,
                PageId = pageId,
                ResourceName = await ResolveResourceNameAsync(context, ns, pageId),
            };
        }

        /// <summary>
        /// Shared by <see cref="InsertAccountPermission"/>/<see cref="InsertRolePermission"/>/<see
        /// cref="ComputeResourceName"/>: mirrors the reference scripts' repeated <c>CASE WHEN Namespace IS NOT NULL
        /// THEN Namespace WHEN PageId IS NOT NULL THEN CASE WHEN PageId = '*' THEN '*' ELSE PG.Name END END</c>
        /// expression - namespace scope wins if set, otherwise the literal <c>'*'</c> wildcard if <paramref
        /// name="pageId"/> is that literal string, otherwise the name of the Pages.Page whose Id matches <paramref
        /// name="pageId"/> parsed as an integer (or <see langword="null"/> if it does not parse, or no such page
        /// exists - the same "left outer join finds nothing" outcome as the reference's own <c>LEFT OUTER JOIN
        /// pages_db.Page</c>), otherwise <see langword="null"/> if neither <paramref name="ns"/> nor <paramref
        /// name="pageId"/> is set.
        /// </summary>
        private static async Task<string?> ResolveResourceNameAsync(TightWikiDbContext context, string? ns, string? pageId)
        {
            if (ns != null)
            {
                return ns;
            }
            if (pageId == null)
            {
                return null;
            }
            if (pageId == "*")
            {
                return "*";
            }
            if (!int.TryParse(pageId, out var parsedPageId))
            {
                return null;
            }
            return await context.Pages_Pages.Where(p => p.Id == parsedPageId).Select(p => p.Name).FirstOrDefaultAsync();
        }

        /// <summary>
        /// In-memory equivalent of <see cref="ResolveResourceNameAsync"/>, used by <see cref="GetRolePermissionsPaged"/>/
        /// <see cref="GetAccountPermissionsPaged"/> against a pre-fetched <paramref name="pageNamesById"/> lookup
        /// (built once for the whole result set, see those members' remarks) rather than issuing one query per row.
        /// </summary>
        private static string? ComputeResourceName(string? ns, string? pageId, IReadOnlyDictionary<int, string> pageNamesById)
        {
            if (ns != null)
            {
                return ns;
            }
            if (pageId == null)
            {
                return null;
            }
            if (pageId == "*")
            {
                return "*";
            }
            if (!int.TryParse(pageId, out var parsedPageId))
            {
                return null;
            }
            return pageNamesById.TryGetValue(parsedPageId, out var name) ? name : null;
        }

        /// <summary>
        /// Same resource-scope precedence as <see cref="ComputeResourceName"/>, but returning the <c>'N-'</c>/
        /// <c>'P-'</c>-prefixed sort key the reference scripts' own <c>--CONFIG::</c> "Resource" mapping computes
        /// (<c>CASE WHEN Namespace IS NOT NULL THEN 'N-' || Namespace WHEN PageId IS NOT NULL THEN CASE WHEN PageId
        /// = '*' THEN 'P-' || '*' ELSE 'P-' || PG.Name END END</c>) - the prefix keeps namespace-scoped and
        /// page-scoped rows from interleaving when sorted by resource name, exactly as in the reference.
        /// </summary>
        private static string? ComputeResourceSortKey(string? ns, string? pageId, IReadOnlyDictionary<int, string> pageNamesById)
        {
            if (ns != null)
            {
                return "N-" + ns;
            }
            if (pageId == null)
            {
                return null;
            }
            if (pageId == "*")
            {
                return "P-*";
            }
            if (!int.TryParse(pageId, out var parsedPageId))
            {
                return null;
            }
            return pageNamesById.TryGetValue(parsedPageId, out var name) ? "P-" + name : null;
        }

        /// <summary>
        /// Mirrors GetApparentAccountPermissions.sql: the union of every Users.RolePermission row for every role
        /// <paramref name="userId"/> is a member of, with every direct Users.AccountPermission row for that same
        /// user - the "effective"/apparent permission set a user actually has, before namespace/page-scope
        /// resolution happens elsewhere in the caller. Translated as a single provider-translatable LINQ <see
        /// cref="Queryable.Union{TSource}(IQueryable{TSource}, IEnumerable{TSource})"/> of the two shaped
        /// projections (SQL <c>UNION</c>, same operator and de-duplication semantics as the reference script's own
        /// <c>UNION</c> - not <c>UNION ALL</c>) rather than materializing both sides and de-duplicating in memory.
        /// Cached under <see cref="MemCache.Category.Security"/>, same cache key shape as the reference. Not
        /// reproduced: the reference caches via <c>MemCache.AddOrGet</c> (the non-async overload) around an
        /// <see langword="async"/> lambda, which ends up caching the still-running <see cref="Task{TResult}"/>
        /// itself rather than its resolved result - functionally harmless (an already-completed <see
        /// cref="Task{TResult}"/> can be awaited repeatedly with the same result) but not a pattern worth
        /// reproducing; this uses the async-correct <see cref="MemCache.AddOrGetAsync{T}(ITwCacheKey, MemCache.GetValueDelegateAsync{T}, TimeSpan?)"/>
        /// overload instead, same as <see cref="GetApparentRolePermissions(string)"/> already does.
        /// </summary>
        public async Task<List<TwApparentPermission>> GetApparentAccountPermissions(Guid userId)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security, [userId]);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                var rolePermissions =
                    from rp in context.RolePermissions
                    join ar in context.AccountRoles on rp.RoleId equals ar.RoleId
                    where ar.UserId == userId
                    select new TwApparentPermission
                    {
                        Permission = rp.Permission.Name,
                        PermissionDisposition = rp.PermissionDisposition.Name,
                        Namespace = rp.Namespace,
                        PageId = rp.PageId,
                    };

                // Note: the reference script inner-joins this branch against Profile (Profile.UserId = AP.UserId)
                // as well, but that join is a no-op here: AccountPermissionConfiguration declares AccountPermission.
                // UserId as IsRequired() with a real FK onto Profile.UserId, so no AccountPermission row can exist
                // in this schema without a matching Profile row - the join can never filter anything out.
                var accountPermissions =
                    from ap in context.AccountPermissions
                    where ap.UserId == userId
                    select new TwApparentPermission
                    {
                        Permission = ap.Permission.Name,
                        PermissionDisposition = ap.PermissionDisposition.Name,
                        Namespace = ap.Namespace,
                        PageId = ap.PageId,
                    };

                return await rolePermissions.Union(accountPermissions).ToListAsync();
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetApparentRolePermissions.sql via <see cref="TwRoles.ToString"/> resolving to the role's Name -
        /// same delegation as the SQLite reference's own C# overload.
        /// </summary>
        public async Task<List<TwApparentPermission>> GetApparentRolePermissions(TwRoles role)
            => await GetApparentRolePermissions(role.ToString());

        /// <summary>
        /// Mirrors GetApparentRolePermissions.sql: every distinct Permission/PermissionDisposition/Namespace/PageId
        /// combination assigned directly to the Users.Role named <paramref name="roleName"/> (no membership join -
        /// unlike <see cref="GetApparentAccountPermissions"/>, this is scoped to one role's own grants, not a
        /// user's inherited set). <c>Distinct()</c> is translated server-side to SQL <c>SELECT DISTINCT</c>, same
        /// as the reference script's own <c>SELECT DISTINCT</c>. Cached under <see cref="MemCache.Category.Security"/>,
        /// same cache key shape as the reference.
        /// </summary>
        public async Task<List<TwApparentPermission>> GetApparentRolePermissions(string roleName)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security, [roleName]);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                return await context.RolePermissions
                    .Where(rp => rp.Role.Name == roleName)
                    .Select(rp => new TwApparentPermission
                    {
                        Permission = rp.Permission.Name,
                        PermissionDisposition = rp.PermissionDisposition.Name,
                        Namespace = rp.Namespace,
                        PageId = rp.PageId,
                    })
                    .Distinct()
                    .ToListAsync();
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetAllPermissionDispositions.sql: every Users.PermissionDisposition row (the fixed "Allow"/"Deny"
        /// set), ordered by Name. Cached under <see cref="MemCache.Category.Security"/>, same cache key shape
        /// (no extra segments) as the reference.
        /// </summary>
        public async Task<List<TightWiki.Plugin.Models.TwPermissionDisposition>> GetAllPermissionDispositions()
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                return await context.PermissionDispositions
                    .OrderBy(pd => pd.Name)
                    .Select(pd => new TightWiki.Plugin.Models.TwPermissionDisposition
                    {
                        Id = pd.Id,
                        Name = pd.Name,
                    })
                    .ToListAsync();
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetAllPermissions.sql: every Users.Permission row (the fixed "Read"/"Edit"/"Delete"/"Moderate"/
        /// "Create" set), ordered by Name. Cached under <see cref="MemCache.Category.Security"/>, same cache key
        /// shape (no extra segments) as the reference.
        /// </summary>
        public async Task<List<TightWiki.Plugin.Models.TwPermission>> GetAllPermissions()
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Security);

            return (await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                return await context.Permissions
                    .OrderBy(p => p.Name)
                    .Select(p => new TightWiki.Plugin.Models.TwPermission
                    {
                        Id = p.Id,
                        Name = p.Name,
                    })
                    .ToListAsync();
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetRolePermissionsPaged.sql: every Users.RolePermission row for <paramref name="roleId"/>, each
        /// joined to its Permission/PermissionDisposition names and a computed ResourceName (see <see
        /// cref="ComputeResourceName"/>). <see cref="TwRolePermission.PaginationPageSize"/>/<see
        /// cref="TwRolePermission.PaginationPageCount"/> are computed from <paramref name="pageSize"/> if supplied,
        /// else the "Pagination Size" customization setting - same <c>pageSize ??= ...</c> precedence as the
        /// reference's own C# wrapper (contrast <see cref="GetRoleMembersPaged"/>, which always ignores its
        /// <paramref name="pageSize"/> argument). Because ordering by "Resource" needs the same namespace/page-id
        /// precedence and Pages.Page.Name lookup as <see cref="ComputeResourceName"/> (and the reference script's
        /// own <c>--CONFIG::</c> "Resource" mapping computes an <c>'N-'</c>/<c>'P-'</c>-prefixed sort key over
        /// exactly that same LEFT OUTER JOIN, see <see cref="ComputeResourceSortKey"/>), the (typically small) set
        /// of a role's permissions is pulled into memory first - same "paginate/sort in memory" approach as <see
        /// cref="GetRoleMembersPaged"/>, but here because of the computed sort key rather than a second
        /// <see cref="ApplicationDbContext"/>. Default ordering mirrors the script's own un-transposed <c>ORDER BY
        /// P.Name, PD.Name</c> (both ascending); an unrecognized <paramref name="orderBy"/> throws, same pattern as
        /// <see cref="GetAllRoles"/>. String comparisons/sorts use <see cref="StringComparer.Ordinal"/>, same
        /// reasoning as <see cref="GetRoleMembersPaged"/>.
        /// </summary>
        public async Task<List<TwRolePermission>> GetRolePermissionsPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");
            var effectivePageSize = pageSize.Value;

            using var context = _createContext();

            var rows = await context.RolePermissions
                .Where(rp => rp.RoleId == roleId)
                .Select(rp => new
                {
                    rp.Id,
                    Permission = rp.Permission.Name,
                    PermissionDisposition = rp.PermissionDisposition.Name,
                    rp.Namespace,
                    rp.PageId,
                })
                .ToListAsync();

            var pageNamesById = await ResolvePageNamesAsync(context, rows.Select(r => (r.Namespace, r.PageId)));

            var totalCount = rows.Count;
            var paginationPageCount = (totalCount + (effectivePageSize - 1)) / effectivePageSize;

            var permissions = rows.Select(r => new TwRolePermission
            {
                Id = r.Id,
                Permission = r.Permission,
                PermissionDisposition = r.PermissionDisposition,
                Namespace = r.Namespace,
                PageId = r.PageId,
                ResourceName = ComputeResourceName(r.Namespace, r.PageId, pageNamesById),
                PaginationPageSize = effectivePageSize,
                PaginationPageCount = paginationPageCount,
            }).ToList();

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            IOrderedEnumerable<TwRolePermission> ordered = string.IsNullOrEmpty(orderBy)
                ? permissions.OrderBy(x => x.Permission, StringComparer.Ordinal).ThenBy(x => x.PermissionDisposition, StringComparer.Ordinal)
                : orderBy.ToUpperInvariant() switch
                {
                    "PERMISSION" => ascending
                        ? permissions.OrderBy(x => x.Permission, StringComparer.Ordinal)
                        : permissions.OrderByDescending(x => x.Permission, StringComparer.Ordinal),
                    "DISPOSITION" => ascending
                        ? permissions.OrderBy(x => x.PermissionDisposition, StringComparer.Ordinal)
                        : permissions.OrderByDescending(x => x.PermissionDisposition, StringComparer.Ordinal),
                    "RESOURCE" => ascending
                        ? permissions.OrderBy(x => ComputeResourceSortKey(x.Namespace, x.PageId, pageNamesById), StringComparer.Ordinal)
                        : permissions.OrderByDescending(x => ComputeResourceSortKey(x.Namespace, x.PageId, pageNamesById), StringComparer.Ordinal),
                    _ => throw new InvalidOperationException($"No order by mapping was found in 'GetRolePermissionsPaged.sql' for the field '{orderBy}'."),
                };

            return ordered
                .Skip((pageNumber - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();
        }

        /// <summary>
        /// Shared by <see cref="GetRolePermissionsPaged"/>/<see cref="GetAccountPermissionsPaged"/>: batch-resolves
        /// Pages.Page.Name for every namespace-less, non-wildcard, numeric <c>PageId</c> among <paramref
        /// name="scopes"/> in a single query (<c>Contains(...)</c>-based - same "temp-table join replaced by a
        /// single <c>Contains</c> query" idiom as <see cref="EfPageRepository"/>'s <c>TempPageIds</c> pattern, see
        /// that project's remarks), rather than one query per row.
        /// </summary>
        private static async Task<Dictionary<int, string>> ResolvePageNamesAsync(TightWikiDbContext context, IEnumerable<(string? Namespace, string? PageId)> scopes)
        {
            var pageIds = scopes
                .Where(s => s.Namespace == null && s.PageId != null && s.PageId != "*")
                .Select(s => int.TryParse(s.PageId, out var parsed) ? (int?)parsed : null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .Distinct()
                .ToList();

            if (pageIds.Count == 0)
            {
                return [];
            }

            return await context.Pages_Pages
                .Where(p => pageIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);
        }

        /// <summary>
        /// One row of the shared result set fetched by <see cref="GetAllAccountUserRowsAsync"/>.
        /// </summary>
        private sealed record AccountUserRow(
            Guid UserId,
            string AccountName,
            string Navigation,
            DateTime CreatedDate,
            DateTime ModifiedDate,
            string? Email,
            bool EmailConfirmed,
            string? FirstName,
            string? LastName,
            string? TimeZone,
            string? Language,
            string? Country);

        /// <summary>
        /// Shared by <see cref="GetAllUsers"/>/<see cref="GetAllUsersPaged"/>/<see cref="GetAllPublicProfilesPaged"/>:
        /// every Users.Profile row that has a matching AspNetUsers row (INNER JOIN semantics, mirroring all three
        /// reference scripts' own <c>FROM Profile ... INNER JOIN AspNetUsers</c> - a Profile with no matching
        /// Identity user is excluded entirely, not surfaced with blank identity fields), combined with
        /// Email/EmailConfirmed and the "firstname"/"lastname"/"timezone"/"language"/"*/country" AspNetUserClaims
        /// claims from the separate <see cref="ApplicationDbContext"/> - same two-context split and LEFT-OUTER-
        /// JOIN-shaped claim lookups as <see cref="GetRoleMembersPaged"/> (see that member's remarks), just
        /// unfiltered/unpaged/unsorted here - each of the three callers applies its own filter/sort/paging over
        /// this shared result set in memory.
        /// </summary>
        private async Task<List<AccountUserRow>> GetAllAccountUserRowsAsync()
        {
            using var context = _createContext();
            var profiles = await context.Profiles
                .Select(p => new { p.UserId, p.AccountName, p.Navigation, p.CreatedDate, p.ModifiedDate })
                .ToListAsync();

            using var identityContext = _createIdentityContext();
            var identityUserIds = profiles.Select(p => p.UserId.ToString()).ToList();

            var identities = await identityContext.Users
                .Where(u => identityUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.EmailConfirmed })
                .ToDictionaryAsync(u => u.Id, StringComparer.OrdinalIgnoreCase);

            var relevantClaimTypes = new[] { "firstname", "lastname", "timezone", "language" };
            var claims = await identityContext.UserClaims
                .Where(c => identityUserIds.Contains(c.UserId) && c.ClaimType != null
                    && (relevantClaimTypes.Contains(c.ClaimType) || c.ClaimType.EndsWith("/country")))
                .Select(c => new { c.UserId, c.ClaimType, c.ClaimValue })
                .ToListAsync();

            var claimsByUser = claims
                .GroupBy(c => c.UserId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            string? GetClaimValue(string identityUserId, Func<string, bool> claimTypeMatch)
                => claimsByUser.TryGetValue(identityUserId, out var userClaims)
                    ? userClaims.FirstOrDefault(c => c.ClaimType != null && claimTypeMatch(c.ClaimType))?.ClaimValue
                    : null;

            return profiles
                .Where(p => identities.ContainsKey(p.UserId.ToString()))
                .Select(p =>
                {
                    var identityUserId = p.UserId.ToString();
                    var identity = identities[identityUserId];

                    return new AccountUserRow(
                        p.UserId,
                        p.AccountName ?? string.Empty,
                        p.Navigation ?? string.Empty,
                        p.CreatedDate,
                        p.ModifiedDate,
                        identity.Email,
                        identity.EmailConfirmed,
                        GetClaimValue(identityUserId, t => t == "firstname"),
                        GetClaimValue(identityUserId, t => t == "lastname"),
                        GetClaimValue(identityUserId, t => t == "timezone"),
                        GetClaimValue(identityUserId, t => t == "language"),
                        GetClaimValue(identityUserId, t => t.EndsWith("/country", StringComparison.Ordinal)));
                })
                .ToList();
        }

        /// <summary>
        /// Mirrors GetAllPublicProfilesPaged.sql: like <see cref="GetAllUsersPaged"/> but exposes only
        /// non-personal fields (no EmailAddress/FirstName/LastName/EmailConfirmed in the projection, even though
        /// Email is still used for <paramref name="searchToken"/> filtering, same as the reference script's own
        /// comment "exactly like GetAllUsersPaged except it has no filter on personal information"). No
        /// <paramref name="orderBy"/> parameter exists on this member (unlike <see cref="GetAllUsersPaged"/>) -
        /// mirroring the reference script, which has no <c>--CUSTOM_ORDER_BEGIN::</c> section and always orders by
        /// AccountName then UserId ascending.
        /// </summary>
        /// <remarks>
        /// <b>Deliberate divergence from the reference's PaginationPageCount computation:</b>
        /// GetAllPublicProfilesPaged.sql's inner <c>PaginationPageCount</c> subquery aliases its own <c>FROM</c>
        /// clause as <c>Profile AS P</c>, but its <c>JOIN</c>/<c>WHERE</c> clauses only ever reference the
        /// *outer* query's <c>U</c>/<c>ANU</c> aliases, never <c>P</c> - i.e. it is an accidentally correlated
        /// subquery whose filter condition evaluates to the same constant (true or false) for every row of
        /// <c>P</c>, for a given outer row. Since every row the outer query actually returns already satisfies
        /// that same condition (it is copy-pasted from the outer <c>WHERE</c>), the subquery's <c>WHERE</c> is
        /// always true when it runs, so it always counts the *entire, unfiltered* Profile table - <paramref
        /// name="searchToken"/> has no effect whatsoever on the reported page count. Confirmed by contrast with
        /// GetAllUsersPaged.sql's structurally near-identical subquery, which instead re-aliases its own local
        /// <c>Profile AS U</c>/<c>AspNetUsers AS ANU</c> and so correctly computes the filtered count - this is a
        /// copy-paste bug specific to GetAllPublicProfilesPaged.sql, not an intentional quirk. This computes the
        /// actually-correct filtered count instead (same approach as every other paged member in this class),
        /// rather than reproducing the reference's bug.
        /// </remarks>
        public async Task<List<TwAccountProfile>> GetAllPublicProfilesPaged(int pageNumber, int? pageSize = null, string? searchToken = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");
            var effectivePageSize = pageSize.Value;

            var rows = await GetAllAccountUserRowsAsync();

            var filtered = string.IsNullOrEmpty(searchToken)
                ? rows
                : rows.Where(r =>
                    r.AccountName.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ||
                    (r.Email?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.FirstName?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.LastName?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

            var paginationPageCount = effectivePageSize == 0 ? 0 : (filtered.Count + (effectivePageSize - 1)) / effectivePageSize;

            return filtered
                .OrderBy(r => r.AccountName, StringComparer.Ordinal)
                .ThenBy(r => r.UserId)
                .Skip((pageNumber - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .Select(r => new TwAccountProfile
                {
                    UserId = r.UserId,
                    AccountName = r.AccountName,
                    Navigation = r.Navigation,
                    TimeZone = r.TimeZone ?? string.Empty,
                    Language = r.Language ?? string.Empty,
                    Country = r.Country ?? string.Empty,
                    CreatedDate = r.CreatedDate,
                    ModifiedDate = r.ModifiedDate,
                    PaginationPageSize = effectivePageSize,
                    PaginationPageCount = paginationPageCount,
                })
                .ToList();
        }

        /// <summary>
        /// Mirrors AnonymizeProfile.sql: overwrites Users.Profile AccountName/Navigation/Biography/ModifiedDate
        /// for <paramref name="userId"/> with GDPR-style anonymized placeholder values and clears Avatar - a
        /// no-op (not an error) if no such Profile row exists, same as the reference's own <c>UPDATE ... WHERE
        /// UserId = @UserId</c> affecting zero rows. The anonymized name is generated by exactly the same C# logic
        /// as the SQLite reference's own <c>UsersRepository.AnonymizeProfile</c> (this is plain C#, not SQL-side):
        /// <c>"DeletedUser_"</c> followed by the current UTC timestamp's default <see cref="DateTime.ToString()"/>
        /// rendering, run through <see cref="Utility.SanitizeAccountName"/> (treating spaces as invalid, in
        /// addition to the usual filesystem-invalid characters) with every resulting underscore then stripped out
        /// entirely (not just de-duplicated). AvatarContentType is deliberately left untouched, same as the
        /// reference script's own column list (it clears Avatar but not AvatarContentType). Not reproduced: the
        /// reference's own <c>UsersRepository.AnonymizeProfile</c> does not clear any <see
        /// cref="MemCache.Category.User"/> cache entry for <paramref name="userId"/> afterward, unlike <see
        /// cref="UpdateProfile"/>/<see cref="UpdateProfileAvatar"/> (both of which do) - kept as-is here to match
        /// the reference's actual, if inconsistent, caching behavior rather than "fixing" an omission nobody asked
        /// about.
        /// </summary>
        public async Task AnonymizeProfile(Guid userId)
        {
            var anonymousName = "DeletedUser_" + Utility.SanitizeAccountName($"{DateTime.UtcNow}", [' ']).Replace("_", "");

            using var context = _createContext();

            await context.Profiles
                .Where(p => p.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.AccountName, anonymousName)
                    .SetProperty(p => p.Navigation, TwNavigation.Clean(anonymousName))
                    .SetProperty(p => p.Biography, "Deleted account.")
                    .SetProperty(p => p.Avatar, (byte[]?)null)
                    .SetProperty(p => p.ModifiedDate, DateTime.UtcNow));
        }

        /// <summary>
        /// Mirrors IsUserMemberOfAdministrators.sql: whether <paramref name="userId"/> has a Users.AccountRole
        /// membership in the role named "Administrator" (<see cref="TwRoles.Administrator"/>). The reference's own
        /// <c>INNER JOIN Profile</c> is a no-op under the consolidated schema's real FK constraints - same
        /// reasoning as <see cref="GetApparentAccountPermissions"/>'s remarks on Users.AccountPermission/Profile
        /// (AccountRoleConfiguration declares AccountRole.UserId as required with a real FK onto Profile.UserId,
        /// so no AccountRole row can exist without a matching Profile row). Cached under <see
        /// cref="MemCache.Category.User"/>, same cache key shape ([userId], no forceReCache parameter) as the
        /// reference.
        /// </summary>
        public async Task<bool> IsUserMemberOfAdministrators(Guid userId)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.User, [userId]);

            return await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();
                return await context.AccountRoles.AnyAsync(ar => ar.UserId == userId && ar.Role.Name == TwRoles.Administrator.ToString());
            });
        }

        /// <summary>
        /// Mirrors GetRoleByName.sql: the single Users.Role row matching <paramref name="name"/> exactly (all four
        /// columns - Id/Name/Description/IsBuiltIn - same as the reference's own column list). Uses
        /// <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync{TSource}(IQueryable{TSource})"/>,
        /// matching the reference's <c>QuerySingle&lt;TwRole&gt;</c> (Dapper's "exactly one row or throw", not
        /// "first row or default") - throws if no role or more than one role matches.
        /// </summary>
        public async Task<TwRole> GetRoleByName(string name)
        {
            using var context = _createContext();

            return await context.Roles
                .Where(r => r.Name == name)
                .Select(r => new TwRole
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description ?? string.Empty,
                    IsBuiltIn = r.IsBuiltIn,
                })
                .SingleAsync();
        }

        /// <summary>
        /// Mirrors GetAllRoles.sql: every Users.Role row, ordered by Name ascending by default. Custom ordering
        /// mirrors <c>RepositoryHelpers.TransposeOrderby</c> against the script's <c>--CONFIG::</c> mapping
        /// ("Name"/"Description" - same pattern as <see cref="EfPageRepository.GetPageRevisionsInfoByNavigationPaged"/>);
        /// an unrecognized <paramref name="orderBy"/> throws, same as <c>RepositoryHelpers.TransposeOrderby</c>'s
        /// "No order by mapping..." exception. <see cref="TwRole.IsBuiltIn"/> is deliberately left unset here, same
        /// as the reference script's own column list (it selects only Id/Name/Description, unlike
        /// <see cref="GetRoleByName"/>'s four-column list) - the same "reference selects fewer columns than the
        /// target model has properties for" idiom as <see cref="AutoCompleteRole"/>, with no observable behavioral
        /// impact since the one page that lists roles this way (<c>AdminSecurity/Roles</c>) only ever displays
        /// Name/Description, never IsBuiltIn (that flag is only read off <see cref="GetRoleByName"/>'s result, on
        /// the single-role detail page, to hide the delete button for a built-in role).
        /// </summary>
        public async Task<List<TwRole>> GetAllRoles(string? orderBy = null, string? orderByDirection = null)
        {
            using var context = _createContext();

            var query = context.Roles.AsQueryable();
            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            var ordered = string.IsNullOrEmpty(orderBy)
                ? query.OrderBy(r => r.Name)
                : orderBy.ToUpperInvariant() switch
                {
                    "NAME" => ascending ? query.OrderBy(r => r.Name) : query.OrderByDescending(r => r.Name),
                    "DESCRIPTION" => ascending ? query.OrderBy(r => r.Description) : query.OrderByDescending(r => r.Description),
                    _ => throw new InvalidOperationException($"No order by mapping was found in 'GetAllRoles.sql' for the field '{orderBy}'."),
                };

            return await ordered
                .Select(r => new TwRole
                {
                    Id = r.Id,
                    Name = r.Name,
                    Description = r.Description ?? string.Empty,
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetRoleMembersPaged.sql: every Users.Profile row for a user with a Users.AccountRole membership
        /// in <paramref name="roleId"/>, combined with Email/EmailConfirmed and the "firstname"/"lastname"/
        /// "timezone"/"language"/"*/country" AspNetUserClaims claims from the separate <see cref="ApplicationDbContext"/>
        /// (same two-context split as <see cref="InsertRoleMemberAndBuildResultAsync"/>, but for a whole page of
        /// rows instead of one). Because ordering/paging in the reference can be driven by columns that live in
        /// that second context (EmailAddress/FirstName/LastName - see the script's own <c>--CONFIG::</c> mapping),
        /// a single server-translated LINQ query cannot express this: the (typically small) set of role members is
        /// pulled into memory first, joined to identity data by <see cref="Guid"/>-keyed dictionaries, then
        /// sorted/paged client-side. Default ordering mirrors the script's own un-transposed
        /// <c>ORDER BY U.AccountName, U.UserId</c> (both ascending); an unrecognized <paramref name="orderBy"/>
        /// throws, same pattern as <see cref="GetAllRoles"/>. All string comparisons/sorts here are explicit
        /// <see cref="StringComparer.Ordinal"/>/<see cref="StringComparer.OrdinalIgnoreCase"/> rather than the
        /// default (culture-sensitive) comparer, since none of this runs through the database - the same class of
        /// fix already applied to in-memory string grouping/dedup elsewhere in <see cref="EfPageRepository"/> (see
        /// that class's doc comment on the phase 2b.5 case-insensitive-dedup bug).
        /// </summary>
        /// <remarks>
        /// <see cref="TwAccountProfile.PaginationPageSize"/>/<see cref="TwAccountProfile.PaginationPageCount"/> are
        /// computed from the "Pagination Size" customization setting, <b>not</b> from <paramref name="pageSize"/> -
        /// the reference script's own <c>GetRoleMembersPaged</c> C# wrapper ignores its <c>pageSize</c> parameter
        /// entirely and always reads the configured value, the same already-established idiom used throughout
        /// <see cref="EfPageRepository"/> (e.g. <see cref="EfPageRepository.GetTopViewedPagesInfo"/>) for reference
        /// members with the identical quirk. The reference's own <see cref="TwAccountProfile.PaginationPageCount"/>
        /// subquery additionally inner-joins AspNetUsers when counting; that is not reproduced here (the plain
        /// Profile+AccountRole count is used instead) since a Users.Profile row with no matching Identity user
        /// would itself be a data integrity anomaly with no legitimate path to exist.
        /// </remarks>
        public async Task<List<TwAccountProfile>> GetRoleMembersPaged(int roleId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            var membersQuery = from ar in context.AccountRoles
                                join p in context.Profiles on ar.UserId equals p.UserId
                                where ar.RoleId == roleId
                                select p;

            var totalCount = await membersQuery.CountAsync();
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            var members = await membersQuery.ToListAsync();

            using var identityContext = _createIdentityContext();
            var identityUserIds = members.Select(p => p.UserId.ToString()).ToList();

            var identities = await identityContext.Users
                .Where(u => identityUserIds.Contains(u.Id))
                .Select(u => new { u.Id, u.Email, u.EmailConfirmed })
                .ToDictionaryAsync(u => u.Id, StringComparer.OrdinalIgnoreCase);

            var relevantClaimTypes = new[] { "firstname", "lastname", "timezone", "language" };
            var claims = await identityContext.UserClaims
                .Where(c => identityUserIds.Contains(c.UserId) && c.ClaimType != null
                    && (relevantClaimTypes.Contains(c.ClaimType) || c.ClaimType.EndsWith("/country")))
                .Select(c => new { c.UserId, c.ClaimType, c.ClaimValue })
                .ToListAsync();

            var claimsByUser = claims
                .GroupBy(c => c.UserId, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

            string? GetClaimValue(string identityUserId, Func<string, bool> claimTypeMatch)
                => claimsByUser.TryGetValue(identityUserId, out var userClaims)
                    ? userClaims.FirstOrDefault(c => c.ClaimType != null && claimTypeMatch(c.ClaimType))?.ClaimValue
                    : null;

            var profiles = members.Select(p =>
            {
                var identityUserId = p.UserId.ToString();
                identities.TryGetValue(identityUserId, out var identity);

                return new TwAccountProfile
                {
                    UserId = p.UserId,
                    EmailAddress = identity?.Email ?? string.Empty,
                    AccountName = p.AccountName ?? string.Empty,
                    Navigation = p.Navigation ?? string.Empty,
                    FirstName = GetClaimValue(identityUserId, t => t == "firstname"),
                    LastName = GetClaimValue(identityUserId, t => t == "lastname"),
                    TimeZone = GetClaimValue(identityUserId, t => t == "timezone") ?? string.Empty,
                    Language = GetClaimValue(identityUserId, t => t == "language") ?? string.Empty,
                    Country = GetClaimValue(identityUserId, t => t.EndsWith("/country", StringComparison.Ordinal)) ?? string.Empty,
                    CreatedDate = p.CreatedDate,
                    ModifiedDate = p.ModifiedDate,
                    EmailConfirmed = identity?.EmailConfirmed ?? false,
                    PaginationPageSize = paginationSize,
                    PaginationPageCount = paginationPageCount,
                };
            }).ToList();

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            IOrderedEnumerable<TwAccountProfile> ordered = string.IsNullOrEmpty(orderBy)
                ? profiles.OrderBy(x => x.AccountName, StringComparer.Ordinal).ThenBy(x => x.UserId)
                : orderBy.ToUpperInvariant() switch
                {
                    "EMAILADDRESS" => ascending
                        ? profiles.OrderBy(x => x.EmailAddress, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.EmailAddress, StringComparer.Ordinal),
                    "ACCOUNTNAME" => ascending
                        ? profiles.OrderBy(x => x.AccountName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.AccountName, StringComparer.Ordinal),
                    "LASTNAME" => ascending
                        ? profiles.OrderBy(x => x.LastName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.LastName, StringComparer.Ordinal),
                    "FIRSTNAME" => ascending
                        ? profiles.OrderBy(x => x.FirstName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.FirstName, StringComparer.Ordinal),
                    _ => throw new InvalidOperationException($"No order by mapping was found in 'GetRoleMembersPaged.sql' for the field '{orderBy}'."),
                };

            return ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetAccountPermissionsPaged.sql: same shape and sort/page-in-memory approach as <see
        /// cref="GetRolePermissionsPaged"/> (see that member's remarks), scoped to <paramref name="userId"/>'s
        /// direct Users.AccountPermission rows instead of a role's Users.RolePermission rows.
        /// </summary>
        public async Task<List<TwAccountPermission>> GetAccountPermissionsPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");
            var effectivePageSize = pageSize.Value;

            using var context = _createContext();

            var rows = await context.AccountPermissions
                .Where(ap => ap.UserId == userId)
                .Select(ap => new
                {
                    ap.Id,
                    Permission = ap.Permission.Name,
                    PermissionDisposition = ap.PermissionDisposition.Name,
                    ap.Namespace,
                    ap.PageId,
                })
                .ToListAsync();

            var pageNamesById = await ResolvePageNamesAsync(context, rows.Select(r => (r.Namespace, r.PageId)));

            var totalCount = rows.Count;
            var paginationPageCount = (totalCount + (effectivePageSize - 1)) / effectivePageSize;

            var permissions = rows.Select(r => new TwAccountPermission
            {
                Id = r.Id,
                Permission = r.Permission,
                PermissionDisposition = r.PermissionDisposition,
                Namespace = r.Namespace,
                PageId = r.PageId,
                ResourceName = ComputeResourceName(r.Namespace, r.PageId, pageNamesById),
                PaginationPageSize = effectivePageSize,
                PaginationPageCount = paginationPageCount,
            }).ToList();

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            IOrderedEnumerable<TwAccountPermission> ordered = string.IsNullOrEmpty(orderBy)
                ? permissions.OrderBy(x => x.Permission, StringComparer.Ordinal).ThenBy(x => x.PermissionDisposition, StringComparer.Ordinal)
                : orderBy.ToUpperInvariant() switch
                {
                    "PERMISSION" => ascending
                        ? permissions.OrderBy(x => x.Permission, StringComparer.Ordinal)
                        : permissions.OrderByDescending(x => x.Permission, StringComparer.Ordinal),
                    "DISPOSITION" => ascending
                        ? permissions.OrderBy(x => x.PermissionDisposition, StringComparer.Ordinal)
                        : permissions.OrderByDescending(x => x.PermissionDisposition, StringComparer.Ordinal),
                    "RESOURCE" => ascending
                        ? permissions.OrderBy(x => ComputeResourceSortKey(x.Namespace, x.PageId, pageNamesById), StringComparer.Ordinal)
                        : permissions.OrderByDescending(x => ComputeResourceSortKey(x.Namespace, x.PageId, pageNamesById), StringComparer.Ordinal),
                    _ => throw new InvalidOperationException($"No order by mapping was found in 'GetAccountPermissionsPaged.sql' for the field '{orderBy}'."),
                };

            return ordered
                .Skip((pageNumber - 1) * effectivePageSize)
                .Take(effectivePageSize)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetAccountRoleMembershipPaged.sql: every Users.AccountRole row for <paramref name="userId"/>,
        /// joined to its Role.Name - but only when both a Users.Profile row and a matching Identity (AspNetUsers)
        /// row exist for <paramref name="userId"/> (the reference's own <c>INNER JOIN Profile</c>/<c>INNER JOIN
        /// AspNetUsers</c>, existence-only - no Profile/Identity column is ever selected). The Identity check
        /// requires the separate <see cref="ApplicationDbContext"/>, same two-context split as <see
        /// cref="GetRoleMembersPaged"/>. <see cref="TwAccountRoleMembership.PaginationPageSize"/>/<see
        /// cref="TwAccountRoleMembership.PaginationPageCount"/> always come from the "Pagination Size"
        /// customization setting, ignoring <paramref name="pageSize"/> - same quirk, and same reasoning for not
        /// reproducing the reference's own <c>PaginationPageCount</c> subquery's additional <c>INNER JOIN
        /// AspNetUsers</c>, as <see cref="GetRoleMembersPaged"/> already documents.
        /// </summary>
        /// <remarks>
        /// The reference script's own custom <c>--CONFIG::</c> "OrderBy" mapping (EmailAddress/AccountName/
        /// LastName/FirstName) sorts by columns that - because this method is always scoped to the single
        /// <paramref name="userId"/> passed in - hold the exact same value on every returned row; sorting by a
        /// constant is a no-op (a stable sort leaves relative row order unchanged), so which of those four keys is
        /// requested has no observable effect on the returned order under either the reference or here. This still
        /// validates <paramref name="orderBy"/> against that same four-key set and throws for anything else,
        /// matching the reference script's own behavior for an unrecognized key - including the one caller,
        /// <c>AdminSecurityController.AccountRoles</c>, which (confirmed by inspection) actually reads the query
        /// string key <c>OrderBy_Members</c> while the corresponding column header link
        /// (<c>AccountRoles.cshtml</c>) generates <c>OrderBy_Memberships</c> - a pre-existing key-name mismatch
        /// that means <paramref name="orderBy"/> is always <see langword="null"/> in practice from that caller, out
        /// of scope to fix here. Rows are otherwise returned ordered by <see cref="TwAccountRoleMembership.Id"/>
        /// ascending for determinism, mirroring the reference script's own default <c>ORDER BY U.AccountName,
        /// U.UserId</c> (also both constant per-user, hence also effectively a no-op).
        /// </remarks>
        public async Task<List<TwAccountRoleMembership>> GetAccountRoleMembershipPaged(Guid userId, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            if (!string.IsNullOrEmpty(orderBy))
            {
                switch (orderBy.ToUpperInvariant())
                {
                    case "EMAILADDRESS":
                    case "ACCOUNTNAME":
                    case "LASTNAME":
                    case "FIRSTNAME":
                        break;
                    default:
                        throw new InvalidOperationException($"No order by mapping was found in 'GetAccountRoleMembershipPaged.sql' for the field '{orderBy}'.");
                }
            }

            using var context = _createContext();

            if (!await context.Profiles.AnyAsync(p => p.UserId == userId))
            {
                return [];
            }

            using var identityContext = _createIdentityContext();
            var identityUserId = userId.ToString();
            if (!await identityContext.Users.AnyAsync(u => u.Id == identityUserId))
            {
                return [];
            }

            var memberships = await context.AccountRoles
                .Where(ar => ar.UserId == userId)
                .OrderBy(ar => ar.Id)
                .Select(ar => new TwAccountRoleMembership
                {
                    Id = ar.Id,
                    Name = ar.Role.Name,
                    RoleId = ar.RoleId,
                })
                .ToListAsync();

            var totalCount = memberships.Count;
            var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

            foreach (var membership in memberships)
            {
                membership.PaginationPageSize = paginationSize;
                membership.PaginationPageCount = paginationPageCount;
            }

            return memberships
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .ToList();
        }

        /// <summary>
        /// Mirrors GetAllUsers.sql: every row from <see cref="GetAllAccountUserRowsAsync"/>, unfiltered/unsorted -
        /// same shape as the reference script's own bare <c>SELECT ... FROM Profile INNER JOIN AspNetUsers ...</c>
        /// with no <c>WHERE</c>/<c>ORDER BY</c>/<c>LIMIT</c> clause. Row order is therefore whatever <see
        /// cref="GetAllAccountUserRowsAsync"/> happens to return, same as the reference (no ordering is requested
        /// or guaranteed by either implementation) - the only caller, <c>DummyPageGenerator</c>, only ever picks a
        /// random entry from the result, so this is not observable.
        /// </summary>
        public async Task<List<TwAccountProfile>> GetAllUsers()
        {
            var rows = await GetAllAccountUserRowsAsync();

            return rows.Select(r => new TwAccountProfile
            {
                UserId = r.UserId,
                EmailAddress = r.Email ?? string.Empty,
                AccountName = r.AccountName,
                Navigation = r.Navigation,
                FirstName = r.FirstName,
                LastName = r.LastName,
                TimeZone = r.TimeZone ?? string.Empty,
                Language = r.Language ?? string.Empty,
                Country = r.Country ?? string.Empty,
                CreatedDate = r.CreatedDate,
                ModifiedDate = r.ModifiedDate,
                EmailConfirmed = r.EmailConfirmed,
            }).ToList();
        }

        /// <summary>
        /// Mirrors GetAllUsersPaged.sql: every row from <see cref="GetAllAccountUserRowsAsync"/>, filtered by
        /// <paramref name="searchToken"/> against AccountName/Email/FirstName/LastName (a null/empty token matches
        /// everything, same as the reference's own <c>@SearchToken IS NULL OR ...</c>), then sorted/paged. Custom
        /// ordering mirrors the script's own <c>--CONFIG::</c> mapping (Account/FirstName/LastName/Created/
        /// TimeZone/Language/Country/EmailAddress); an unrecognized <paramref name="orderBy"/> throws, same as
        /// <see cref="GetAllRoles"/>. Default ordering (no <paramref name="orderBy"/>) mirrors the script's own
        /// un-transposed <c>ORDER BY U.AccountName</c> (ascending). <see cref="TwAccountProfile.PaginationPageSize"/>/
        /// <see cref="TwAccountProfile.PaginationPageCount"/> are computed from the same filtered row count - the
        /// reference script's own PaginationPageCount subquery re-aliases its own local <c>Profile AS U</c>/
        /// <c>AspNetUsers AS ANU</c> and so is already correctly filtered by <paramref name="searchToken"/>
        /// (contrast <see cref="GetAllPublicProfilesPaged"/>'s remarks, where the structurally similar subquery is
        /// not correctly filtered due to a reference-side aliasing bug). String comparisons/sorts use <see
        /// cref="StringComparer.Ordinal"/>/<see cref="StringComparison.OrdinalIgnoreCase"/>, same reasoning as
        /// <see cref="GetRoleMembersPaged"/>.
        /// </summary>
        public async Task<List<TwAccountProfile>> GetAllUsersPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, string? searchToken = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            var rows = await GetAllAccountUserRowsAsync();

            var filtered = string.IsNullOrEmpty(searchToken)
                ? rows
                : rows.Where(r =>
                    r.AccountName.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ||
                    (r.Email?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.FirstName?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (r.LastName?.Contains(searchToken, StringComparison.OrdinalIgnoreCase) ?? false))
                    .ToList();

            var paginationPageCount = (filtered.Count + (paginationSize - 1)) / paginationSize;

            var profiles = filtered.Select(r => new TwAccountProfile
            {
                UserId = r.UserId,
                EmailAddress = r.Email ?? string.Empty,
                AccountName = r.AccountName,
                Navigation = r.Navigation,
                FirstName = r.FirstName,
                LastName = r.LastName,
                TimeZone = r.TimeZone ?? string.Empty,
                Language = r.Language ?? string.Empty,
                Country = r.Country ?? string.Empty,
                CreatedDate = r.CreatedDate,
                ModifiedDate = r.ModifiedDate,
                EmailConfirmed = r.EmailConfirmed,
                PaginationPageSize = paginationSize,
                PaginationPageCount = paginationPageCount,
            }).ToList();

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            IOrderedEnumerable<TwAccountProfile> ordered = string.IsNullOrEmpty(orderBy)
                ? profiles.OrderBy(x => x.AccountName, StringComparer.Ordinal)
                : orderBy.ToUpperInvariant() switch
                {
                    "ACCOUNT" => ascending
                        ? profiles.OrderBy(x => x.AccountName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.AccountName, StringComparer.Ordinal),
                    "FIRSTNAME" => ascending
                        ? profiles.OrderBy(x => x.FirstName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.FirstName, StringComparer.Ordinal),
                    "LASTNAME" => ascending
                        ? profiles.OrderBy(x => x.LastName, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.LastName, StringComparer.Ordinal),
                    "CREATED" => ascending
                        ? profiles.OrderBy(x => x.CreatedDate)
                        : profiles.OrderByDescending(x => x.CreatedDate),
                    "TIMEZONE" => ascending
                        ? profiles.OrderBy(x => x.TimeZone, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.TimeZone, StringComparer.Ordinal),
                    "LANGUAGE" => ascending
                        ? profiles.OrderBy(x => x.Language, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.Language, StringComparer.Ordinal),
                    "COUNTRY" => ascending
                        ? profiles.OrderBy(x => x.Country, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.Country, StringComparer.Ordinal),
                    "EMAILADDRESS" => ascending
                        ? profiles.OrderBy(x => x.EmailAddress, StringComparer.Ordinal)
                        : profiles.OrderByDescending(x => x.EmailAddress, StringComparer.Ordinal),
                    _ => throw new InvalidOperationException($"No order by mapping was found in 'GetAllUsersPaged.sql' for the field '{orderBy}'."),
                };

            return ordered
                .Skip((pageNumber - 1) * paginationSize)
                .Take(paginationSize)
                .ToList();
        }

        /// <summary>
        /// Mirrors CreateProfile.sql: inserts a new Users.Profile row for <paramref name="userId"/>/<paramref
        /// name="accountName"/> (Navigation computed via <see cref="TwNavigation.Clean"/>, CreatedDate/ModifiedDate
        /// both set to <see cref="DateTime.UtcNow"/>) - same pre-check-then-insert shape as the reference's own
        /// <c>UsersRepository.CreateProfile</c> C# wrapper, which throws if <see cref="DoesProfileAccountExist"/>
        /// already returns true for the cleaned navigation, rather than letting the real unique index on
        /// Navigation (<see cref="Configurations.Users.ProfileConfiguration"/>) throw a <see
        /// cref="Microsoft.EntityFrameworkCore.DbUpdateException"/> instead.
        /// </summary>
        public async Task CreateProfile(Guid userId, string accountName)
        {
            var navigation = TwNavigation.Clean(accountName);

            if (await DoesProfileAccountExist(navigation))
            {
                throw new Exception("An account with that name already exists");
            }

            using var context = _createContext();

            context.Profiles.Add(new UsersEntities.Profile
            {
                UserId = userId,
                AccountName = accountName,
                Navigation = navigation,
                CreatedDate = DateTime.UtcNow,
                ModifiedDate = DateTime.UtcNow,
            });

            await context.SaveChangesAsync();
        }

        /// <summary>
        /// Mirrors DoesEmailAddressExist.sql: whether an AspNetUsers row with the exact (lower-invariant-compared)
        /// <paramref name="emailAddress"/> exists. Unlike <see cref="DoesRoleExist"/>/<see
        /// cref="DoesProfileAccountExist"/>, this does replicate the reference's own client-side
        /// <c>emailAddress?.ToLowerInvariant()</c> lowering before comparing - AspNetUsers.Email carries no
        /// <c>COLLATE NOCASE</c> (or any other case-insensitive collation) anywhere in this schema, unlike
        /// Users.Role.Name/Users.Profile.Navigation, so there is no DB-level collation to lean on here. A
        /// <see langword="null"/> <paramref name="emailAddress"/> always returns <see langword="false"/>, matching
        /// the reference SQL's own <c>Email = @EmailAddress</c> with a <see langword="null"/>-valued parameter
        /// (standard SQL three-valued logic: "anything = NULL" is never true) - without this explicit guard, EF
        /// Core's null-semantics compensation would instead translate <c>u.Email == email</c> (for a
        /// <see langword="null"/> <c>email</c>) into <c>WHERE Email IS NULL</c>, which is true for any row with a
        /// <see langword="null"/> Email (e.g. the seeded "admin" account), giving the opposite answer.
        /// </summary>
        public async Task<bool> DoesEmailAddressExist(string? emailAddress)
        {
            if (emailAddress == null)
            {
                return false;
            }

            var email = emailAddress.ToLowerInvariant();

            using var identityContext = _createIdentityContext();
            return await identityContext.Users.AnyAsync(u => u.Email == email);
        }

        /// <summary>
        /// Mirrors DoesProfileAccountExist.sql: whether a Users.Profile row with the exact <paramref
        /// name="navigation"/> exists. Case sensitivity is entirely determined by the DB-level collation on
        /// <see cref="UsersEntities.Profile.Navigation"/> (<see cref="Configurations.Users.ProfileConfiguration"/>:
        /// SQLite keeps <c>COLLATE NOCASE</c>; other providers fall back to the database's own default collation) -
        /// same "no client-side StringComparer needed, this filter is translated to SQL" reasoning as <see
        /// cref="DoesRoleExist"/>. Not reproduced: the reference's own client-side
        /// <c>navigation?.ToLowerInvariant()</c> pre-lowering is redundant given <c>COLLATE NOCASE</c> already
        /// makes the comparison case-insensitive, same conclusion already reached for <see cref="DoesRoleExist"/>.
        /// </summary>
        public async Task<bool> DoesProfileAccountExist(string navigation)
        {
            using var context = _createContext();
            return await context.Profiles.AnyAsync(p => p.Navigation == navigation);
        }

        /// <summary>
        /// Mirrors GetBasicProfileByUserId.sql: UserId/AccountName/Navigation/Biography plus the "theme"/
        /// "language" AspNetUserClaims claims (from the separate <see cref="ApplicationDbContext"/>) for
        /// <paramref name="userId"/> - or <see langword="null"/> if no Users.Profile row matches, or (mirroring
        /// the reference's own <c>INNER JOIN AspNetUsers</c>) no matching Identity user exists either. Cached
        /// under <see cref="MemCache.Category.User"/>, same cache key shape ([userId], no forceReCache parameter)
        /// as the reference. Every other <see cref="TwAccountProfile"/> field is deliberately left unset here,
        /// same as the reference script's own column list (it selects only UserId/AccountName/Navigation/
        /// Biography/Theme/Language) - contrast the fuller column list on <see cref="GetAccountProfileByUserId"/>.
        /// </summary>
        public async Task<TwAccountProfile?> GetBasicProfileByUserId(Guid userId)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.User, [userId]);

            return await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();
                var profile = await context.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);
                if (profile == null)
                {
                    return null;
                }

                using var identityContext = _createIdentityContext();
                var identityUserId = userId.ToString();
                if (!await identityContext.Users.AnyAsync(u => u.Id == identityUserId))
                {
                    return null;
                }

                var theme = await identityContext.UserClaims
                    .Where(c => c.UserId == identityUserId && c.ClaimType == "theme")
                    .Select(c => c.ClaimValue)
                    .FirstOrDefaultAsync();

                var language = await identityContext.UserClaims
                    .Where(c => c.UserId == identityUserId && c.ClaimType == "language")
                    .Select(c => c.ClaimValue)
                    .FirstOrDefaultAsync();

                return new TwAccountProfile
                {
                    UserId = profile.UserId,
                    AccountName = profile.AccountName ?? string.Empty,
                    Navigation = profile.Navigation ?? string.Empty,
                    Biography = profile.Biography,
                    Theme = theme,
                    Language = language ?? string.Empty,
                };
            });
        }

        /// <summary>
        /// Shared by <see cref="GetAccountProfileByUserId"/>/<see cref="GetAccountProfileByNavigation"/>: builds
        /// the full <see cref="TwAccountProfile"/> projection both reference scripts share (Avatar/EmailAddress/
        /// AccountName/Navigation/Biography/FirstName/LastName/TimeZone/Language/Country/Theme/CreatedDate/
        /// ModifiedDate/EmailConfirmed) for an already-fetched <paramref name="profile"/>, reading
        /// Email/EmailConfirmed/claims from the separate <see cref="ApplicationDbContext"/> (same two-context
        /// split as <see cref="GetRoleMembersPaged"/>). Returns <see langword="null"/> if no matching Identity
        /// user exists, mirroring both reference scripts' own <c>INNER JOIN AspNetUsers</c>.
        /// </summary>
        private async Task<TwAccountProfile?> BuildFullAccountProfileAsync(UsersEntities.Profile profile)
        {
            using var identityContext = _createIdentityContext();
            var identityUserId = profile.UserId.ToString();

            var identity = await identityContext.Users
                .Where(u => u.Id == identityUserId)
                .Select(u => new { u.Email, u.EmailConfirmed })
                .FirstOrDefaultAsync();

            if (identity == null)
            {
                return null;
            }

            var claims = await identityContext.UserClaims
                .Where(c => c.UserId == identityUserId)
                .Select(c => new { c.ClaimType, c.ClaimValue })
                .ToListAsync();

            string? GetClaimValue(Func<string, bool> claimTypeMatch)
                => claims.FirstOrDefault(c => c.ClaimType != null && claimTypeMatch(c.ClaimType))?.ClaimValue;

            return new TwAccountProfile
            {
                UserId = profile.UserId,
                Avatar = profile.Avatar,
                EmailAddress = identity.Email ?? string.Empty,
                AccountName = profile.AccountName ?? string.Empty,
                Navigation = profile.Navigation ?? string.Empty,
                Biography = profile.Biography,
                FirstName = GetClaimValue(t => t == "firstname"),
                LastName = GetClaimValue(t => t == "lastname"),
                TimeZone = GetClaimValue(t => t == "timezone") ?? string.Empty,
                Language = GetClaimValue(t => t == "language") ?? string.Empty,
                Country = GetClaimValue(t => t.EndsWith("/country", StringComparison.Ordinal)) ?? string.Empty,
                Theme = GetClaimValue(t => t == "theme"),
                CreatedDate = profile.CreatedDate,
                ModifiedDate = profile.ModifiedDate,
                EmailConfirmed = identity.EmailConfirmed,
            };
        }

        /// <summary>
        /// Mirrors GetAccountProfileByUserId.sql: the full <see cref="BuildFullAccountProfileAsync"/> projection
        /// for the Users.Profile row matching <paramref name="userId"/> exactly. Uses
        /// <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleAsync{TSource}(IQueryable{TSource})"/>
        /// for the Profile lookup, matching the reference's <c>QuerySingleAsync&lt;TwAccountProfile&gt;</c>
        /// (Dapper's "exactly one row or throw") - throws if no profile matches. Cached under <see
        /// cref="MemCache.Category.User"/>, same cache key shape ([userId]) as the reference; <paramref
        /// name="forceReCache"/> bypasses the cache exactly like every other <c>forceReCache</c> parameter in this
        /// class.
        /// </summary>
        public async Task<TwAccountProfile> GetAccountProfileByUserId(Guid userId, bool forceReCache = false)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.User, [userId]);

            return (await MemCache.AddOrGetAsync(cacheKey, forceReCache, async () =>
            {
                using var context = _createContext();
                var profile = await context.Profiles.SingleAsync(p => p.UserId == userId);
                return await BuildFullAccountProfileAsync(profile);
            })).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors SetProfileUserId.sql via EF Core's LINQ bulk <c>ExecuteUpdateAsync</c> - updates the primary
        /// key column (Users.Profile.UserId) of the row matching <paramref name="navigation"/> directly in SQL,
        /// bypassing EF's change tracker entirely (which would otherwise require detaching/re-attaching the
        /// entity to change its own primary key) - same idiom as <see cref="RemoveRoleMember"/>'s bulk
        /// <c>ExecuteDeleteAsync</c>, just an update instead of a delete.
        /// </summary>
        public async Task SetProfileUserId(string navigation, Guid userId)
        {
            using var context = _createContext();
            await context.Profiles
                .Where(p => p.Navigation == navigation)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.UserId, userId));
        }

        /// <summary>
        /// Mirrors GetUserAccountIdByNavigation.sql: the UserId of the Users.Profile row matching <paramref
        /// name="navigation"/>. Not reproduced faithfully at the type level, but reproduced exactly at the value
        /// level: the reference's own <c>UsersFactory.QueryFirstOrDefaultAsync&lt;Guid&gt;(...)</c> queries for a
        /// non-nullable <see cref="Guid"/> despite this member's <see cref="Guid"/>? return type and its own XML
        /// doc comment ("or null if not found") - Dapper's <c>QueryFirstOrDefaultAsync&lt;Guid&gt;</c> returns
        /// <see cref="Guid.Empty"/>, not <see langword="null"/>, when zero rows match a value-type projection, so
        /// that is what the reference actually returns for an unknown navigation (confirmed by
        /// <c>SelfDocument.cs</c>'s own <c>GetUserAccountIdByNavigation("admin").EnsureNotNull()</c> call site,
        /// which only compiles/works against this exact behavior - a non-null but empty Guid value passes
        /// <c>EnsureNotNull()</c> unchanged rather than throwing). This reproduces that same "empty Guid, not
        /// null" result for an unknown navigation, via the same "non-nullable projection, implicit conversion to
        /// the nullable return type" shape.
        /// </summary>
        public async Task<Guid?> GetUserAccountIdByNavigation(string navigation)
        {
            using var context = _createContext();

            var userId = await context.Profiles
                .Where(p => p.Navigation == navigation)
                .Select(p => p.UserId)
                .FirstOrDefaultAsync();

            return userId;
        }

        /// <summary>
        /// Mirrors GetAccountProfileByNavigation.sql: the full <see cref="BuildFullAccountProfileAsync"/>
        /// projection for the Users.Profile row matching <paramref name="navigation"/>, or <see langword="null"/>
        /// if no such profile exists (or, mirroring the reference's own <c>INNER JOIN AspNetUsers</c>, no
        /// matching Identity user does). Uses
        /// <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync{TSource}(IQueryable{TSource})"/>
        /// for the Profile lookup, matching the reference's <c>QuerySingleOrDefaultAsync&lt;TwAccountProfile&gt;</c>
        /// - not cached, matching the reference. A <see langword="null"/> <paramref name="navigation"/> always
        /// returns <see langword="null"/>, matching the reference SQL's own <c>Navigation = @Navigation</c> with a
        /// <see langword="null"/>-valued parameter (never matches, standard SQL three-valued logic) - see the same
        /// reasoning on <see cref="DoesEmailAddressExist"/> for why this guard is needed despite <see
        /// cref="Configurations.Users.ProfileConfiguration"/>'s <c>COLLATE NOCASE</c> on Navigation not otherwise
        /// mattering here. In practice this is theoretical: <see cref="UsersEntities.Profile.Navigation"/> is
        /// always populated via <see cref="TwNavigation.Clean(string?)"/> in <see cref="CreateProfile"/>/<see
        /// cref="UpdateProfile"/>, so a <see langword="null"/> Navigation row should never exist.
        /// </summary>
        public async Task<TwAccountProfile?> GetAccountProfileByNavigation(string? navigation)
        {
            if (navigation == null)
            {
                return null;
            }

            using var context = _createContext();
            var profile = await context.Profiles.SingleOrDefaultAsync(p => p.Navigation == navigation);
            if (profile == null)
            {
                return null;
            }

            return await BuildFullAccountProfileAsync(profile);
        }

        /// <summary>
        /// Mirrors GetProfileAvatarByNavigation.sql: the Avatar bytes and AvatarContentType of the Users.Profile
        /// row matching <paramref name="navigation"/>, or <see langword="null"/> if no such profile exists. Uses
        /// <see cref="Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.SingleOrDefaultAsync{TSource}(IQueryable{TSource})"/>,
        /// matching the reference's <c>QuerySingleOrDefaultAsync&lt;TwProfileAvatar&gt;</c> - unlike <see
        /// cref="GetAccountProfileByNavigation"/>, no Identity lookup is needed here (Avatar/AvatarContentType
        /// both live entirely on Users.Profile), so there is no cross-<see cref="ApplicationDbContext"/> join at
        /// all.
        /// </summary>
        public async Task<TwProfileAvatar?> GetProfileAvatarByNavigation(string navigation)
        {
            using var context = _createContext();

            return await context.Profiles
                .Where(p => p.Navigation == navigation)
                .Select(p => new TwProfileAvatar
                {
                    Bytes = p.Avatar,
                    ContentType = p.AvatarContentType ?? string.Empty,
                })
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors UpdateProfile.sql via EF Core's LINQ bulk <c>ExecuteUpdateAsync</c>: updates
        /// AccountName/Navigation/Biography for the Users.Profile row matching <paramref name="item"/>'s UserId -
        /// same idiom as <see cref="SetProfileUserId"/>. Not reproduced: <paramref name="item"/>'s ModifiedDate is
        /// <b>not</b> written - matches the reference script's own column list, which never includes
        /// <c>ModifiedDate</c> in its <c>SET</c> clause despite the reference's own C# wrapper still passing a
        /// <c>ModifiedDate</c> parameter down to it (a dead parameter in the reference, not "fixed" here - this
        /// simply never binds a ModifiedDate parameter at all, same end result). Clears <see
        /// cref="MemCache.Category.User"/> for <paramref name="item"/>'s UserId afterward, same as the reference.
        /// </summary>
        public async Task UpdateProfile(TwAccountProfile item)
        {
            using var context = _createContext();

            await context.Profiles
                .Where(p => p.UserId == item.UserId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.AccountName, item.AccountName)
                    .SetProperty(p => p.Navigation, item.Navigation)
                    .SetProperty(p => p.Biography, item.Biography));

            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.User, [item.UserId]));
        }

        /// <summary>
        /// Mirrors UpdateProfileAvatar.sql via EF Core's LINQ bulk <c>ExecuteUpdateAsync</c>: updates
        /// Avatar/AvatarContentType for the Users.Profile row matching <paramref name="userId"/> - same idiom as
        /// <see cref="UpdateProfile"/>. Clears <see cref="MemCache.Category.User"/> for <paramref name="userId"/>
        /// afterward, same as the reference.
        /// </summary>
        public async Task UpdateProfileAvatar(Guid userId, byte[] imageData, string contentType)
        {
            using var context = _createContext();

            await context.Profiles
                .Where(p => p.UserId == userId)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.Avatar, imageData)
                    .SetProperty(p => p.AvatarContentType, contentType));

            MemCache.ClearCategory(MemCacheKey.Build(MemCache.Category.User, [userId]));
        }

        /// <summary>
        /// Mirrors <c>UsersRepository.AdminPasswordStatus</c> (no SQL script of its own beyond IsAdminPasswordChanged.sql -
        /// it's C# glue, same "pure C# wrapper" shape as <see cref="EfConfigurationRepository.IsFirstRun"/>): reads
        /// the single, possibly-absent Users.AdminPwCheck row (see <see cref="UsersEntities.AdminPwCheck"/>'s own
        /// remarks) and maps its <see cref="UsersEntities.AdminPwCheck.Value"/> to
        /// <see cref="TwAdminPasswordChangeState.HasBeenChanged"/> (1), <see cref="TwAdminPasswordChangeState.IsDefault"/>
        /// (0), or <see cref="TwAdminPasswordChangeState.NeedsToBeSet"/> (no row at all - <see
        /// cref="Queryable.FirstOrDefaultAsync{TSource}(IQueryable{TSource})"/> over a keyless, primary-key-less
        /// table returns <see langword="default"/>/<see langword="null"/> for zero rows, same as the reference's own
        /// <c>ExecuteScalarAsync&lt;bool?&gt;</c> returning <see langword="null"/> for zero rows). Once
        /// <see cref="TwAdminPasswordChangeState.HasBeenChanged"/> has been observed once, it is cached under
        /// <see cref="MemCache.Category.Configuration"/> (no extra key segments) and never re-queried nor
        /// invalidated for the lifetime of the cache entry - same one-way "sticky true" caching quirk as the
        /// reference (there is no code path, here or there, that clears this specific cache key once set), not
        /// treated as a bug worth fixing since <see cref="SetAdminPasswordIsDefault"/>/<see cref="SetAdminPasswordClear"/>
        /// are only ever called once, at first-run bootstrap, long before this could plausibly already be cached as
        /// changed.
        /// </summary>
        public async Task<TwAdminPasswordChangeState> AdminPasswordStatus()
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Configuration);

            if (MemCache.Get<bool?>(cacheKey) == true)
            {
                return TwAdminPasswordChangeState.HasBeenChanged;
            }

            using var context = _createContext();
            var value = await context.AdminPwChecks.Select(a => a.Value).FirstOrDefaultAsync();

            if (value == 1)
            {
                MemCache.Set(cacheKey, true);
                return TwAdminPasswordChangeState.HasBeenChanged;
            }
            if (value == null)
            {
                return TwAdminPasswordChangeState.NeedsToBeSet;
            }

            return TwAdminPasswordChangeState.IsDefault;
        }

        /// <summary>
        /// Mirrors SetAdminPasswordClear.sql ("DELETE FROM AdminPwCheck") via EF Core's LINQ bulk
        /// <c>ExecuteDeleteAsync</c> - Users.AdminPwCheck is a keyless entity type (<c>HasNoKey()</c>), so it cannot
        /// be tracked for <c>Add</c>/<c>Remove</c> like every other entity in this class, same reasoning as <see
        /// cref="EfConfigurationRepository.SetCryptoCheck"/>'s own delete half.
        /// </summary>
        public async Task SetAdminPasswordClear()
        {
            using var context = _createContext();
            await context.AdminPwChecks.ExecuteDeleteAsync();
        }

        /// <summary>
        /// Mirrors SetAdminPasswordIsChanged.sql ("DELETE FROM AdminPwCheck; INSERT INTO AdminPwCheck(Value) SELECT 1").
        /// See <see cref="SetAdminPwCheckValueAsync"/>'s remarks for why the insert half falls back to raw SQL.
        /// </summary>
        public async Task SetAdminPasswordIsChanged() => await SetAdminPwCheckValueAsync(1);

        /// <summary>
        /// Mirrors SetAdminPasswordIsDefault.sql ("DELETE FROM AdminPwCheck; INSERT INTO AdminPwCheck(Value) SELECT 0").
        /// See <see cref="SetAdminPwCheckValueAsync"/>'s remarks for why the insert half falls back to raw SQL.
        /// </summary>
        public async Task SetAdminPasswordIsDefault() => await SetAdminPwCheckValueAsync(0);

        /// <summary>
        /// Shared by <see cref="SetAdminPasswordIsChanged"/>/<see cref="SetAdminPasswordIsDefault"/>: deletes then
        /// re-inserts the single Users.AdminPwCheck row with <paramref name="value"/> - same "keyless entity type,
        /// bulk delete plus a raw parameterized insert whose table/schema identifier <em>and</em> column identifier
        /// are both resolved and quoted via <see cref="ISqlGenerationHelper"/>" idiom as
        /// <see cref="EfConfigurationRepository.SetCryptoCheck"/> (see that member's remarks for why a keyless
        /// entity needs this instead of a tracked <c>Add</c>, and for why both identifiers are resolved
        /// dynamically rather than hardcoding one provider's identifier quoting - an unquoted column name would
        /// silently lowercase-fold on Postgres and fail to match the quoted, PascalCase column its migrations
        /// actually create).
        /// </summary>
        private async Task SetAdminPwCheckValueAsync(int value)
        {
            using var context = _createContext();

            await context.AdminPwChecks.ExecuteDeleteAsync();

            //Built via plain string concatenation, not a C# interpolated-string literal, so that EF Core's
            //"don't hand raw SQL an interpolated string" analyzer (EF1002) does not flag this - "{0}" below is a
            //literal placeholder for ExecuteSqlRawAsync's own (safe, provider-parameterized) substitution, not a
            //C# interpolation hole. quotedTable/quotedColumn themselves come only from trusted EF metadata (never
            //user input), so splicing them into the SQL text is not an injection risk.
            var quotedTable = GetQuotedAdminPwCheckTableName(context);
            var quotedColumn = GetQuotedAdminPwCheckValueColumnName(context);
            var insertSql = "INSERT INTO " + quotedTable + " (" + quotedColumn + ") VALUES ({0})";
            await context.Database.ExecuteSqlRawAsync(insertSql, value);
        }

        private static string GetQuotedAdminPwCheckTableName(TightWikiDbContext context)
        {
            var entityType = context.Model.FindEntityType(typeof(UsersEntities.AdminPwCheck))
                ?? throw new InvalidOperationException(
                    $"'{typeof(UsersEntities.AdminPwCheck)}' is not part of the {nameof(TightWikiDbContext)} model.");

            var sqlGenerationHelper = context.GetService<ISqlGenerationHelper>();
            return sqlGenerationHelper.DelimitIdentifier(entityType.GetTableName()!, entityType.GetSchema());
        }

        private static string GetQuotedAdminPwCheckValueColumnName(TightWikiDbContext context)
        {
            var entityType = context.Model.FindEntityType(typeof(UsersEntities.AdminPwCheck))
                ?? throw new InvalidOperationException(
                    $"'{typeof(UsersEntities.AdminPwCheck)}' is not part of the {nameof(TightWikiDbContext)} model.");

            var property = entityType.FindProperty(nameof(UsersEntities.AdminPwCheck.Value))
                ?? throw new InvalidOperationException(
                    $"'{nameof(UsersEntities.AdminPwCheck.Value)}' is not part of the {typeof(UsersEntities.AdminPwCheck)} model.");

            var sqlGenerationHelper = context.GetService<ISqlGenerationHelper>();
            return sqlGenerationHelper.DelimitIdentifier(property.GetColumnName());
        }

        #region Security.

        /// <summary>
        /// Mirrors <c>UsersRepository.ValidateEncryptionAndCreateAdminUser</c>: on first run (<see
        /// cref="ITwConfigurationRepository.IsFirstRun"/>), clears the admin-password-changed state (<see
        /// cref="SetAdminPasswordClear"/>), then - only if <see cref="AdminPasswordStatus"/> still reports <see
        /// cref="TwAdminPasswordChangeState.NeedsToBeSet"/> - creates (or reuses, by <see
        /// cref="Constants.DEFAULTUSERNAME"/>) the built-in admin <see cref="IdentityUser"/> via <paramref
        /// name="userManager"/>, confirms its email, grants it the "Administrator" claim role plus the
        /// Users.ConfigurationEntry-driven default timezone/country/language claims (<see cref="UpsertUserClaims"/>),
        /// resets its password to <see cref="Constants.DEFAULTPASSWORD"/>, marks the admin password as default
        /// (<see cref="SetAdminPasswordIsDefault"/>), and finally ensures a Users.Profile row exists for it under
        /// <see cref="Constants.DEFAULTACCOUNT"/> - creating one (<see cref="CreateProfile"/>) if none exists yet,
        /// or re-pointing an existing one at the (re)created Identity user's id (<see cref="SetProfileUserId"/>)
        /// otherwise, exactly as the reference intends.
        /// </summary>
        /// <remarks>
        /// <para>
        /// Two confirmed, long-standing bugs in the reference are deliberately <b>not</b> reproduced here - both
        /// were introduced together in commit <c>e5b230aa</c> ("Cleanup.") and have never been touched since
        /// (confirmed via <c>git log --follow -p</c>), so neither is a recent regression:
        /// <list type="bullet">
        /// <item><description>
        /// The reference's own final "did the password reset succeed?" check
        /// (<c>if (!result.Succeeded) { throw new Exception(string.Join("\r\n",
        /// emailUpdateResult.Errors.Select(...))); }</c>) references the wrong variable - <c>emailUpdateResult</c>
        /// (the email-update call's result, already known to have succeeded at that point, so always an empty
        /// <c>Errors</c> collection) instead of <c>result</c> (the actual <c>ResetPasswordAsync</c> result being
        /// checked) - so a real password-reset failure would throw with an empty/misleading message instead of the
        /// actual Identity errors. This uses the correct <c>result.Errors</c> instead.
        /// </description></item>
        /// <item><description>
        /// The reference's own final "does a profile already exist?" check
        /// (<c>var existingProfileUserId = GetUserAccountIdByNavigation(...); if (existingProfileUserId == null)</c>)
        /// is missing an <c>await</c> - <c>existingProfileUserId</c> ends up holding the still-running
        /// <c>Task&lt;Guid?&gt;</c> object itself (never <see langword="null"/>), not its resolved value, so the
        /// condition is always <see langword="false"/> and the reference always takes the "profile already exists"
        /// branch (<c>SetProfileUserId</c>), never the "create it" branch (<c>CreateProfile</c>) - on a genuinely
        /// fresh install with no pre-existing Users.Profile row for <see cref="Constants.DEFAULTACCOUNT"/>, that
        /// <c>UPDATE ... WHERE Navigation = @Navigation</c> silently affects zero rows and the admin account ends
        /// up with no profile at all. Doubly so because even a properly-awaited call would still never observe
        /// <see langword="null"/> here: <see cref="GetUserAccountIdByNavigation"/>'s own doc comment already
        /// documents that it returns <see cref="Guid.Empty"/>, not <see langword="null"/>, for "no such profile" -
        /// mirroring the reference script's own <c>QueryFirstOrDefaultAsync&lt;Guid&gt;</c> non-nullable-projection
        /// behavior. This awaits the call and treats both <see langword="null"/> and <see cref="Guid.Empty"/> as
        /// "no profile exists yet", so a genuinely fresh install now gets one created.
        /// </description></item>
        /// </list>
        /// </para>
        /// <para>
        /// A third bug - this one in <c>TightWiki/Program.cs</c>'s shared (not <c>#if</c>-gated) call site rather
        /// than in <see cref="ITwUsersRepository.ValidateEncryptionAndCreateAdminUser"/>'s SQLite implementation
        /// itself - is also deliberately not reproduced, and is the reason this member is a thin synchronous
        /// wrapper around <see cref="ValidateEncryptionAndCreateAdminUserAsync"/> rather than the reference's own
        /// <see langword="async void"/> method body directly. <c>Program.cs</c> resolves <paramref
        /// name="userManager"/> from a manually created <c>using (var scope = app.Services.CreateScope())</c> block
        /// and calls this member as the last statement inside it, without <c>await</c> (impossible anyway - the
        /// interface member is <see langword="void"/>, not <see cref="Task"/>). The SQLite reference implements this
        /// the same way the interface demands - <see langword="async void"/> - which resumes past its first
        /// <see langword="await"/> (<c>_configurationRepository.IsFirstRun()</c>) as a queued continuation on a
        /// thread-pool thread, by which point <c>Program.cs</c>'s <c>using</c> block has already disposed
        /// <paramref name="userManager"/> (and everything else in that scope). Confirmed live against SQL Server
        /// LocalDB (built with <c>-p:DataProvider=SqlServer</c>): reproducing the reference's <see
        /// langword="async void"/> shape verbatim here made every startup crash the whole process with an unhandled
        /// <see cref="ObjectDisposedException"/> ("Cannot access a disposed object... UserManager`1") thrown from a
        /// thread-pool continuation - unrecoverable, since <c>Program.cs</c>'s own <see langword="try"/>/<see
        /// langword="catch"/> around the call can only ever observe exceptions from the synchronous portion of an
        /// <see langword="async void"/> method (everything up to its first <see langword="await"/>), never from
        /// later continuations. This bug is latent, not absent, under the SQLite driver: <c>Microsoft.Data.Sqlite</c>'s
        /// ADO.NET provider does not perform genuine asynchronous I/O (a well-documented limitation - SQLite has no
        /// async C API), so every <see langword="await"/> in the reference's call graph resolves an
        /// already-completed <see cref="Task"/> and the whole method runs to completion synchronously on the
        /// caller's own thread before ever returning - the same race condition exists in principle, it simply never
        /// gets a chance to manifest. Blocking here via <see cref="Task.GetAwaiter"/>/<c>GetResult()</c> (safe: no
        /// captured <see cref="System.Threading.SynchronizationContext"/> exists on the plain thread-pool/<c>Main</c>
        /// thread this runs on, so there is nothing to deadlock against) makes this member actually synchronous from
        /// the caller's point of view again, restoring both the intended "the whole bootstrap finished, or this
        /// member threw, before <c>Program.cs</c> moves on" semantics and the caller's <see langword="try"/>/<see
        /// langword="catch"/>'s ability to observe every failure - not just synchronous ones - regardless of
        /// provider. Not applicable to <see cref="UpsertUserClaims"/> (also called from <c>Program.cs</c>, but only
        /// indirectly from within <see cref="ValidateEncryptionAndCreateAdminUserAsync"/>, and independently by
        /// every other caller as a properly awaited <see cref="Task"/>-returning member from a request-lifetime
        /// scope, not a use-and-immediately-dispose one).
        /// </para>
        /// </remarks>
        public void ValidateEncryptionAndCreateAdminUser(UserManager<IdentityUser> userManager)
            => ValidateEncryptionAndCreateAdminUserAsync(userManager).GetAwaiter().GetResult();

        /// <summary>
        /// The fully asynchronous body behind <see cref="ValidateEncryptionAndCreateAdminUser"/> - see that
        /// member's doc comment/remarks for the full behavior and for why it is invoked via a blocking
        /// <c>GetAwaiter().GetResult()</c> wrapper rather than being <see langword="async void"/> itself.
        /// </summary>
        private async Task ValidateEncryptionAndCreateAdminUserAsync(UserManager<IdentityUser> userManager)
        {
            if (await _configurationRepository.IsFirstRun())
            {
                //If this is the first time the app has run on this machine (based on an encryption key) then clear
                //the admin password status. This will cause the application to set the admin password to the
                //default password and display a warning until it is changed.
                await SetAdminPasswordClear();
            }

            if (await AdminPasswordStatus() == TwAdminPasswordChangeState.NeedsToBeSet)
            {
                var user = await userManager.FindByNameAsync(Constants.DEFAULTUSERNAME);
                if (user == null)
                {
                    var creationResult = await userManager.CreateAsync(new IdentityUser(Constants.DEFAULTUSERNAME), Constants.DEFAULTPASSWORD);
                    if (!creationResult.Succeeded)
                    {
                        throw new Exception(string.Join("\r\n", creationResult.Errors.Select(o => o.Description)));
                    }

                    user = await userManager.FindByNameAsync(Constants.DEFAULTUSERNAME);
                }

                user.EnsureNotNull();

                user.Email = Constants.DEFAULTUSERNAME; // Ensure email is set or updated
                user.EmailConfirmed = true;
                var emailUpdateResult = await userManager.UpdateAsync(user);
                if (!emailUpdateResult.Succeeded)
                {
                    throw new Exception(string.Join("\r\n", emailUpdateResult.Errors.Select(o => o.Description)));
                }

                var membershipConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(TwConfigGroup.Membership);

                var claimsToAdd = new List<Claim>
                {
                    new (ClaimTypes.Role, "Administrator"),
                    new ("timezone", membershipConfig.Value<string>("Default TimeZone").EnsureNotNull()),
                    new (ClaimTypes.Country, membershipConfig.Value<string>("Default Country").EnsureNotNull()),
                    new ("language", membershipConfig.Value<string>("Default Language").EnsureNotNull()),
                };

                await UpsertUserClaims(userManager, user, claimsToAdd);

                var token = await userManager.GeneratePasswordResetTokenAsync(user);
                var result = await userManager.ResetPasswordAsync(user, token, Constants.DEFAULTPASSWORD);
                if (!result.Succeeded)
                {
                    // Not reproduced: the SQLite reference references emailUpdateResult.Errors here instead of
                    // result.Errors - see this member's <remarks> for the full writeup.
                    throw new Exception(string.Join("\r\n", result.Errors.Select(o => o.Description)));
                }

                await SetAdminPasswordIsDefault();

                // Not reproduced: the SQLite reference is missing an "await" on GetUserAccountIdByNavigation here
                // (and even awaited, that member never returns null - see this member's <remarks> for the full
                // writeup), so it always falls into the "else" branch below. This awaits the call and treats
                // Guid.Empty the same as null.
                var existingProfileUserId = await GetUserAccountIdByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT));
                if (existingProfileUserId == null || existingProfileUserId == Guid.Empty)
                {
                    await CreateProfile(Guid.Parse(user.Id), Constants.DEFAULTACCOUNT);
                }
                else
                {
                    await SetProfileUserId(Constants.DEFAULTACCOUNT, Guid.Parse(user.Id));
                }

                await EnsureAdministratorRoleMembership(Guid.Parse(user.Id));
            }
        }

        /// <summary>
        /// Grants <paramref name="userId"/> membership in the built-in "Administrator" Users.Role via a
        /// Users.AccountRole row - called once, at the end of <see cref="ValidateEncryptionAndCreateAdminUserAsync"/>,
        /// right after that method ensures the admin's Users.Profile row exists (a prerequisite: see <see
        /// cref="Configurations.Users.AccountRoleConfiguration"/>'s real FK from AccountRole.UserId onto
        /// Profile.UserId).
        /// </summary>
        /// <remarks>
        /// <b>Not mirrored from any SQLite reference script - this closes a gap that has no SQLite-side
        /// equivalent to keep in sync with.</b> On SQLite, <c>Defaults/defaults.db</c> (built from the committed
        /// dev fixture <c>Data/users.db</c>) already ships this admin account pre-seeded end-to-end under a fixed,
        /// well-known <see cref="Guid"/>: an <c>AspNetUsers</c> row, a matching <c>Profile</c> row, and - critically -
        /// an <c>AccountRole</c> row already linking that same <see cref="Guid"/> to the "Administrator"
        /// <c>Role</c> row (confirmed by inspection of <c>Data/users.db</c>). So on a fresh SQLite install, this
        /// class's <c>ValidateEncryptionAndCreateAdminUserAsync</c>'s own <c>userManager.FindByNameAsync(Constants.DEFAULTUSERNAME)</c>
        /// call finds that pre-existing, already-role-member Identity user (not <see langword="null"/>) and never
        /// takes the "create a new one" branch at all - the SQLite reference itself never needs to insert an
        /// AccountRole row here because one already exists by construction.
        /// <para>
        /// This driver's seed data has no equivalent: the provider-neutral seed package
        /// (<c>Seed/tightwiki.seed.zip</c>, Database-Providers-Plan.md chapter 4.6) deliberately carries only
        /// configuration/themes/feature templates/wiki pages/menu items/emoji - no user, profile, role, or
        /// account-role data (by design; a dynamically-generated ASP.NET Identity <see cref="Guid"/> cannot be
        /// baked into a static seed package the way SQLite's fixed-Guid dev fixture can). So on a fresh SQL
        /// Server/Postgres install, <c>userManager.FindByNameAsync</c> above always takes the "create a new
        /// <see cref="IdentityUser"/>" branch, and without this method that new user would be left with the
        /// "Administrator" <see cref="ClaimTypes.Role"/> claim (see the <c>claimsToAdd</c> block above) but zero
        /// <c>Users.AccountRole</c> rows - which matters because <c>TwSessionState.IsAdministrator</c>/
        /// <c>HoldsPermission</c> (the wiki's own authorization system, entirely separate from ASP.NET Identity's
        /// own claims/roles) never consults that claim at all: <see cref="IsUserMemberOfAdministrators"/> is the
        /// only thing that sets <c>IsAdministrator</c>, and it is purely an <c>Users.AccountRole</c> JOIN
        /// <c>Users.Role</c> query. Confirmed live against SQL Server LocalDB, before this fix existed: the
        /// freshly seeded admin account could authenticate (log in successfully) but held zero permissions on
        /// anything at all, including reading the public home page - every request redirected to
        /// <c>/Utility/Notify?NotifyErrorMessage=You+do+not+have+permission+to+perform+the+action%3A+Read</c>.
        /// </para>
        /// <para>
        /// Existence-checked rather than an unconditional insert (mirroring <see cref="IsAccountAMemberOfRole"/>'s
        /// own "check before insert" shape, though without its <see cref="MemCache"/> layer - this runs once at
        /// startup, well before any request could have populated or relied on that cache) since
        /// <see cref="Configurations.Users.AccountRoleConfiguration"/>'s real unique index on
        /// <c>(UserId, RoleId)</c> would otherwise throw a <see cref="Microsoft.EntityFrameworkCore.DbUpdateException"/>
        /// if this whole gated block ever re-runs for the same admin user (e.g. a later <see cref="SetAdminPasswordClear"/>
        /// call re-arming <see cref="TwAdminPasswordChangeState.NeedsToBeSet"/> on a subsequent "first run").
        /// A missing "Administrator" <c>Users.Role</c> row (never expected in practice - it is one of the five
        /// <c>IsBuiltIn</c> roles <see cref="Configurations.Users.RoleConfiguration"/> seeds via migration
        /// <c>HasData</c>) is treated as a silent no-op rather than thrown, consistent with how
        /// <see cref="InsertRoleMemberAndBuildResultAsync"/> already handles the same "role does not exist" case
        /// for every other caller of the role-membership members in this class.
        /// </para>
        /// </remarks>
        private async Task EnsureAdministratorRoleMembership(Guid userId)
        {
            using var context = _createContext();

            var administratorRoleId = await context.Roles
                .Where(r => r.Name == TwRoles.Administrator.ToString())
                .Select(r => (int?)r.Id)
                .FirstOrDefaultAsync();

            if (administratorRoleId == null)
            {
                return;
            }

            var alreadyMember = await context.AccountRoles
                .AnyAsync(ar => ar.UserId == userId && ar.RoleId == administratorRoleId.Value);

            if (!alreadyMember)
            {
                context.AccountRoles.Add(new UsersEntities.AccountRole { UserId = userId, RoleId = administratorRoleId.Value });
                await context.SaveChangesAsync();
            }
        }

        /// <summary>
        /// Mirrors <c>UsersRepository.UpsertUserClaims</c>: for each of <paramref name="givenClaims"/>, removes any
        /// existing claim of the same <see cref="Claim.Type"/> already on <paramref name="user"/> (if present),
        /// then adds the new claim - all purely against ASP.NET Core Identity's <paramref name="userManager"/>, no
        /// <see cref="TightWikiDbContext"/>/<see cref="ApplicationDbContext"/> access needed here at all, since
        /// Identity itself already persists claims via its own <c>AddEntityFrameworkStores&lt;ApplicationDbContext&gt;()</c>
        /// registration regardless of which provider is active. Throws if the final <c>userManager.UpdateAsync(user)</c>
        /// does not succeed, same as the reference.
        /// </summary>
        public async Task UpsertUserClaims(UserManager<IdentityUser> userManager, IdentityUser user, List<Claim> givenClaims)
        {
            var existingClaims = await userManager.GetClaimsAsync(user);

            foreach (var givenClaim in givenClaims)
            {
                var existingClaim = existingClaims.FirstOrDefault(c => c.Type == givenClaim.Type);
                if (existingClaim != null)
                {
                    await userManager.RemoveClaimAsync(user, existingClaim);
                }

                await userManager.AddClaimAsync(user, givenClaim);
            }

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded)
            {
                throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
            }
        }

        #endregion
    }
}
