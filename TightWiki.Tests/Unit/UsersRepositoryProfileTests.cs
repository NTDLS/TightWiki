using TightWiki.Library;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Scenario-level integration tests for the user-profile slice of <see cref="ITwUsersRepository"/> (49 members
    /// total, per <c>EfUsersRepository</c>'s own class-level remarks - far too many for one test file, same
    /// reasoning as <see cref="UsersRepositoryRoleTests"/>/<see cref="UsersRepositoryPermissionAuthTests"/>). This
    /// is the third and last of the three <c>ITwUsersRepository</c> test tasks, covering the 17-member
    /// "user-profile" category <c>EfUsersRepository</c>'s own class-level remarks call out as implemented together
    /// in phase 2b.11: <see cref="ITwUsersRepository.AutoCompleteAccount"/>, <see
    /// cref="ITwUsersRepository.GetAllPublicProfilesPaged"/>, <see cref="ITwUsersRepository.AnonymizeProfile"/>,
    /// <see cref="ITwUsersRepository.IsUserMemberOfAdministrators"/>, <see cref="ITwUsersRepository.GetAllUsers"/>,
    /// <see cref="ITwUsersRepository.GetAllUsersPaged"/>, <see cref="ITwUsersRepository.CreateProfile"/>, <see
    /// cref="ITwUsersRepository.DoesEmailAddressExist"/>, <see cref="ITwUsersRepository.DoesProfileAccountExist"/>,
    /// <see cref="ITwUsersRepository.GetBasicProfileByUserId"/>, <see
    /// cref="ITwUsersRepository.GetAccountProfileByUserId"/>, <see cref="ITwUsersRepository.SetProfileUserId"/>,
    /// <see cref="ITwUsersRepository.GetUserAccountIdByNavigation"/>, <see
    /// cref="ITwUsersRepository.GetAccountProfileByNavigation"/>, <see cref="ITwUsersRepository.UpdateProfile"/>,
    /// <see cref="ITwUsersRepository.UpdateProfileAvatar"/>, and <see
    /// cref="ITwUsersRepository.GetProfileAvatarByNavigation"/>. Role/permission/authentication (already covered by
    /// the two sibling classes above) and the Page repository are out of scope - see this task's own brief.
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
    /// <b>Why this class never creates a new Users.Profile row, and why three of the 17 members above
    /// (<see cref="ITwUsersRepository.CreateProfile"/>, <see cref="ITwUsersRepository.AnonymizeProfile"/>, <see
    /// cref="ITwUsersRepository.SetProfileUserId"/>) are deliberately reviewed by inspection only, never invoked
    /// live here:</b> unlike <see cref="UsersRepositoryRoleTests"/>/<see cref="UsersRepositoryPermissionAuthTests"/>
    /// (which can mutate role/permission rows and clean them up again via <see
    /// cref="ITwUsersRepository.DeleteRole"/>/<see cref="ITwUsersRepository.RemoveRolePermission"/>/<see
    /// cref="ITwUsersRepository.RemoveAccountPermission"/>), <see cref="ITwUsersRepository"/> has <b>no member that
    /// deletes a Users.Profile row at all</b> - confirmed by re-reading the full interface for this task. A profile
    /// row created via <see cref="ITwUsersRepository.CreateProfile"/> is therefore permanent, and (same reasoning
    /// already documented on <see cref="UsersRepositoryRoleTests"/>) would corrupt <c>MarkupTests</c>'/
    /// <c>FullPageTests</c>' golden <c>TestProfileList_*</c>/<c>TestProfileGlossary_*</c> <c>.wiki.expected</c>
    /// files, which bake in an exact, unfiltered enumeration of every Users.Profile row (confirmed by inspection of
    /// <c>TightWiki.Plugin.Default.StandardFunctions.UsersFunctions.ProfileList</c>/<c>ProfileGlossary</c>, backed
    /// by <see cref="ITwUsersRepository.GetAllPublicProfilesPaged"/>, which applies no filter of any kind beyond
    /// the caller-supplied search token).
    /// </para>
    /// <para>
    /// This task's own brief asked whether route (A) - finding some way to hide/remove a test profile without a
    /// real delete member - was possible, specifically calling out <see
    /// cref="ITwUsersRepository.AnonymizeProfile"/> as a candidate ("does it exclude the row from
    /// GetAllPublicProfilesPaged?"). By inspection of both <c>EfUsersRepository.AnonymizeProfile</c> and the SQLite
    /// reference's own <c>UsersRepository.AnonymizeProfile</c>/<c>AnonymizeProfile.sql</c>: it is a plain
    /// <c>UPDATE</c> that overwrites AccountName/Navigation/Biography/Avatar in place - the row still exists
    /// afterward, unconditionally visible to <see cref="ITwUsersRepository.GetAllPublicProfilesPaged"/> (which has
    /// no "is this row anonymized/hidden" filter of any kind - there is no such flag anywhere on <see
    /// cref="TwAccountProfile"/>/the underlying schema). Route (A) is therefore not possible: a
    /// <see cref="ITwUsersRepository.CreateProfile"/>-created row, anonymized or not, would still permanently add
    /// one more entry to every golden <c>TestProfileList_*</c>/<c>TestProfileGlossary_*</c> enumeration. This
    /// confirms route (B) from this task's own brief: profile round-trips are exercised only against the real,
    /// already-seeded <c>admin</c> account (<see cref="Constants.DEFAULTACCOUNT"/> - the same account already read
    /// read-only or mutated-and-restored by both sibling classes above), never a newly created one, and <see
    /// cref="ITwUsersRepository.CreateProfile"/> itself is verified by code review only (mirrors
    /// <c>StatisticsRepositoryTests</c>' own treatment of <see
    /// cref="TightWiki.Plugin.Interfaces.Repository.ITwStatisticsRepository.PurgePageStatistics"/> - reviewed
    /// against its SQLite counterpart as part of this task, deliberately not exercised live): a one-line insert
    /// (<c>EfUsersRepository.CreateProfile</c> - a <see cref="DoesProfileAccountExist"/> pre-check then a single
    /// <c>context.Profiles.Add</c>/<c>SaveChangesAsync</c>) about as low-risk as EF Core LINQ gets, and already
    /// exercised indirectly by every other test in this class via the very same <c>admin</c> row it once created.
    /// </para>
    /// <para>
    /// <b><see cref="ITwUsersRepository.AnonymizeProfile"/>/<see cref="ITwUsersRepository.SetProfileUserId"/> are
    /// likewise reviewed by inspection only, never invoked live here</b> - a decision this task's own brief did not
    /// pre-empt, made while writing this class: both mutate exactly the field(s) every other concurrently-running
    /// xunit collection in this assembly relies on to resolve the <c>admin</c> profile at all.
    /// <see cref="ITwUsersRepository.AnonymizeProfile"/> overwrites AccountName/Navigation (to a
    /// <c>"DeletedUser_..."</c> placeholder) and unconditionally nulls Avatar for the row matching a given UserId -
    /// applied to the real <c>admin</c> UserId, <see cref="ITwUsersRepository.GetAccountProfileByNavigation"/>
    /// (<c>Navigation = "admin"</c>) would find zero rows for the whole window between the call and any
    /// restoration, exactly the lookup <see cref="UsersRepositoryRoleTests"/>/<see
    /// cref="UsersRepositoryPermissionAuthTests"/> (running concurrently, in separate xunit collections, per
    /// default xunit parallelization) perform via <c>GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
    /// ?? throw</c> at the very start of several of their own tests - a real risk of transient cross-collection
    /// failures, not a theoretical one. <see cref="ITwUsersRepository.SetProfileUserId"/> is structurally the same
    /// risk one level deeper: it repoints the Users.Profile row's own primary key (UserId) for a given Navigation,
    /// so a temporary "swap away, then restore" round trip would - for that same window - make <i>any</i>
    /// concurrently-running <c>GetAccountProfileByUserId(adminOriginalUserId)</c>/<c>GetAccountProfileByNavigation("admin")</c>
    /// call either throw (<c>SingleAsync</c>) or fail to resolve the Identity user via <see
    /// cref="EfUsersRepository.BuildFullAccountProfileAsync"/>'s own inner Identity lookup (confirmed by inspection:
    /// it looks up by <c>profile.UserId</c>, which would transiently point at a nonexistent Identity user) - the
    /// same class of risk <c>StatisticsRepositoryTests</c>' own remarks document, empirically, for
    /// <c>PurgePageStatistics</c> against this same shared/concurrently-used database, just triggered here by a
    /// primary-key swap instead of a table truncation. There is also no *other* pre-existing Users.Profile row to
    /// safely target either method against instead (the seeded database ships exactly one - <c>admin</c> - same
    /// "sole pre-existing profile" fact <see cref="UsersRepositoryRoleTests"/>' own remarks already establish), and
    /// creating one first via <see cref="ITwUsersRepository.CreateProfile"/> is exactly the permanent-row problem
    /// this whole remarks section starts from. Both are instead verified by direct comparison of
    /// <c>EfUsersRepository</c> against the SQLite reference (<see cref="ITwUsersRepository.AnonymizeProfile"/>'s
    /// own doc comment on <c>EfUsersRepository</c> already documents the one behavioral divergence found: the
    /// reference never clears the <see cref="TightWiki.Library.Caching.MemCache.Category.User"/> cache entry
    /// afterward, and neither does the EF port, kept as-is rather than "fixed").
    /// </para>
    /// <para>
    /// Every mutating test below therefore targets only fields <b>not</b> read by anything else in this assembly
    /// while running: <see cref="ITwUsersRepository.UpdateProfile"/> is exercised passing <c>admin</c>'s own
    /// unchanged AccountName/Navigation back through (only Biography actually differs - confirmed, by inspection of
    /// <c>TightWiki.Plugin.Default.StandardFunctions.UsersFunctions</c>, that no built-in markup function renders
    /// Biography), and <see cref="ITwUsersRepository.UpdateProfileAvatar"/> is exercised similarly (Avatar is
    /// likewise never rendered by any built-in markup function) - both restored to their original values in a
    /// <c>finally</c> block, same pattern as <see cref="ConfigurationRepositoryTests"/>' <c>SaveConfigurationEntryValueByGroupAndEntry</c>
    /// scenario. Every other test in this class is purely read-only.
    /// </para>
    /// </remarks>
    [Collection("Users Repository Profile Tests")]
    public class UsersRepositoryProfileTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// Walks every page of <see cref="ITwUsersRepository.GetAllUsersPaged"/> for the given <paramref
        /// name="searchToken"/> and returns every row found - same "don't trust a single page against the shared
        /// test database" reasoning as <c>UsersRepositoryRoleTests.RoleHasMemberAsync</c>/
        /// <c>UsersRepositoryPermissionAuthTests.GetAllRolePermissionsAsync</c>.
        /// </summary>
        private static async Task<List<TwAccountProfile>> GetAllUsersPagedAsync(ITwUsersRepository repo, string? searchToken)
        {
            var all = new List<TwAccountProfile>();
            var firstPage = await repo.GetAllUsersPaged(1, searchToken: searchToken);
            all.AddRange(firstPage);
            var totalPages = firstPage.Count > 0 ? firstPage[0].PaginationPageCount : 1;
            for (var pageNumber = 2; pageNumber <= totalPages; pageNumber++)
            {
                all.AddRange(await repo.GetAllUsersPaged(pageNumber, searchToken: searchToken));
            }
            return all;
        }

        /// <summary>
        /// Same as <see cref="GetAllUsersPagedAsync"/>, for <see
        /// cref="ITwUsersRepository.GetAllPublicProfilesPaged"/> instead.
        /// </summary>
        private static async Task<List<TwAccountProfile>> GetAllPublicProfilesPagedAsync(ITwUsersRepository repo, string? searchToken)
        {
            var all = new List<TwAccountProfile>();
            var firstPage = await repo.GetAllPublicProfilesPaged(1, searchToken: searchToken);
            all.AddRange(firstPage);
            var totalPages = firstPage.Count > 0 ? firstPage[0].PaginationPageCount : 1;
            for (var pageNumber = 2; pageNumber <= totalPages; pageNumber++)
            {
                all.AddRange(await repo.GetAllPublicProfilesPaged(pageNumber, searchToken: searchToken));
            }
            return all;
        }

        [Fact]
        public async Task GetAccountProfileByNavigation_GetAccountProfileByUserId_GetBasicProfileByUserId_GetUserAccountIdByNavigation_ReturnConsistentSeededAdminProfile()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;
            var navigation = TwNavigation.Clean(Constants.DEFAULTACCOUNT);

            var byNav = await repo.GetAccountProfileByNavigation(navigation);
            Assert.NotNull(byNav);
            //AccountName's exact casing ("Admin" in the shipped seed data) is not guaranteed to match
            //Constants.DEFAULTACCOUNT's own casing ("admin", only ever used for bootstrap/login, not display) -
            //Navigation (below) is the case-normalized identity, compared exact-case instead.
            Assert.Equal(Constants.DEFAULTACCOUNT, byNav!.AccountName, StringComparer.OrdinalIgnoreCase);
            Assert.Equal(navigation, byNav.Navigation);
            Assert.Equal(Constants.DEFAULTUSERNAME, byNav.EmailAddress);

            var byUserId = await repo.GetAccountProfileByUserId(byNav.UserId, forceReCache: true);
            Assert.Equal(byNav.UserId, byUserId.UserId);
            Assert.Equal(byNav.AccountName, byUserId.AccountName);
            Assert.Equal(byNav.Navigation, byUserId.Navigation);

            var basic = await repo.GetBasicProfileByUserId(byNav.UserId);
            Assert.NotNull(basic);
            Assert.Equal(byNav.UserId, basic!.UserId);
            Assert.Equal(byNav.AccountName, basic.AccountName);
            Assert.Equal(byNav.Navigation, basic.Navigation);

            Assert.Equal(byNav.UserId, await repo.GetUserAccountIdByNavigation(navigation));

            //Documented edge-case behavior for an unknown navigation - see
            //EfUsersRepository.GetUserAccountIdByNavigation's own doc comment: unlike GetAccountProfileByNavigation
            //(which returns null), GetUserAccountIdByNavigation returns Guid.Empty, not null, matching the
            //SQLite reference's own value-level (if not type-level) behavior.
            var unknownNavigation = $"no-such-account-{Guid.NewGuid():N}";
            Assert.Null(await repo.GetAccountProfileByNavigation(unknownNavigation));
            Assert.Null(await repo.GetAccountProfileByNavigation(null));
            Assert.Equal(Guid.Empty, await repo.GetUserAccountIdByNavigation(unknownNavigation));
        }

        [Fact]
        public async Task DoesProfileAccountExist_DoesEmailAddressExist_ReturnTrueForSeededAdmin_FalseForUnknown()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;
            var navigation = TwNavigation.Clean(Constants.DEFAULTACCOUNT);

            Assert.True(await repo.DoesProfileAccountExist(navigation));
            Assert.False(await repo.DoesProfileAccountExist($"no-such-account-{Guid.NewGuid():N}"));

            Assert.True(await repo.DoesEmailAddressExist(Constants.DEFAULTUSERNAME));
            //Both the SQLite reference and EfUsersRepository lower-invariant the input client-side before
            //comparing (DoesEmailAddressExist's own doc comment on EfUsersRepository) - exercises that explicitly.
            Assert.True(await repo.DoesEmailAddressExist(Constants.DEFAULTUSERNAME.ToUpperInvariant()));
            Assert.False(await repo.DoesEmailAddressExist($"no-such-email-{Guid.NewGuid():N}@example.com"));
            //A null address always returns false (three-valued-logic guard) - EfUsersRepository.DoesEmailAddressExist's
            //own doc comment on why this needs an explicit guard rather than relying on EF's null-semantics.
            Assert.False(await repo.DoesEmailAddressExist(null));
        }

        [Fact]
        public async Task IsUserMemberOfAdministrators_ReturnsTrueForAdmin_FalseForRandomUser()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            var profile = await repo.GetAccountProfileByNavigation(TwNavigation.Clean(Constants.DEFAULTACCOUNT))
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");

            Assert.True(await repo.IsUserMemberOfAdministrators(profile.UserId));
            Assert.False(await repo.IsUserMemberOfAdministrators(Guid.NewGuid()));
        }

        [Fact]
        public async Task AutoCompleteAccount_GetAllUsers_GetAllUsersPaged_GetAllPublicProfilesPaged_ContainSeededAdmin()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;

            //"admin" alone is a unique enough substring of the seeded account's own AccountName/EmailAddress
            //(Constants.DEFAULTACCOUNT/DEFAULTUSERNAME) that it cannot collide with any other seeded row, same
            //"exact-case substring of the row's own name" reasoning as UsersRepositoryRoleTests' AutoCompleteRole
            //assertions - AutoCompleteAccount's own 25-row cap can therefore never exclude it. All the substring
            //filters below (AutoCompleteAccount/GetAllUsers/GetAllUsersPaged/GetAllPublicProfilesPaged) already
            //match case-insensitively on the provider side - only the final AccountName assertion needs its own
            //explicit case-insensitive comparer, since the shipped seed data's exact casing ("Admin") is not
            //guaranteed to match Constants.DEFAULTACCOUNT's own casing ("admin", only used for bootstrap/login).
            var autoComplete = await repo.AutoCompleteAccount(Constants.DEFAULTACCOUNT);
            Assert.Contains(autoComplete, p => string.Equals(p.AccountName, Constants.DEFAULTACCOUNT, StringComparison.OrdinalIgnoreCase));

            var allUsers = await repo.GetAllUsers();
            Assert.Contains(allUsers, p => string.Equals(p.AccountName, Constants.DEFAULTACCOUNT, StringComparison.OrdinalIgnoreCase));

            //Walks every page rather than trusting page 1 alone - same reasoning as every sibling class's own
            //paged helpers - and exercises the searchToken filter.
            var usersPaged = await GetAllUsersPagedAsync(repo, Constants.DEFAULTACCOUNT);
            Assert.Contains(usersPaged, p => string.Equals(p.AccountName, Constants.DEFAULTACCOUNT, StringComparison.OrdinalIgnoreCase));

            //Exercises the orderBy/orderByDirection parameters (GetAllUsersPaged.sql / RepositoryHelpers.TransposeOrderby),
            //same idiom as ConfigurationRepositoryTests.MenuItem_InsertGetUpdateDelete_RoundTrips' own "Name"/"desc" case.
            var usersPagedOrdered = await repo.GetAllUsersPaged(1, orderBy: "Account", orderByDirection: "asc", searchToken: Constants.DEFAULTACCOUNT);
            Assert.Contains(usersPagedOrdered, p => string.Equals(p.AccountName, Constants.DEFAULTACCOUNT, StringComparison.OrdinalIgnoreCase));

            //GetAllPublicProfilesPaged has no orderBy parameter at all (unlike GetAllUsersPaged) - see its own doc
            //comment on EfUsersRepository - only searchToken is exercised here.
            var publicProfiles = await GetAllPublicProfilesPagedAsync(repo, Constants.DEFAULTACCOUNT);
            Assert.Contains(publicProfiles, p => string.Equals(p.AccountName, Constants.DEFAULTACCOUNT, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Round-trips <see cref="ITwUsersRepository.UpdateProfile"/> against the real seeded <c>admin</c> account,
        /// changing only Biography - AccountName/Navigation are passed through unchanged (same values already on
        /// the row), so this never touches the identity fields every other concurrently-running xunit collection in
        /// this assembly relies on to resolve this profile (see this class's own remarks).
        /// </summary>
        [Fact]
        public async Task UpdateProfile_MutatesBiographyOnly_RoundTrip_RestoresOriginalViaFinally()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;
            var navigation = TwNavigation.Clean(Constants.DEFAULTACCOUNT);

            var original = await repo.GetAccountProfileByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
            var originalBiography = original.Biography;

            var newBiography = $"TestBio_{Guid.NewGuid():N}";

            try
            {
                await repo.UpdateProfile(new TwAccountProfile
                {
                    UserId = original.UserId,
                    AccountName = original.AccountName,
                    Navigation = original.Navigation,
                    Biography = newBiography,
                });

                //UpdateProfile clears the User cache category for this UserId, but forceReCache: true is used
                //anyway for clarity/defensiveness, matching every other forceReCache-capable read in this project.
                var afterUpdateByUserId = await repo.GetAccountProfileByUserId(original.UserId, forceReCache: true);
                Assert.Equal(newBiography, afterUpdateByUserId.Biography);
                Assert.Equal(original.AccountName, afterUpdateByUserId.AccountName);
                Assert.Equal(original.Navigation, afterUpdateByUserId.Navigation);

                //GetAccountProfileByNavigation is never cached at all (its own doc comment on EfUsersRepository) -
                //an independent, uncached confirmation of the same write.
                var afterUpdateByNav = await repo.GetAccountProfileByNavigation(navigation);
                Assert.NotNull(afterUpdateByNav);
                Assert.Equal(newBiography, afterUpdateByNav!.Biography);
            }
            finally
            {
                await repo.UpdateProfile(new TwAccountProfile
                {
                    UserId = original.UserId,
                    AccountName = original.AccountName,
                    Navigation = original.Navigation,
                    Biography = originalBiography,
                });
            }
        }

        /// <summary>
        /// Round-trips <see cref="ITwUsersRepository.UpdateProfileAvatar"/>/<see
        /// cref="ITwUsersRepository.GetProfileAvatarByNavigation"/> against the real seeded <c>admin</c> account -
        /// Avatar/AvatarContentType are not read by anything else in this assembly (see this class's own remarks),
        /// so this is safe to mutate for the brief window between the write and the <c>finally</c> restore.
        /// </summary>
        [Fact]
        public async Task UpdateProfileAvatar_GetProfileAvatarByNavigation_RoundTrip_RestoresOriginalViaFinally()
        {
            var repo = fixture.Artifacts.DatabaseManager.UsersRepository;
            var navigation = TwNavigation.Clean(Constants.DEFAULTACCOUNT);

            var original = await repo.GetAccountProfileByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded '{Constants.DEFAULTACCOUNT}' account profile.");
            var originalAvatar = await repo.GetProfileAvatarByNavigation(navigation)
                ?? throw new Exception($"Could not find an avatar row for the seeded '{Constants.DEFAULTACCOUNT}' account profile.");

            var newAvatarBytes = System.Text.Encoding.UTF8.GetBytes($"TestAvatar_{Guid.NewGuid():N}");
            var newContentType = "image/x-test";

            try
            {
                await repo.UpdateProfileAvatar(original.UserId, newAvatarBytes, newContentType);

                //GetProfileAvatarByNavigation is never cached (its own doc comment on EfUsersRepository) - a
                //directly-post-write read always reflects the real, current database state.
                var afterUpdate = await repo.GetProfileAvatarByNavigation(navigation);
                Assert.NotNull(afterUpdate);
                Assert.Equal(newAvatarBytes, afterUpdate!.Bytes);
                Assert.Equal(newContentType, afterUpdate.ContentType);
            }
            finally
            {
                //Null-forgiving: restores exactly whatever this account's Avatar was before this test ran, which
                //may legitimately be null (no avatar uploaded) - UpdateProfileAvatar's own imageData parameter is
                //non-nullable only at the signature-warning level, not at the (EF/Dapper) runtime level.
                await repo.UpdateProfileAvatar(original.UserId, originalAvatar.Bytes!, originalAvatar.ContentType);
            }
        }
    }
}
