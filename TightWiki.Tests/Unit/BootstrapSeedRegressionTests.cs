using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Regression tests for four specific, already-fixed bugs uncovered during the Database-Providers-Plan.md
    /// implementation phase (before the dedicated Database-Providers-Testing-Plan.md testing initiative existed) -
    /// unlike every sibling <c>*RepositoryTests</c> class in this project, which covers a repository interface's
    /// members scenario-by-scenario, each test here targets the exact narrow scenario one specific historical bug
    /// broke, not the surrounding functionality in general, so a future regression of the same root cause fails
    /// loudly instead of silently reappearing:
    /// <list type="bullet">
    /// <item><description><b>Bug 1</b> (commit b354acfe) - <c>RolePermissionConfiguration</c> shipped with no
    /// <c>HasData</c> block at all, so a freshly migrated MSSQL/Postgres database seeded zero
    /// Users.RolePermission rows for every built-in role, including Administrator. See
    /// <see cref="GetApparentRolePermissions_ForEverySeededBuiltInRole_MatchesCanonicalPermissionDefaultsScript"/>.</description></item>
    /// <item><description><b>Bug 2</b> (commit 48039b86) - a freshly bootstrapped SQL Server/Postgres admin
    /// Identity user was never granted a Users.AccountRole row linking it to the built-in "Administrator" role, so
    /// it could log in but held zero permissions anywhere. See
    /// <see cref="Bootstrap_AdminAccount_IsAMemberOfTheAdministratorRole"/>.</description></item>
    /// <item><description><b>Bug 3</b> (commit 069507f1) - a FOREIGN KEY conflict in the SQL Server admin
    /// bootstrap sequence, caused by two different startup code paths resolving the admin Identity user under two
    /// different usernames. See <see cref="Bug3_AdminBootstrapFkConflict_CodeReviewOnly_NotLiveReproducible"/> for
    /// why this is a documented code-review note rather than a live test.</description></item>
    /// <item><description><b>Bug 4</b> (commit 0ac968b2) - the <c>SET IDENTITY_INSERT ... OFF</c> half of the
    /// helper that brackets identity-column inserts during SQL Server page delete/restore was not reliably
    /// re-armed for a subsequent call. See
    /// <see cref="SqlServer_RepeatedDeleteRestoreCycles_IdentityInsertToggle_NeverLeaksAcrossCalls"/>.</description></item>
    /// </list>
    /// Written entirely against the provider-agnostic <see cref="ITwUsersRepository"/>/<see cref="ITwPageRepository"/>
    /// interfaces - no <c>#if SQLITE_PROVIDER</c>/<c>#elif SQLSERVER_PROVIDER</c>/<c>#elif POSTGRES_PROVIDER</c>
    /// branching anywhere in this file, even for Bug 4 (SQL-Server-only in substance): that test instead branches
    /// at runtime on <see cref="ITwDatabaseManager.GetType"/>'s <see cref="Type.Name"/>, exactly as this task's own
    /// brief specifies, so the same compiled test binary exists (and is visibly reported as a documented no-op,
    /// not silently absent) under every provider. The same compiled test code runs three times: <c>dotnet test</c>
    /// (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>, <c>dotnet test -p:DataProvider=Postgres</c>
    /// (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </summary>
    /// <remarks>
    /// Runs against the shared, persistent test database (Database-Providers-Testing-Plan.md chapter 5.3),
    /// concurrently with every other xunit collection in this assembly - same reasoning as every sibling
    /// repository test class. Bugs 1 and 2 are read-only checks against data that is never mutated by any test in
    /// this assembly (seeded role permissions/the seeded admin account's role membership - see
    /// <see cref="UsersRepositoryRoleTests"/>'s and <see cref="UsersRepositoryPermissionAuthTests"/>'s own remarks
    /// for why every other test class in this project already avoids touching either), so no <c>finally</c>
    /// cleanup is needed for either. Bug 4 creates and permanently deletes its own GUID-named pages, mirroring
    /// <see cref="PageRepositoryDeleteRestoreTests"/>'s own conventions.
    /// </remarks>
    [Collection("Bootstrap Seed Regression Tests")]
    public class BootstrapSeedRegressionTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// Bug 1 (commit b354acfe, "Fix missing RolePermission seed data in shared EF Core model"). Before the
        /// fix, <c>TightWiki.Data.EfCore.Configurations.Users.RolePermissionConfiguration</c> had no
        /// <c>HasData</c> block at all (its siblings - Role/Permission/PermissionDisposition - all did), so a
        /// freshly migrated MSSQL/Postgres database ended up with an entirely empty Users.RolePermission table -
        /// no role, including Administrator, had any permission grant. This test asserts every one of the 26 grants
        /// the fix's <c>HasData</c> block transcribes verbatim from the canonical SQLite reference,
        /// <c>Scripts/Initialization/Versions/2.26.0/^007^Users^CreatePermissionDefaults.sql</c>, is present: every
        /// grant is "Allow", every permission is granted twice over (once <c>Namespace=null/PageId="*"</c>, once
        /// <c>Namespace="*"/PageId=null</c> - the source script's own two mirrored <c>SELECT</c>/<c>UNION SELECT</c>
        /// halves) - Administrator gets all five permissions, Anonymous/Member get Read only, Moderator gets
        /// Read/Edit/Delete/Moderate, Contributor gets Read/Edit. Would fail on the pre-fix code (every list below
        /// would be empty, failing every single <see cref="Assert.Contains{T}(T, IEnumerable{T})"/> below) and
        /// passes on the current, fixed code.
        /// <para>
        /// <b>Deliberately asserts containment, not an exact row count</b> - confirmed during verification: SQLite's
        /// live/cumulative reference state also carries two further Create-permission grants for
        /// Moderator/Contributor from a separate, unrelated, much older script
        /// (<c>Scripts/Initialization/Versions/2.27.1/^001^Users^AddCreatePermissionToContributor.sql</c>/
        /// <c>...Moderator.sql</c>, commit 2040d943, dated over a year before this bug's own fix commit), which the
        /// EF <c>HasData</c> block this test targets does not mirror (confirmed by reading the fix's own diff - its
        /// 26 rows stop at Read/Edit/Delete/Moderate for Moderator and Read/Edit for Contributor, no Create row for
        /// either). That gap is a real, currently-live SQLite/EF seed-data discrepancy, but it is not this bug
        /// (b354acfe was "the whole table is empty", not "two specific rows are missing") and fixing it is out of
        /// this task's scope ("Neopravuj žádný produkční kód"). Asserting exact-count equality here would make this
        /// test fail against SQLite (28 real rows) for a reason unrelated to the bug it exists to guard against, so
        /// this only asserts that the 26-row canonical baseline is present as a subset - true on SQLite (superset)
        /// and on the fixed SqlServer/Postgres EF seed (exact set), false only when the fix regresses.
        /// </para>
        /// </summary>
        [Fact]
        public async Task GetApparentRolePermissions_ForEverySeededBuiltInRole_MatchesCanonicalPermissionDefaultsScript()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var expectedPermissionsByRole = new Dictionary<TwRoles, string[]>
            {
                [TwRoles.Administrator] = [
                    TightWiki.Plugin.TwPermission.Read.ToString(), TightWiki.Plugin.TwPermission.Edit.ToString(),
                    TightWiki.Plugin.TwPermission.Delete.ToString(), TightWiki.Plugin.TwPermission.Moderate.ToString(),
                    TightWiki.Plugin.TwPermission.Create.ToString()
                ],
                [TwRoles.Anonymous] = [TightWiki.Plugin.TwPermission.Read.ToString()],
                [TwRoles.Member] = [TightWiki.Plugin.TwPermission.Read.ToString()],
                [TwRoles.Moderator] = [
                    TightWiki.Plugin.TwPermission.Read.ToString(), TightWiki.Plugin.TwPermission.Edit.ToString(),
                    TightWiki.Plugin.TwPermission.Delete.ToString(), TightWiki.Plugin.TwPermission.Moderate.ToString()
                ],
                [TwRoles.Contributor] = [TightWiki.Plugin.TwPermission.Read.ToString(), TightWiki.Plugin.TwPermission.Edit.ToString()],
            };

            foreach (var (role, permissionNames) in expectedPermissionsByRole)
            {
                var apparent = await repo.GetApparentRolePermissions(role);

                //Every seeded grant below is "Allow" - see this test's own doc comment for the source script this
                //is transcribed from, and why this checks containment of the canonical baseline rather than an
                //exact row count.
                Assert.True(apparent.All(a => a.PermissionDisposition == TightWiki.Plugin.TwPermissionDisposition.Allow.ToString()),
                    $"Expected every seeded grant for role '{role}' to be 'Allow'.");

                foreach (var permissionName in permissionNames)
                {
                    Assert.Contains(apparent, a => a.Permission == permissionName && a.Namespace == null && a.PageId == "*");
                    Assert.Contains(apparent, a => a.Permission == permissionName && a.Namespace == "*" && a.PageId == null);
                }
            }
        }

        /// <summary>
        /// Bug 2 (commit 48039b86, "Seed Administrator AccountRole for freshly created SQL Server admin"). Before
        /// the fix, a freshly bootstrapped SQL Server/Postgres admin Identity user got the "Administrator"
        /// ASP.NET Identity claim but zero Users.AccountRole rows - <c>TwSessionState.IsAdministrator</c>/
        /// <c>HoldsPermission</c> never consult Identity claims at all, only a Users.AccountRole -&gt; Users.Role
        /// join (<c>EfUsersRepository.IsUserMemberOfAdministrators</c>), so the account could authenticate but held
        /// no permissions on anything, including the public home page. The fix added
        /// <c>EfUsersRepository.EnsureAdministratorRoleMembership</c>, called at the end of
        /// <c>ValidateEncryptionAndCreateAdminUserAsync</c> - which every SQL Server/Postgres run of
        /// <see cref="TwEngineFixture"/> already exercises once, at fixture-construction time, against this
        /// project's shared/persistent Docker container (<see cref="TightWiki.Test.Library.MockWikiEngineArtifacts"/>'s
        /// own <c>SQLSERVER_PROVIDER</c>/<c>POSTGRES_PROVIDER</c> branch calls
        /// <see cref="ITwUsersRepository.ValidateEncryptionAndCreateAdminUser"/> unconditionally). This test
        /// asserts the observable end state that call is supposed to leave behind: the real seeded admin account
        /// (<see cref="Constants.DEFAULTACCOUNT"/>) really is a member of the "Administrator" role, on every
        /// provider (on SQLite this was never broken - <c>Defaults/defaults.db</c> ships the AccountRole row
        /// pre-seeded - so this test is a genuine no-op assertion there, not a skip, and still guards against a
        /// future regression that removes/breaks that pre-seeded row).
        /// </summary>
        [Fact]
        public async Task Bootstrap_AdminAccount_IsAMemberOfTheAdministratorRole()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var profile = await repo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
            var adminRole = await repo.GetRoleByName(TwRoles.Administrator.ToString());

            Assert.True(await repo.IsAccountAMemberOfRole(profile.UserId, adminRole.Id, forceReCache: true),
                $"Expected the seeded '{Constants.DEFAULTACCOUNT}' account to be a member of the '{adminRole.Name}' role.");

            var membership = await repo.GetAccountRoleMembershipPaged(profile.UserId, 1);
            Assert.Contains(membership, m => m.RoleId == adminRole.Id && m.Name == adminRole.Name);
        }

        /// <summary>
        /// Bug 3 (commit 069507f1, "Fix admin bootstrap FK conflict in SqlServerDatabaseManager") - documented as
        /// a code-review-only note per this task's own brief, not live-reproduced, for reasons confirmed by
        /// reading both the commit and the current code:
        /// <list type="bullet">
        /// <item><description><b>What was broken:</b> <c>SqlServerDatabaseManager.EnsureAdminUser</c> (called
        /// from <c>ApplyAllSeedData</c>, itself called once during startup seeding) looked up/created the
        /// bootstrap Identity user by the literal username <c>"admin"</c>, while
        /// <c>EfUsersRepository.ValidateEncryptionAndCreateAdminUserAsync</c> - run moments later, from the same
        /// DI scope, mirroring <c>Program.cs</c>'s own two-call sequence - looks up/creates it by
        /// <c>Constants.DEFAULTUSERNAME</c> ("admin@tightwiki.com"). Under SQL Server this produced two distinct
        /// <c>IdentityUser</c> rows, and the second call's <c>SetProfileUserId</c> then tried to repoint the
        /// already-seeded Profile.UserId from the first Id to the second, violating the FK from every
        /// Pages/DeletedPages/DeletedPageRevisions row still referencing the first Id (an UPDATE of a primary key
        /// still referenced by existing FK rows).</description></item>
        /// <item><description><b>The fix</b> (verified by reading the current
        /// <c>TightWiki.Data.EfCore.SqlServer/SqlServerDatabaseManager.cs</c>): <c>EnsureAdminUser</c> now looks
        /// up/creates the Identity user by <c>Constants.DEFAULTUSERNAME</c> too, so both bootstrap paths resolve
        /// to the same Identity user and the later <c>SetProfileUserId</c> call is a no-op update instead of a
        /// repoint. No literal <c>"admin"</c> username lookup remains anywhere in that file.</description></item>
        /// <item><description><b>Why this cannot be meaningfully live-tested at the repository level, per this
        /// task's own explicit fallback instruction:</b> the FK conflict can only manifest the very first time
        /// <c>EnsureAdminUser</c> and <c>ValidateEncryptionAndCreateAdminUserAsync</c> ever run in sequence
        /// against a truly fresh, unseeded database - exactly the one-time bootstrap
        /// <see cref="TightWiki.Test.Library.MockWikiEngineArtifacts"/>'s constructor already performs, gated on
        /// its own <c>wasDatabaseUpgraded</c> flag, on the very first construction against a given database. This
        /// project's fixture deliberately reuses a shared, already-migrated, already-seeded Docker container
        /// across every test run (Database-Providers-Testing-Plan.md chapter 5.3) - by the time any test in this
        /// assembly runs, that one-time bootstrap window has already closed and cannot be reopened without
        /// dropping/re-migrating the shared database, which would be destructive to every other concurrently-
        /// running test class and is explicitly out of this task's scope. Compounding this,
        /// <c>SqlServerDatabaseManager.EnsureAdminUser</c> is <c>private</c> - there is no way to invoke just that
        /// half of the sequence directly to construct an artificial reproduction, either.</description></item>
        /// </list>
        /// </summary>
        [Fact(Skip = "Bug 3 is a one-time, fresh-database startup-sequence bug (see this test's own doc comment) " +
            "that cannot be reproduced against this project's shared, already-seeded test database without " +
            "dropping/re-migrating it - documented as a code-review-only regression note instead, per this task's " +
            "own explicit fallback instruction. Verified by reading the current SqlServerDatabaseManager.cs: no " +
            "literal \"admin\" username lookup remains, both bootstrap paths resolve via Constants.DEFAULTUSERNAME.")]
        public Task Bug3_AdminBootstrapFkConflict_CodeReviewOnly_NotLiveReproducible()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Bug 4 (commit 0ac968b2, "...and fix the IDENTITY_INSERT finally block", part of "Implement final 15
        /// EfPageRepository methods"). <c>EfPageRepository.SaveChangesWithIdentityInsertAsync</c> is the helper
        /// this landed: SQL Server's EF Core provider does not automatically wrap
        /// <c>SET IDENTITY_INSERT ... ON/OFF</c> around a <c>SaveChangesAsync</c> call that inserts an explicit
        /// value into a store-generated identity column (needed by <see cref="ITwPageRepository.RestoreDeletedPageByPageId"/>/
        /// <see cref="ITwPageRepository.MovePageToDeletedById"/>'s inverse, which must preserve the original
        /// Pages.Page/PageFile/PageComment Id values verbatim when moving a row back out of the
        /// <c>ValueGeneratedNever()</c>-configured DeletedPages schema). The <c>OFF</c> half runs inside a
        /// <c>finally</c> block specifically so a failed insert still re-arms IDENTITY_INSERT before the next
        /// unrelated call reuses the connection/table - this test exercises that <c>finally</c> block directly by
        /// running several delete/restore round trips back-to-back, across several pages, which is exactly the
        /// scenario this task's own brief calls out as the one a broken <c>finally</c> block (missing entirely, or
        /// turning IDENTITY_INSERT off on the wrong table) would fail on the second attempt, not necessarily the
        /// first: SQL Server only allows IDENTITY_INSERT to be ON for one table at a time per session, so a
        /// never-cleared toggle from an earlier call surfaces as a failure on a <i>later</i>, otherwise-unrelated
        /// call. SQL-Server-only in substance (IDENTITY_INSERT has no SQLite/Postgres equivalent - Postgres'
        /// <c>GENERATED BY DEFAULT AS IDENTITY</c> accepts an explicit value with no session-state toggle at all,
        /// per <c>SaveChangesWithIdentityInsertAsync</c>'s own doc comment), so this branches at runtime on
        /// <see cref="ITwDatabaseManager.GetType"/> rather than a compile-time <c>#if</c>, per this task's own
        /// instruction - a documented no-op (not a skip) under SQLite/Postgres, where there is nothing to exercise.
        /// </summary>
        [Fact]
        public async Task SqlServer_RepeatedDeleteRestoreCycles_IdentityInsertToggle_NeverLeaksAcrossCalls()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;

            //Runtime provider check (not a compile-time #if), per this task's own instruction - IDENTITY_INSERT is
            //a SQL-Server-only concept (see this test's own doc comment), so this is a deliberate, documented
            //no-op under SQLite/Postgres rather than exercising anything.
            if (databaseManager.GetType().Name != "SqlServerDatabaseManager")
            {
                return;
            }

            var pageRepo = databaseManager.PageRepository;
            var usersRepo = databaseManager.UsersRepository;

            var admin = await usersRepo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");

            var pages = new List<TwPage>();
            try
            {
                //Several distinct pages, each with its own Pages.Page identity value - gives the IDENTITY_INSERT
                //ON/OFF toggle (scoped per-table, not per-row) several distinct rows to preserve across calls.
                for (var i = 0; i < 3; i++)
                {
                    var navigation = TwNamespaceNavigation.CleanAndValidate($"ZzzIdentityInsertRegression_{i}_{Guid.NewGuid():N}");
                    var page = new TwPage
                    {
                        Name = navigation,
                        Navigation = navigation,
                        Body = $"IDENTITY_INSERT finally-block regression content {i} {Guid.NewGuid():N}.\r\n",
                        Description = "BootstrapSeedRegressionTests scratch page (Bug 4).",
                        CreatedByUserId = admin.UserId,
                        ModifiedByUserId = admin.UserId,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                    };
                    page.Id = await pageRepo.UpsertPage(fixture.Artifacts.Engine, fixture.Artifacts.Localizer, page, fixture.CreateWikiSession());
                    pages.Add(page);
                }

                //Three delete/restore rounds across all three pages: a broken finally block would leave
                //IDENTITY_INSERT stuck ON for Pages.Page after the very first restore below, so the second
                //restore's own "SET IDENTITY_INSERT [Pages].[Page] ON" would fail - this gives that failure
                //several chances to surface rather than relying on exactly one.
                for (var round = 0; round < 3; round++)
                {
                    foreach (var page in pages)
                    {
                        await pageRepo.MovePageToDeletedById(page.Id, admin.UserId);
                        Assert.NotNull(await pageRepo.GetDeletedPageById(page.Id));

                        await pageRepo.RestoreDeletedPageByPageId(page.Id);
                        var restored = await pageRepo.GetPageRevisionInfoById(page.Id);
                        Assert.NotNull(restored);
                        Assert.Equal(page.Id, restored!.Id);
                    }
                }
            }
            finally
            {
                foreach (var page in pages)
                {
                    //Idempotent regardless of which delete/restore state the loop above left each page in - same
                    //pattern as PageRepositoryDeleteRestoreTests.DeleteTestPageAsync.
                    await pageRepo.MovePageToDeletedById(page.Id, admin.UserId);
                    await pageRepo.PurgeDeletedPageByPageId(page.Id);
                }
            }
        }
    }
}
