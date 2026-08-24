using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the permissions and admin-default-password-state-machine slices of
    /// <see cref="ITwUsersRepository"/> (51 members total - far too many for one test file, per this task's own
    /// brief, same reasoning as <see cref="UsersRepositoryRoleTests"/>). Covers the 13 "permissions" members
    /// <c>EfUsersRepository</c>'s own class-level remarks call out as implemented together in phase 2b.10 (Users
    /// &lt;-&gt; Pages cross-schema, four <c>Attach("pages.db")</c> call sites on the SQLite reference side - see
    /// <see cref="InsertRolePermission_PageScoped_ResourceName_ResolvesToSeededPageName_CrossSchemaLookup"/>/<see
    /// cref="InsertAccountPermission_PageScoped_ResourceName_ResolvesToSeededPageName_CrossSchemaLookup"/> below,
    /// which specifically exercise that join): <see cref="ITwUsersRepository.IsAccountPermissionDefined"/>, <see
    /// cref="ITwUsersRepository.InsertAccountPermission"/>, <see cref="ITwUsersRepository.IsRolePermissionDefined"/>,
    /// <see cref="ITwUsersRepository.RemoveRolePermission"/>, <see cref="ITwUsersRepository.RemoveAccountPermission"/>,
    /// <see cref="ITwUsersRepository.InsertRolePermission"/>, <see cref="ITwUsersRepository.GetApparentAccountPermissions"/>,
    /// both <see cref="ITwUsersRepository.GetApparentRolePermissions(TwRoles)"/>/<see
    /// cref="ITwUsersRepository.GetApparentRolePermissions(string)"/> overloads, <see
    /// cref="ITwUsersRepository.GetAllPermissionDispositions"/>, <see cref="ITwUsersRepository.GetAllPermissions"/>,
    /// <see cref="ITwUsersRepository.GetRolePermissionsPaged"/> and <see
    /// cref="ITwUsersRepository.GetAccountPermissionsPaged"/> - plus the 4-member admin-default-password state
    /// machine from phase 2b.12: <see cref="ITwUsersRepository.AdminPasswordStatus"/>, <see
    /// cref="ITwUsersRepository.SetAdminPasswordClear"/>, <see cref="ITwUsersRepository.SetAdminPasswordIsChanged"/>,
    /// <see cref="ITwUsersRepository.SetAdminPasswordIsDefault"/>. <see
    /// cref="ITwUsersRepository.GetAccountRoleMembershipPaged"/> (also nominally "permissions category", phase
    /// 2b.10) is deliberately <b>not</b> re-covered here - it is already exercised by
    /// <see cref="UsersRepositoryRoleTests"/> (see that class's own remarks for why it lives there instead).
    /// <see cref="ITwUsersRepository.ValidateEncryptionAndCreateAdminUser"/>/<see
    /// cref="ITwUsersRepository.UpsertUserClaims"/> (the actual Identity admin-bootstrap flow, phase 2b.13) and
    /// every profile-CRUD member (phase 2b.11) are out of scope for this task - see its own brief - and are left
    /// for a separate, not-yet-written task.
    /// <para>
    /// Obtained through <see cref="TwEngineFixture"/> exactly like every sibling repository test class in this
    /// project. Written entirely against the provider-agnostic interface - no <c>#if SQLITE_PROVIDER</c>/
    /// <c>#elif SQLSERVER_PROVIDER</c>/<c>#elif POSTGRES_PROVIDER</c> branching here. The same compiled test code
    /// runs three times: <c>dotnet test</c> (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>,
    /// <c>dotnet test -p:DataProvider=Postgres</c> (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// These run against the shared, persistent test database (chapter 5.3), concurrently with every other xunit
    /// collection in this assembly - same reasoning as every sibling repository test class. Every mutating test
    /// below avoids colliding with <see cref="UsersRepositoryRoleTests"/> (which mutates the seeded <c>admin</c>
    /// account's Users.AccountRole <b>membership</b> rows) by only ever touching Users.AccountPermission/
    /// Users.RolePermission rows - a structurally separate pair of tables - for either a brand-new, GUID-suffixed
    /// "TestRole_"-prefixed role (created here, deleted again via <see cref="ITwUsersRepository.DeleteRole"/> in a
    /// <c>finally</c> block - which also deletes any leftover Users.RolePermission rows for it, see
    /// <c>EfUsersRepository.DeleteRole</c>'s own doc comment, so no separate <see
    /// cref="ITwUsersRepository.RemoveRolePermission"/> cleanup is strictly required there) or the real seeded
    /// <c>admin</c> account's Users.AccountPermission rows (removed again via <see
    /// cref="ITwUsersRepository.RemoveAccountPermission"/> in a <c>finally</c> block - unlike a role, there is no
    /// "delete the whole account" member on this interface to lean on instead, see
    /// <see cref="UsersRepositoryRoleTests"/>'s own remarks on why a new Users.Profile row is never created here
    /// either). Every namespace-scoped grant below additionally uses a fresh, GUID-suffixed
    /// <c>"TestNamespace_&lt;guid&gt;"</c> scope string - confirmed by direct inspection of the SQLite dev fixture
    /// (<c>Data/users.db</c>: exactly one pre-existing Users.AccountPermission row for <c>admin</c>, scoped
    /// Namespace=NULL/PageId='*', and 28 pre-existing Users.RolePermission rows across the five seeded roles, none
    /// of them namespace-scoped to anything a GUID could ever collide with) to guarantee <see
    /// cref="ITwUsersRepository.IsAccountPermissionDefined"/>/<see cref="ITwUsersRepository.IsRolePermissionDefined"/>'s
    /// own "an existing broader row already covers this" semantics (see <c>EfUsersRepository.IsAccountPermissionDefined</c>'s
    /// own doc comment for the three-valued-logic derivation) can never accidentally match a pre-existing row,
    /// regardless of what this shared database's exact permission/role content looks like on any given run.
    /// </para>
    /// <para>
    /// <b><see cref="ITwUsersRepository.GetApparentAccountPermissions"/>/both <see
    /// cref="ITwUsersRepository.GetApparentRolePermissions(TwRoles)"/>/<c>(string)</c> overloads are cached with
    /// no <c>forceReCache</c> parameter at all</b> (unlike <see cref="ITwUsersRepository.IsAccountPermissionDefined"/>/
    /// <see cref="ITwUsersRepository.IsRolePermissionDefined"/>, both of which do have one, always passed
    /// <see langword="true"/> below) - confirmed by inspection of <c>EfUsersRepository</c>, neither the insert nor
    /// the remove path for either permission table clears that cache. Every test below that inserts a permission
    /// therefore asserts against <c>GetApparentAccountPermissions</c>/<c>GetApparentRolePermissions</c> <b>exactly
    /// once</b>, immediately after the insert and before any removal - the first-ever call for a given cache key
    /// (a brand-new GUID-suffixed role name, or the "admin" <see cref="Guid"/> the very first time this file
    /// exercises it) always computes fresh, but a second call with the same key later in the same test would
    /// silently return the stale, pre-removal cached list instead of re-querying, which would make a
    /// post-removal "does not contain" assertion fail for the wrong reason. Post-removal verification instead uses
    /// <see cref="ITwUsersRepository.GetRolePermissionsPaged"/>/<see cref="ITwUsersRepository.GetAccountPermissionsPaged"/>
    /// (genuinely uncached - confirmed by inspection) and the <c>forceReCache: true</c> "Is...Defined" checks.
    /// </para>
    /// <para>
    /// <b><see cref="ITwUsersRepository.AdminPasswordStatus"/> has its own, more severe caching gotcha</b> - see
    /// <see cref="AdminPasswordStatus_SetAdminPasswordClear_SetAdminPasswordIsDefault_SetAdminPasswordIsChanged_StateMachine_RoundTrip"/>'s
    /// own remarks for the full writeup (a process-wide "sticky true" cache with no per-role/per-user key
    /// segmentation at all, plus an interaction with <c>MockWikiEngineArtifacts</c>' own SqlServer/Postgres-only
    /// bootstrap call).
    /// </para>
    /// </remarks>
    [Collection("Users Repository Permission Auth Tests")]
    public class UsersRepositoryPermissionAuthTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// The full name (with namespace prefix) of the real, already-seeded page used by the two
        /// page-scoped/cross-schema tests below to resolve a permission's ResourceName - same page,
        /// same "real seed content, not a fixture-created page" reasoning, as
        /// <c>StatisticsRepositoryTests.SeededPageName</c>.
        /// </summary>
        private const string SeededPageName = "Sandbox :: Default";

        /// <summary>
        /// Walks every page of <see cref="ITwUsersRepository.GetRolePermissionsPaged"/> for <paramref
        /// name="roleId"/> and returns every row found - same "don't trust a single page against the shared test
        /// database" reasoning as <c>UsersRepositoryRoleTests.RoleHasMemberAsync</c>/<c>StatisticsRepositoryTests.GetAllPageStatisticsAsync</c>.
        /// </summary>
        private static async Task<List<TwRolePermission>> GetAllRolePermissionsAsync(ITwUsersRepository repo, int roleId)
        {
            var all = new List<TwRolePermission>();
            var firstPage = await repo.GetRolePermissionsPaged(roleId, 1);
            all.AddRange(firstPage);
            var totalPages = firstPage.Count > 0 ? firstPage[0].PaginationPageCount : 1;
            for (var pageNumber = 2; pageNumber <= totalPages; pageNumber++)
            {
                all.AddRange(await repo.GetRolePermissionsPaged(roleId, pageNumber));
            }
            return all;
        }

        /// <summary>
        /// Same as <see cref="GetAllRolePermissionsAsync"/>, for <see
        /// cref="ITwUsersRepository.GetAccountPermissionsPaged"/> instead.
        /// </summary>
        private static async Task<List<TwAccountPermission>> GetAllAccountPermissionsAsync(ITwUsersRepository repo, Guid userId)
        {
            var all = new List<TwAccountPermission>();
            var firstPage = await repo.GetAccountPermissionsPaged(userId, 1);
            all.AddRange(firstPage);
            var totalPages = firstPage.Count > 0 ? firstPage[0].PaginationPageCount : 1;
            for (var pageNumber = 2; pageNumber <= totalPages; pageNumber++)
            {
                all.AddRange(await repo.GetAccountPermissionsPaged(userId, pageNumber));
            }
            return all;
        }

        [Fact]
        public async Task GetAllPermissions_GetAllPermissionDispositions_ReturnConsistentSeededSets()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            //TightWiki.Plugin.TwPermission/TwPermissionDisposition (enums, not the same-named
            //TightWiki.Plugin.Models.TwPermission/TwPermissionDisposition model classes GetAllPermissions/
            //GetAllPermissionDispositions actually return) enumerate the fixed, five-permission/two-disposition
            //seeded set by name - same set confirmed by inspection of
            //Scripts/Initialization/Versions/2.26.0/^001^Users^PermissionDisposition.sql/^003^Users^Permission.sql
            //(SQLite) and PermissionConfiguration/PermissionDispositionConfiguration's own HasData (EF, identical
            //Ids across every provider - both are "static lookup" tables per Database-Providers-Plan.md chapter
            //4.6a, unlike Users.Role whose Ids are confirmed to differ across providers).
            var permissionNames = Enum.GetNames<TightWiki.Plugin.TwPermission>();
            var dispositionNames = Enum.GetNames<TightWiki.Plugin.TwPermissionDisposition>();

            var permissions = await repo.GetAllPermissions();
            Assert.True(permissions.Count >= permissionNames.Length,
                $"Expected at least {permissionNames.Length} seeded permissions, found {permissions.Count}.");
            foreach (var name in permissionNames)
            {
                Assert.Contains(permissions, p => p.Name == name);
            }

            var dispositions = await repo.GetAllPermissionDispositions();
            Assert.True(dispositions.Count >= dispositionNames.Length,
                $"Expected at least {dispositionNames.Length} seeded permission dispositions, found {dispositions.Count}.");
            foreach (var name in dispositionNames)
            {
                Assert.Contains(dispositions, d => d.Name == name);
            }
        }

        [Fact]
        public async Task InsertRolePermission_IsRolePermissionDefined_GetRolePermissionsPaged_GetApparentRolePermissions_RemoveRolePermission_RoundTrip_NamespaceScope()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var marker = Guid.NewGuid().ToString("N");
            var roleName = $"TestRole_{marker}";
            await repo.InsertRole(roleName, "Test role for namespace-scoped Users.RolePermission grants.");
            var role = await repo.GetRoleByName(roleName);

            try
            {
                var permission = (await repo.GetAllPermissions()).Single(p => p.Name == TightWiki.Plugin.TwPermission.Edit.ToString());
                var disposition = (await repo.GetAllPermissionDispositions()).Single(d => d.Name == TightWiki.Plugin.TwPermissionDisposition.Deny.ToString());
                var ns = $"TestNamespace_{marker}";

                Assert.False(await repo.IsRolePermissionDefined(role.Id, permission.Id, disposition.Id.ToString(), ns, null, forceReCache: true));

                var insertResult = await repo.InsertRolePermission(role.Id, permission.Id, disposition.Id.ToString(), ns, null);
                Assert.NotNull(insertResult);
                Assert.True(insertResult!.Id > 0);
                Assert.Equal(permission.Name, insertResult.Permission);
                Assert.Equal(disposition.Name, insertResult.PermissionDisposition);
                Assert.Equal(ns, insertResult.Namespace);
                Assert.Null(insertResult.PageId);
                //Namespace wins over PageId in the ResourceName precedence (ResolveResourceNameAsync's own
                //remarks) - trivially true here since PageId is null anyway, but asserted for completeness.
                Assert.Equal(ns, insertResult.ResourceName);

                Assert.True(await repo.IsRolePermissionDefined(role.Id, permission.Id, disposition.Id.ToString(), ns, null, forceReCache: true));

                var stored = (await GetAllRolePermissionsAsync(repo, role.Id)).Single(rp => rp.Id == insertResult.Id);
                Assert.Equal(permission.Name, stored.Permission);
                Assert.Equal(disposition.Name, stored.PermissionDisposition);
                Assert.Equal(ns, stored.Namespace);
                Assert.Equal(ns, stored.ResourceName);

                //Exactly one GetApparentRolePermissions call in this whole test - see this class's own remarks
                //for why a second one (e.g. after the removal below) would return a stale, pre-removal result.
                var apparent = await repo.GetApparentRolePermissions(roleName);
                Assert.Contains(apparent, a => a.Permission == permission.Name && a.PermissionDisposition == disposition.Name
                    && a.Namespace == ns && a.PageId == null);

                await repo.RemoveRolePermission(insertResult.Id);
                Assert.False(await repo.IsRolePermissionDefined(role.Id, permission.Id, disposition.Id.ToString(), ns, null, forceReCache: true));
                Assert.DoesNotContain(await GetAllRolePermissionsAsync(repo, role.Id), rp => rp.Id == insertResult.Id);
            }
            finally
            {
                //DeleteRole is guarded to only ever affect a non-built-in role, and also deletes any leftover
                //Users.RolePermission rows for it (EfUsersRepository.DeleteRole's own doc comment) - safe/
                //idempotent even if RemoveRolePermission above already ran, and cleans up the role itself too.
                await repo.DeleteRole(role.Id);
            }
        }

        /// <summary>
        /// Exercises the "Namespace is null, PageId resolves to a real Pages.Page.Name" branch of
        /// <c>EfUsersRepository.ResolveResourceNameAsync</c>'s precedence - the cross-schema (Users -&gt; Pages)
        /// lookup that the SQLite reference implements via <c>Attach("pages.db", "pages_db")</c> (four call sites
        /// total across <see cref="ITwUsersRepository.InsertAccountPermission"/>/<see
        /// cref="ITwUsersRepository.InsertRolePermission"/>/<see cref="ITwUsersRepository.GetRolePermissionsPaged"/>/
        /// <see cref="ITwUsersRepository.GetAccountPermissionsPaged"/> - see this class's own remarks) and the EF
        /// implementation instead resolves via a plain, provider-portable LINQ query against
        /// <c>TightWikiDbContext.Pages_Pages</c> on the same <see cref="TightWikiDbContext"/> (no cross-database
        /// <c>ATTACH</c> needed under the consolidated schema, where Users/Pages are just two schemas in one
        /// database rather than two separate SQLite files).
        /// </summary>
        [Fact]
        public async Task InsertRolePermission_PageScoped_ResourceName_ResolvesToSeededPageName_CrossSchemaLookup()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            var marker = Guid.NewGuid().ToString("N");
            var roleName = $"TestRole_{marker}";
            await repo.InsertRole(roleName, "Test role for page-scoped Users.RolePermission grants.");
            var role = await repo.GetRoleByName(roleName);

            try
            {
                var navigation = TwNamespaceNavigation.CleanAndValidate(SeededPageName);
                var page = await pageRepo.GetPageInfoByNavigation(navigation)
                    ?? throw new Exception($"Could not find the seeded page at navigation '{navigation}'.");
                var pageIdParam = page.Id.ToString();

                var permission = (await repo.GetAllPermissions()).Single(p => p.Name == TightWiki.Plugin.TwPermission.Read.ToString());
                var disposition = (await repo.GetAllPermissionDispositions()).Single(d => d.Name == TightWiki.Plugin.TwPermissionDisposition.Allow.ToString());

                var insertResult = await repo.InsertRolePermission(role.Id, permission.Id, disposition.Id.ToString(), null, pageIdParam);
                Assert.NotNull(insertResult);
                Assert.Null(insertResult!.Namespace);
                Assert.Equal(pageIdParam, insertResult.PageId);
                Assert.Equal(page.Name, insertResult.ResourceName);

                var stored = (await GetAllRolePermissionsAsync(repo, role.Id)).Single(rp => rp.Id == insertResult.Id);
                Assert.Equal(pageIdParam, stored.PageId);
                Assert.Equal(page.Name, stored.ResourceName);

                await repo.RemoveRolePermission(insertResult.Id);
                Assert.DoesNotContain(await GetAllRolePermissionsAsync(repo, role.Id), rp => rp.Id == insertResult.Id);
            }
            finally
            {
                await repo.DeleteRole(role.Id);
            }
        }

        /// <summary>
        /// Purely read-only (no Users.RolePermission mutation at all): confirms <see
        /// cref="ITwUsersRepository.GetApparentRolePermissions(TwRoles)"/> really does just delegate to <see
        /// cref="ITwUsersRepository.GetApparentRolePermissions(string)"/> via <see cref="TwRoles.ToString"/> (both
        /// the SQLite reference's own C# overload and <c>EfUsersRepository</c>'s doc comment claim this) for every
        /// one of the five seeded roles, whatever their real Users.RolePermission content currently is on this
        /// shared/persistent database - safe to run any number of times, including concurrently with every
        /// mutating test elsewhere in this class (different Users.Role rows).
        /// </summary>
        [Fact]
        public async Task GetApparentRolePermissions_TwRolesOverload_DelegatesToStringOverload_ForEverySeededRole()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            static List<(string Permission, string PermissionDisposition, string? Namespace, string? PageId)> Normalize(List<TwApparentPermission> list)
                => list.Select(p => (p.Permission, p.PermissionDisposition, p.Namespace, p.PageId))
                    .OrderBy(t => t.Permission, StringComparer.Ordinal)
                    .ThenBy(t => t.PermissionDisposition, StringComparer.Ordinal)
                    .ThenBy(t => t.Namespace, StringComparer.Ordinal)
                    .ThenBy(t => t.PageId, StringComparer.Ordinal)
                    .ToList();

            foreach (var role in Enum.GetValues<TwRoles>())
            {
                var byEnum = await repo.GetApparentRolePermissions(role);
                var byName = await repo.GetApparentRolePermissions(role.ToString());

                Assert.Equal(Normalize(byName), Normalize(byEnum));
            }
        }

        [Fact]
        public async Task IsAccountPermissionDefined_InsertAccountPermission_GetAccountPermissionsPaged_GetApparentAccountPermissions_RemoveAccountPermission_RoundTrip_NamespaceScope()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            //Reuses the real seeded "admin" account rather than creating a new Users.Profile row - see this
            //class's own remarks (and UsersRepositoryRoleTests', which this mirrors) for why.
            var profile = await repo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
            var userId = profile.UserId;

            var marker = Guid.NewGuid().ToString("N");
            var ns = $"TestNamespace_{marker}";

            var permission = (await repo.GetAllPermissions()).Single(p => p.Name == TightWiki.Plugin.TwPermission.Edit.ToString());
            var disposition = (await repo.GetAllPermissionDispositions()).Single(d => d.Name == TightWiki.Plugin.TwPermissionDisposition.Allow.ToString());

            Assert.False(await repo.IsAccountPermissionDefined(userId, permission.Id, disposition.Id.ToString(), ns, null, forceReCache: true));

            TwInsertAccountPermissionResult? insertResult = null;
            try
            {
                insertResult = await repo.InsertAccountPermission(userId, permission.Id, disposition.Id.ToString(), ns, null);
                Assert.NotNull(insertResult);
                Assert.True(insertResult!.Id > 0);
                Assert.Equal(permission.Name, insertResult.Permission);
                Assert.Equal(disposition.Name, insertResult.PermissionDisposition);
                Assert.Equal(ns, insertResult.Namespace);
                Assert.Null(insertResult.PageId);
                Assert.Equal(ns, insertResult.ResourceName);

                Assert.True(await repo.IsAccountPermissionDefined(userId, permission.Id, disposition.Id.ToString(), ns, null, forceReCache: true));

                var stored = (await GetAllAccountPermissionsAsync(repo, userId)).Single(ap => ap.Id == insertResult.Id);
                Assert.Equal(permission.Name, stored.Permission);
                Assert.Equal(disposition.Name, stored.PermissionDisposition);
                Assert.Equal(ns, stored.Namespace);
                Assert.Equal(ns, stored.ResourceName);

                //Exactly one GetApparentAccountPermissions call in this whole test - same "no forceReCache, stale
                //after a later mutation" reasoning as GetApparentRolePermissions above (see this class's own
                //remarks); doubly so here since every test in this class that calls it shares the same cache key
                //(admin's Guid, not a fresh-per-test one like a GUID-suffixed role name).
                var apparent = await repo.GetApparentAccountPermissions(userId);
                Assert.Contains(apparent, a => a.Permission == permission.Name && a.PermissionDisposition == disposition.Name
                    && a.Namespace == ns && a.PageId == null);

                await repo.RemoveAccountPermission(insertResult.Id);
                Assert.False(await repo.IsAccountPermissionDefined(userId, permission.Id, disposition.Id.ToString(), ns, null, forceReCache: true));
                Assert.DoesNotContain(await GetAllAccountPermissionsAsync(repo, userId), ap => ap.Id == insertResult.Id);
            }
            finally
            {
                //Idempotent no-op if RemoveAccountPermission above already ran successfully; also covers the case
                //where InsertAccountPermission itself returned null (nothing to remove).
                if (insertResult != null)
                {
                    await repo.RemoveAccountPermission(insertResult.Id);
                }
            }
        }

        /// <summary>
        /// Account-permission counterpart to <see
        /// cref="InsertRolePermission_PageScoped_ResourceName_ResolvesToSeededPageName_CrossSchemaLookup"/> - see
        /// that test's own remarks for the cross-schema (Users -&gt; Pages) ResourceName resolution being
        /// exercised here.
        /// </summary>
        [Fact]
        public async Task InsertAccountPermission_PageScoped_ResourceName_ResolvesToSeededPageName_CrossSchemaLookup()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            var profile = await repo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
            var userId = profile.UserId;

            var navigation = TwNamespaceNavigation.CleanAndValidate(SeededPageName);
            var page = await pageRepo.GetPageInfoByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded page at navigation '{navigation}'.");
            var pageIdParam = page.Id.ToString();

            var permission = (await repo.GetAllPermissions()).Single(p => p.Name == TightWiki.Plugin.TwPermission.Moderate.ToString());
            var disposition = (await repo.GetAllPermissionDispositions()).Single(d => d.Name == TightWiki.Plugin.TwPermissionDisposition.Deny.ToString());

            TwInsertAccountPermissionResult? insertResult = null;
            try
            {
                insertResult = await repo.InsertAccountPermission(userId, permission.Id, disposition.Id.ToString(), null, pageIdParam);
                Assert.NotNull(insertResult);
                Assert.Null(insertResult!.Namespace);
                Assert.Equal(pageIdParam, insertResult.PageId);
                Assert.Equal(page.Name, insertResult.ResourceName);

                var stored = (await GetAllAccountPermissionsAsync(repo, userId)).Single(ap => ap.Id == insertResult.Id);
                Assert.Equal(pageIdParam, stored.PageId);
                Assert.Equal(page.Name, stored.ResourceName);

                await repo.RemoveAccountPermission(insertResult.Id);
                Assert.DoesNotContain(await GetAllAccountPermissionsAsync(repo, userId), ap => ap.Id == insertResult.Id);
            }
            finally
            {
                if (insertResult != null)
                {
                    await repo.RemoveAccountPermission(insertResult.Id);
                }
            }
        }

        /// <summary>
        /// Round-trips the four-member Users.AdminPwCheck state machine: <see
        /// cref="ITwUsersRepository.SetAdminPasswordClear"/> (deletes the single, possibly-absent row) &lt;-&gt;
        /// <see cref="TwAdminPasswordChangeState.NeedsToBeSet"/>, <see
        /// cref="ITwUsersRepository.SetAdminPasswordIsDefault"/> (row = 0) &lt;-&gt; <see
        /// cref="TwAdminPasswordChangeState.IsDefault"/>, <see cref="ITwUsersRepository.SetAdminPasswordIsChanged"/>
        /// (row = 1) &lt;-&gt; <see cref="TwAdminPasswordChangeState.HasBeenChanged"/>, each read back via <see
        /// cref="ITwUsersRepository.AdminPasswordStatus"/>.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Read-order is load-bearing here, not just style</b>: <c>EfUsersRepository.AdminPasswordStatus</c>'s
        /// own doc comment documents a deliberate, one-way "sticky true" cache - once it has ever observed <see
        /// cref="TwAdminPasswordChangeState.HasBeenChanged"/> (row value 1), the underlying
        /// <c>MemCache.Category.Configuration</c> entry (keyed only by the calling member name via
        /// <c>[CallerMemberName]</c> - process-wide, no per-test/per-role/per-user segmentation at all, unlike
        /// every other cache key in this class) is set and <b>never</b> invalidated by
        /// <see cref="ITwUsersRepository.SetAdminPasswordClear"/>/<see
        /// cref="ITwUsersRepository.SetAdminPasswordIsDefault"/>/<see cref="ITwUsersRepository.SetAdminPasswordIsChanged"/>
        /// (none of those three touch the cache - they only write the real, single-row Users.AdminPwCheck table)
        /// - so every later <see cref="ITwUsersRepository.AdminPasswordStatus"/> call in this same process would
        /// return <see cref="TwAdminPasswordChangeState.HasBeenChanged"/> unconditionally from then on, regardless
        /// of what the row actually holds. The <see cref="TwAdminPasswordChangeState.NeedsToBeSet"/> (row value
        /// <see langword="null"/>)/<see cref="TwAdminPasswordChangeState.IsDefault"/> (row value 0) read paths
        /// never populate that cache (confirmed by inspection: only the <c>value == 1</c> branch calls
        /// <c>MemCache.Set</c>), so this test verifies both of those transitions first/repeatedly, and only
        /// observes <see cref="TwAdminPasswordChangeState.HasBeenChanged"/> once, last.
        /// </para>
        /// <para>
        /// <b>The final cleanup statement below is required for SqlServer/Postgres re-runnability, not optional
        /// tidiness</b>: unlike SQLite (where <see cref="TwEngineFixture"/>'s constructor unconditionally
        /// re-copies pristine seed <c>.db</c> files - including <c>Data/users.db</c>, which already ships
        /// Users.AdminPwCheck.Value = 1 - on every single <c>dotnet test</c> process, wiping out whatever this
        /// test left behind), <see cref="TightWiki.Test.Library.MockWikiEngineArtifacts"/>'s own
        /// <c>SQLSERVER_PROVIDER</c>/<c>POSTGRES_PROVIDER</c> branch calls <see
        /// cref="ITwUsersRepository.ValidateEncryptionAndCreateAdminUser"/> unconditionally at fixture-construction
        /// time, against the same long-lived Docker container across every test run - and that method itself
        /// calls <see cref="ITwUsersRepository.AdminPasswordStatus"/> internally, <i>before any test in this
        /// process runs</i>, to decide whether its admin-bootstrap body needs to execute at all. If this test's
        /// previous run had left the row at 1, that fixture-construction-time read would immediately re-poison the
        /// sticky cache before this test's own first assertion even runs, breaking the NeedsToBeSet/IsDefault
        /// checks above on the very next run. Restoring the row to "no row" (NeedsToBeSet) here instead makes the
        /// next run's fixture bootstrap re-execute its own (confirmed idempotent - see
        /// <c>MockWikiEngineArtifacts</c>' own doc comment) admin-provisioning flow, ending at IsDefault (0)
        /// again - a safe, self-healing steady state. This final call is deliberately <b>not</b> re-verified via
        /// <see cref="ITwUsersRepository.AdminPasswordStatus"/> afterward - see the previous paragraph for why that
        /// read would just return the already-poisoned <see cref="TwAdminPasswordChangeState.HasBeenChanged"/>
        /// value from this same test's own assertion above, misleadingly.
        /// </para>
        /// </remarks>
        [Fact]
        public async Task AdminPasswordStatus_SetAdminPasswordClear_SetAdminPasswordIsDefault_SetAdminPasswordIsChanged_StateMachine_RoundTrip()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            try
            {
                await repo.SetAdminPasswordClear();
                Assert.Equal(TwAdminPasswordChangeState.NeedsToBeSet, await repo.AdminPasswordStatus());

                await repo.SetAdminPasswordIsDefault();
                Assert.Equal(TwAdminPasswordChangeState.IsDefault, await repo.AdminPasswordStatus());

                await repo.SetAdminPasswordClear();
                Assert.Equal(TwAdminPasswordChangeState.NeedsToBeSet, await repo.AdminPasswordStatus());

                await repo.SetAdminPasswordIsChanged();
                Assert.Equal(TwAdminPasswordChangeState.HasBeenChanged, await repo.AdminPasswordStatus());
            }
            finally
            {
                //Restores the row to "no row" (NeedsToBeSet) - required for SqlServer/Postgres re-runnability, not
                //just cleanup. Deliberately not re-verified via AdminPasswordStatus() - see this test's own remarks.
                //Runs in a finally block - same pattern as the role/permission cleanup calls elsewhere in this
                //class - so a transient failure on this specific call (e.g. a dropped connection) can't skip the
                //cache-poisoning restore and leave the shared database's Users.AdminPwCheck row at "1" for the
                //next process's fixture-construction-time bootstrap to pick up.
                await repo.SetAdminPasswordClear();
            }
        }
    }
}
