using TightWiki.Library;
using TightWiki.Plugin;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the role CRUD/membership slice of
    /// <see cref="TightWiki.Plugin.Interfaces.Repository.ITwUsersRepository"/> (51 members total - far too many for
    /// one test file, per this task's own brief). Covers exactly the 12-member "role CRUD/membership" category
    /// <c>TightWiki.Data.EfCore.Repositories.EfUsersRepository</c>'s own class-level remarks call out as
    /// implemented together in phase 2b.9: <see cref="ITwUsersRepository.IsAccountAMemberOfRole"/>, <see
    /// cref="ITwUsersRepository.DeleteRole"/>, <see cref="ITwUsersRepository.InsertRole"/>, <see
    /// cref="ITwUsersRepository.DoesRoleExist"/>, <see cref="ITwUsersRepository.AutoCompleteRole"/>, <see
    /// cref="ITwUsersRepository.AddRoleMemberByname"/>, <see cref="ITwUsersRepository.AddRoleMember"/>, <see
    /// cref="ITwUsersRepository.AddAccountMembership"/>, <see cref="ITwUsersRepository.RemoveRoleMember"/>, <see
    /// cref="ITwUsersRepository.GetRoleByName"/>, <see cref="ITwUsersRepository.GetAllRoles"/> and <see
    /// cref="ITwUsersRepository.GetRoleMembersPaged"/> - plus <see
    /// cref="ITwUsersRepository.GetAccountRoleMembershipPaged"/> (from the separate permissions category, phase
    /// 2b.10), pulled in here anyway because it reads back the exact same Users.AccountRole rows this class's own
    /// membership mutations create, and there is no more natural place to exercise it. Permissions (Account/Role
    /// permission grants), profiles (account CRUD/avatar/anonymize), and the admin-bootstrap/Identity-claims
    /// members are all deliberately out of scope - see this task's own brief; those are covered by the sibling
    /// <c>UsersRepositoryPermissionTests</c>/<c>UsersRepositoryProfileTests</c> tasks, not yet written as of this
    /// class.
    /// <para>
    /// Obtained through <see cref="TwEngineFixture"/> exactly like <c>ConfigurationRepositoryTests</c>/
    /// <c>LoggingRepositoryTests</c>/<c>StatisticsRepositoryTests</c>/<c>EmojiRepositoryTests</c>/
    /// <c>DefaultsRepositoryTests</c> get their own repositories. Written entirely against the provider-agnostic
    /// interface - no <c>#if SQLITE_PROVIDER</c>/<c>#elif SQLSERVER_PROVIDER</c>/<c>#elif POSTGRES_PROVIDER</c>
    /// branching here. The same compiled test code runs three times: <c>dotnet test</c> (SQLite, default),
    /// <c>dotnet test -p:DataProvider=SqlServer</c>, <c>dotnet test -p:DataProvider=Postgres</c>
    /// (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </para>
    /// </summary>
    /// <remarks>
    /// <para>
    /// These run against the shared, persistent test database (chapter 5.3 - not a fresh-empty database, and not
    /// per-test transaction rollback), concurrently with every other xunit collection in this assembly (default
    /// xunit parallelization: collections run in parallel with each other, tests within one collection run
    /// sequentially - same reasoning as every sibling repository test class above). The five seeded roles this
    /// class reads (Administrator/Member/Contributor/Moderator/Anonymous - verified by inspection of
    /// <c>Scripts/Initialization/Versions/2.26.0/^002^Users^Role.sql</c> on the SQLite side and
    /// <c>Configurations/Users/RoleConfiguration.cs</c>'s <c>HasData</c> on the EF side, both <c>IsBuiltIn = true</c>
    /// with identical names/descriptions but <b>not</b> identical numeric Ids across providers - see that EF
    /// config's own doc comment) are never mutated by anything here; every mutating test below instead creates its
    /// own GUID-suffixed "TestRole_"-prefixed role, or grants/revokes a role membership for the real seeded
    /// <c>admin</c> account (see the next paragraph for why), removing/detaching whatever it added again in a
    /// <c>finally</c> block, making each safe to re-run any number of times against the shared/persistent database -
    /// except <see cref="DeleteRole_BuiltInSeededRole_IsANoOp_RoleSurvivesUnchanged"/>, which reads/no-ops against
    /// one of the five real seeded roles directly but is provably incapable of mutating it (see that test's own
    /// remarks).
    /// </para>
    /// <para>
    /// <b>Test-user choice, and why this deliberately does not create a new Users.Profile row (confirmed by a real
    /// regression caught while writing this class, not just a theoretical concern):</b> <see
    /// cref="ITwUsersRepository.AddRoleMember"/>/<see cref="ITwUsersRepository.AddRoleMemberByname"/>/<see
    /// cref="ITwUsersRepository.AddAccountMembership"/> all require an existing Users.Profile row (with a matching
    /// AspNetUsers row) to attach a Users.AccountRole membership to. An earlier version of this class created one
    /// via the existing <see cref="TwEngineFixture.CreateUserAndProfile"/> helper (the same one <see
    /// cref="TwEngineFixture"/>'s own constructor would use for its own bootstrap account, if anything ever actually
    /// called it - confirmed by inspection, per <c>StatisticsRepositoryTests</c>' own remarks, that it does not).
    /// That approach was reverted: <see cref="ITwUsersRepository"/> has no member to delete a Users.Profile row at
    /// all (deleting an account is squarely profile-CRUD, out of scope for this task), and
    /// <c>TightWiki.Plugin.Default.StandardFunctions.UsersFunctions.ProfileList</c>/<c>ProfileGlossary</c> (backed
    /// by <see cref="ITwUsersRepository.GetAllPublicProfilesPaged"/>, which applies no filtering of any kind) list
    /// <b>every</b> Users.Profile row unconditionally - so a leftover test-created profile does not stay contained
    /// to this class at all, it silently breaks <c>MarkupTests</c>'/<c>FullPageTests</c>' own golden
    /// <c>TestProfileList_*</c>/<c>TestProfileGlossary_*</c> <c>.wiki.expected</c> files (which bake in an exact,
    /// hand-verified enumeration of every profile) the moment both run in the same process, sharing the same
    /// on-disk SQLite files/live database - exactly what happened, and was caught, running the full <c>dotnet
    /// test</c> suite with the earlier version of this class. Reusing the real seeded <c>admin</c> account (<see
    /// cref="TightWiki.Library.Constants.DEFAULTACCOUNT"/> - the same one already baked into those golden files as
    /// the sole pre-existing profile, and already read read-only elsewhere in this test project, e.g.
    /// <c>MockWikiEngineArtifacts.GetMockPage</c>) creates zero new Profile rows, so this concern does not apply.
    /// Mutating <c>admin</c>'s Users.AccountRole membership is otherwise safe here: nothing in this assembly's
    /// rendering path actually consults it - every <c>MarkupTests</c>/<c>FullPageTests</c> case renders through
    /// <c>TwDummySessionState</c>, whose <c>HoldsPermission</c>/<c>RequirePermission</c>/<c>RequireAdminPermission</c>
    /// members are all hardcoded to succeed unconditionally, never touching the database - and no other test in this
    /// assembly reads or writes Users.AccountRole for any user at all (confirmed by inspection: this is the only
    /// "role CRUD/membership" test class). Each mutating test below also defensively removes its own target
    /// membership <i>before</i> its first assertion (not just after, in <c>finally</c>) precisely so a prior crashed
    /// run - or, in principle, an earlier manual admin action against this dedicated test-only container - can never
    /// make the "brand-new membership" sanity check itself unreliable.
    /// </para>
    /// </remarks>
    [Collection("Users Repository Role Tests")]
    public class UsersRepositoryRoleTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        private static readonly string[] _seededRoleNames = ["Administrator", "Member", "Contributor", "Moderator", "Anonymous"];

        [Fact]
        public async Task GetAllRoles_GetRoleByName_DoesRoleExist_AutoCompleteRole_ReturnConsistentSeededRoles()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            //GetAllRoles.sql only selects Id/Name/Description (no IsBuiltIn column) - EfUsersRepository.GetAllRoles'
            //own doc comment confirms this is deliberately mirrored, not an oversight - so IsBuiltIn is not asserted
            //here at all, only via GetRoleByName below (whose SQL/LINQ select list does include it).
            var allRoles = await repo.GetAllRoles();
            Assert.True(allRoles.Count >= _seededRoleNames.Length,
                $"Expected at least the {_seededRoleNames.Length} seeded roles, found {allRoles.Count}.");

            foreach (var name in _seededRoleNames)
            {
                Assert.Contains(allRoles, r => r.Name == name);
                Assert.True(await repo.DoesRoleExist(name), $"Expected seeded role '{name}' to exist.");

                var role = await repo.GetRoleByName(name);
                Assert.Equal(name, role.Name);
                Assert.True(role.IsBuiltIn, $"Expected seeded role '{name}' to be built-in.");
                Assert.False(string.IsNullOrWhiteSpace(role.Description));

                //Exact-case substring of the role's own name - always matches regardless of a provider's collation
                //case-sensitivity (Postgres' default collation is case-sensitive, unlike SQLite's COLLATE NOCASE -
                //see DoesRoleExist's own remarks on EfUsersRepository - so this deliberately never relies on
                //case-insensitive matching).
                var autoComplete = await repo.AutoCompleteRole(name);
                Assert.Contains(autoComplete, r => r.Name == name);
            }

            var unknownName = $"TestRole_NoSuchRole_{Guid.NewGuid():N}";
            Assert.False(await repo.DoesRoleExist(unknownName));
        }

        [Fact]
        public async Task InsertRole_DoesRoleExist_GetRoleByName_AutoCompleteRole_GetAllRoles_RoundTrip_ThenDeleteRole()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var marker = Guid.NewGuid().ToString("N");
            var name = $"TestRole_{marker}";
            var description = $"TestRole description {marker}";

            //InsertRole's own bool return value is deliberately not asserted here - documented divergence
            //(EfUsersRepository.InsertRole's own doc comment): the SQLite reference's dead-code "?? false" always
            //returns false, insert success or not, while EfUsersRepository correctly returns whether a row was
            //actually inserted. Success is instead verified the same way every real caller already has to: via
            //DoesRoleExist immediately below.
            await repo.InsertRole(name, description);

            try
            {
                Assert.True(await repo.DoesRoleExist(name));

                var role = await repo.GetRoleByName(name);
                Assert.Equal(name, role.Name);
                Assert.Equal(description, role.Description);
                //A role created via InsertRole is never built-in (InsertRole.sql's own literal "0" /
                //EfUsersRepository.InsertRole's hardcoded IsBuiltIn = false - there is no UI path to create a
                //built-in role), which is also what makes the DeleteRole call below not a no-op.
                Assert.False(role.IsBuiltIn);

                var allRoles = await repo.GetAllRoles();
                Assert.Contains(allRoles, r => r.Id == role.Id && r.Name == name);

                //marker alone (not the full "TestRole_" prefix) as the search text - still an exact-case substring
                //of the role's own name, unique enough across the whole seeded/test role set that no other role
                //could ever match it, so AutoCompleteRole's own "LIMIT 25"/".Take(25)" cap can never exclude it.
                var autoComplete = await repo.AutoCompleteRole(marker);
                Assert.Contains(autoComplete, r => r.Id == role.Id && r.Name == name);

                await repo.DeleteRole(role.Id);
                Assert.False(await repo.DoesRoleExist(name));
            }
            finally
            {
                //Idempotent no-op if the DeleteRole call above already ran successfully - re-resolves the Id by
                //name rather than reusing a captured variable, since a failure inside GetRoleByName/InsertRole
                //itself (further up) would mean no Id was ever successfully captured in the first place.
                if (await repo.DoesRoleExist(name))
                {
                    var role = await repo.GetRoleByName(name);
                    await repo.DeleteRole(role.Id);
                }
            }
        }

        /// <summary>
        /// <see cref="ITwUsersRepository.DeleteRole"/> is guarded to only ever affect a role that both exists and
        /// is not <c>IsBuiltIn</c> (see <c>EfUsersRepository.DeleteRole</c>'s own doc comment, and
        /// <c>DeleteRole.sql</c>'s own three repeated "... NOT IN (SELECT ... WHERE IsBuiltIn = 1)" subqueries) -
        /// every one of the five seeded roles is <c>IsBuiltIn = true</c> (this class's own remarks), so calling it
        /// against any of them is a structural no-op on every provider, safe to run live against the shared/
        /// persistent database without a <c>finally</c> block: there is nothing this call could ever mutate here to
        /// clean up. "Anonymous" is chosen arbitrarily among the five - any would do.
        /// </summary>
        [Fact]
        public async Task DeleteRole_BuiltInSeededRole_IsANoOp_RoleSurvivesUnchanged()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var before = await repo.GetRoleByName("Anonymous");
            Assert.True(before.IsBuiltIn);

            await repo.DeleteRole(before.Id);

            var after = await repo.GetRoleByName("Anonymous");
            Assert.Equal(before.Id, after.Id);
            Assert.Equal(before.Name, after.Name);
            Assert.Equal(before.Description, after.Description);
            Assert.True(after.IsBuiltIn);
            Assert.True(await repo.DoesRoleExist("Anonymous"));
        }

        [Fact]
        public async Task AddRoleMember_IsAccountAMemberOfRole_GetRoleMembersPaged_GetAccountRoleMembershipPaged_RemoveRoleMember_RoundTrip()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            //Reuses the real seeded "admin" account rather than creating a new Users.Profile row - see this
            //class's own remarks for why.
            var profile = await repo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
            var userId = profile.UserId;

            var role = await repo.GetRoleByName("Contributor");

            //Defensive pre-clean (not just post-test cleanup below) - see this class's own remarks for why.
            await repo.RemoveRoleMember(role.Id, userId);
            Assert.False(await repo.IsAccountAMemberOfRole(userId, role.Id, forceReCache: true));

            var addResult = await repo.AddRoleMember(userId, role.Id);
            Assert.NotNull(addResult);
            Assert.True(addResult!.Id > 0);
            Assert.Equal(userId, addResult.UserId);
            Assert.Equal(profile.Navigation, addResult.Navigation);
            Assert.Equal(profile.AccountName, addResult.AccountName);
            Assert.Equal(profile.EmailAddress, addResult.EmailAddress);

            try
            {
                Assert.True(await repo.IsAccountAMemberOfRole(userId, role.Id, forceReCache: true));

                //Walks every page rather than asserting containment on page 1 alone - this role's real membership
                //count on the shared/persistent database is not otherwise controlled by this test, and
                //GetRoleMembersPaged's own page size is always the "Pagination Size" customization setting,
                //never a caller override (see EfUsersRepository.GetRoleMembersPaged's own doc comment) - so a
                //busy/long-lived database could in principle push this test's own new member onto a later page.
                Assert.True(await RoleHasMemberAsync(repo, role.Id, userId, profile.AccountName));

                var membership = await repo.GetAccountRoleMembershipPaged(userId, 1);
                Assert.Contains(membership, m => m.RoleId == role.Id && m.Name == role.Name);

                await repo.RemoveRoleMember(role.Id, userId);
                Assert.False(await repo.IsAccountAMemberOfRole(userId, role.Id, forceReCache: true));

                var membershipAfterRemoval = await repo.GetAccountRoleMembershipPaged(userId, 1);
                Assert.DoesNotContain(membershipAfterRemoval, m => m.RoleId == role.Id);
            }
            finally
            {
                //Idempotent no-op if the RemoveRoleMember call above already ran successfully.
                await repo.RemoveRoleMember(role.Id, userId);
            }
        }

        [Fact]
        public async Task AddRoleMemberByname_AddAccountMembership_IsAccountAMemberOfRole_GetAccountRoleMembershipPaged_RemoveRoleMember_RoundTrip()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            //Reuses the real seeded "admin" account rather than creating a new Users.Profile row - see this
            //class's own remarks for why.
            var profile = await repo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
            var userId = profile.UserId;

            //Two different seeded roles for the two different insert paths under test, so each assertion below
            //can unambiguously tell which call produced which membership row. Neither is "Administrator" -
            //deliberately avoided so this test never interacts with the EF-only bootstrap membership
            //EfUsersRepository.EnsureAdministratorRoleMembership grants "admin" on SqlServer/Postgres (not present
            //on the SQLite reference at all - see that member's own doc comment).
            var moderatorRole = await repo.GetRoleByName("Moderator");
            var memberRole = await repo.GetRoleByName("Member");

            //Defensive pre-clean (not just post-test cleanup below) - see this class's own remarks for why.
            await repo.RemoveRoleMember(moderatorRole.Id, userId);
            await repo.RemoveRoleMember(memberRole.Id, userId);
            Assert.False(await repo.IsAccountAMemberOfRole(userId, moderatorRole.Id, forceReCache: true));
            Assert.False(await repo.IsAccountAMemberOfRole(userId, memberRole.Id, forceReCache: true));

            //AddRoleMemberByname resolves the role by name (rather than by Id, like AddRoleMember - already
            //covered above) but otherwise builds the same TwAddRoleMemberResult shape.
            var byNameResult = await repo.AddRoleMemberByname(userId, moderatorRole.Name);
            Assert.NotNull(byNameResult);
            Assert.True(byNameResult!.Id > 0);
            Assert.Equal(userId, byNameResult.UserId);
            Assert.Equal(profile.AccountName, byNameResult.AccountName);
            Assert.Equal(profile.Navigation, byNameResult.Navigation);

            //AddAccountMembership is a structurally separate insert path (its own reference script/EF member -
            //see EfUsersRepository.AddAccountMembership's own doc comment) that returns just the new
            //Users.AccountRole.Id plus the role's Name, not a full account profile shape.
            var membershipResult = await repo.AddAccountMembership(userId, memberRole.Id);
            Assert.NotNull(membershipResult);
            Assert.True(membershipResult!.Id > 0);
            Assert.Equal(memberRole.Name, membershipResult.Name);

            try
            {
                Assert.True(await repo.IsAccountAMemberOfRole(userId, moderatorRole.Id, forceReCache: true));
                Assert.True(await repo.IsAccountAMemberOfRole(userId, memberRole.Id, forceReCache: true));

                var membership = await repo.GetAccountRoleMembershipPaged(userId, 1);
                Assert.Contains(membership, m => m.RoleId == moderatorRole.Id && m.Name == moderatorRole.Name);
                Assert.Contains(membership, m => m.RoleId == memberRole.Id && m.Name == memberRole.Name);

                await repo.RemoveRoleMember(moderatorRole.Id, userId);
                await repo.RemoveRoleMember(memberRole.Id, userId);

                Assert.False(await repo.IsAccountAMemberOfRole(userId, moderatorRole.Id, forceReCache: true));
                Assert.False(await repo.IsAccountAMemberOfRole(userId, memberRole.Id, forceReCache: true));

                var membershipAfterRemoval = await repo.GetAccountRoleMembershipPaged(userId, 1);
                Assert.DoesNotContain(membershipAfterRemoval, m => m.RoleId == moderatorRole.Id || m.RoleId == memberRole.Id);
            }
            finally
            {
                //Idempotent no-op for whichever (or both) of these already ran successfully above.
                await repo.RemoveRoleMember(moderatorRole.Id, userId);
                await repo.RemoveRoleMember(memberRole.Id, userId);
            }
        }

        /// <summary>
        /// Shared by the <c>AddRoleMember</c>/<c>AddRoleMemberByname</c> round-trip tests above: whether
        /// <paramref name="roleId"/>'s Users.AccountRole membership list (as seen through
        /// <see cref="ITwUsersRepository.GetRoleMembersPaged"/>) contains <paramref name="userId"/>/<paramref
        /// name="accountName"/>, searching every page rather than just the first - see the caller's own remarks for
        /// why this matters against a shared/persistent database whose real membership count for a given role is
        /// not otherwise controlled by this test class.
        /// </summary>
        private static async Task<bool> RoleHasMemberAsync(TightWiki.Plugin.Interfaces.Repository.ITwUsersRepository repo,
            int roleId, Guid userId, string accountName)
        {
            var page = await repo.GetRoleMembersPaged(roleId, 1);
            if (page.Count == 0)
            {
                return false;
            }

            if (page.Any(m => m.UserId == userId && m.AccountName == accountName))
            {
                return true;
            }

            var totalPages = page[0].PaginationPageCount;
            for (int pageNumber = 2; pageNumber <= totalPages; pageNumber++)
            {
                page = await repo.GetRoleMembersPaged(roleId, pageNumber);
                if (page.Any(m => m.UserId == userId && m.AccountName == accountName))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
