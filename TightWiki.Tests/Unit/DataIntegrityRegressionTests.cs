using System.Reflection;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Regression tests for four further specific, already-fixed bugs uncovered during the
    /// Database-Providers-Plan.md implementation phase - the second such sada from the
    /// Database-Providers-Testing-Plan.md testing initiative, sibling to <see cref="BootstrapSeedRegressionTests"/>
    /// (see that class's own remarks for the shared "one narrow historical bug per test" reasoning, which this
    /// class follows identically):
    /// <list type="bullet">
    /// <item><description><b>Bug 1</b> (commit 3d8512de, "quote raw SQL insert column names for keyless
    /// CryptoCheck/AdminPwCheck") - <c>EfConfigurationRepository.SetCryptoCheck</c>/<c>EfUsersRepository</c>'s
    /// private <c>SetAdminPwCheckValueAsync</c> spliced an unquoted column name (<c>Content</c>/<c>Value</c>) into
    /// a raw parameterized <c>INSERT</c> against a keyless entity type. On Postgres, whose unquoted identifiers
    /// fold to lowercase while EF Core's own migrations create a quoted, PascalCase column, this insert always
    /// targeted a non-existent lowercase column and failed with 42703 ("column does not exist") - SQL Server was
    /// unaffected (its unquoted identifiers are case-insensitive). See
    /// <see cref="Postgres_CryptoCheckAndAdminPwCheck_RawSqlInsertsSucceedWithQuotedColumnNames"/>.</description></item>
    /// <item><description><b>Bug 2</b> (commit 8c02b34d, "Add Npgsql citext branch to shared
    /// StripNonSqliteNoCaseCollation") - <c>TightWikiDbContext.StripNonSqliteNoCaseCollation</c> strips SQLite's
    /// <c>COLLATE NOCASE</c> for every non-SQLite provider (it isn't a recognized collation name there), but before
    /// this fix did nothing further for Postgres, leaving every formerly-<c>NOCASE</c> <see cref="string"/> column
    /// as an ordinary, case-sensitive Postgres <c>text</c> - unlike SQL Server, whose default database collation is
    /// already case-insensitive, so stripping alone was sufficient there. The fix switches those columns to
    /// Postgres's <c>citext</c> type on top of stripping the collation. See
    /// <see cref="GetEmojiByName_CaseInsensitiveLookup_MatchesRegardlessOfCase_OnEveryProvider"/>.</description></item>
    /// <item><description><b>Bug 3</b> (commit a71ee1a7, "seed all wiki pages, their attachments, and search
    /// metadata for MSSQL/Postgres") - one commit fixing three independent seeding gaps found via a full row-count
    /// audit against the SQLite reference, all affecting only the MSSQL/Postgres EF Core seed path (never SQLite,
    /// which ships a full binary copy of the reference <c>.db</c> files instead of reconstructing content from
    /// <c>Seed/tightwiki.seed.zip</c>) - covered as three separate tests per this task's own brief:
    /// <list type="bullet">
    /// <item><description><b>3a</b> - the root (<c>""</c>) and <c>Sandbox</c> namespaces (Home page, Sandbox demo
    /// page) were silently dropped from the seed content entirely (a hardcoded namespace filter in
    /// <c>GetDefaultDefaultWikiPages.sql</c>, plus <c>Program.cs</c> never requesting
    /// <see cref="TwDefaultDataType.IncludePages"/>). See
    /// <see cref="GetAllNamespaces_And_GetAllNamespacePagesPaged_IncludeRootAndSandboxNamespaces"/>.</description></item>
    /// <item><description><b>3b</b> - page file attachments (images embedded in Home/Wiki About/etc.) were never
    /// exported to the seed package at all. See
    /// <see cref="SeededHomePage_ImageAttachment_IsReadableViaPageFileRepositoryMethods"/>.</description></item>
    /// <item><description><b>3c</b> - seeded pages never went through markup tokenization, so
    /// Pages.PageToken/PageTag/PageProcessingInstruction/PageReference stayed empty on a fresh MSSQL/Postgres
    /// install. See <see cref="Bug3c_SeededPageMetadataTokenization_AlreadyCoveredElsewhere_DocumentedNotDuplicated"/>
    /// for why this is a documented pointer to existing coverage rather than a new test, per this task's own
    /// explicit fallback instruction.</description></item>
    /// </list>
    /// </description></item>
    /// <item><description><b>Bug 4</b> (commit 7eb2c329, "Fix emoji image format corruption in seed package
    /// export") - <c>emoji.db</c> stores <c>Emoji.ImageData</c> GZip-compressed, but
    /// <c>SeedPackageGenerator</c> wrote those raw, still-compressed column bytes straight into
    /// <c>Seed/tightwiki.seed.zip</c>'s <c>Emoji/Images/*</c> entries - every image entry therefore actually held a
    /// GZip stream, despite the correct file extension/MIME type. See
    /// <see cref="GetDefaultEmojis_ReadEmojiImageBytes_ReturnsValidUncompressedImageData_NotStillGZipCompressed"/>.
    /// </description></item>
    /// </list>
    /// Written entirely against the provider-agnostic <see cref="ITwConfigurationRepository"/>/
    /// <see cref="ITwUsersRepository"/>/<see cref="ITwPageRepository"/>/<see cref="ITwEmojiRepository"/>/
    /// <see cref="ITwDefaultsRepository"/> interfaces - no <c>#if SQLITE_PROVIDER</c>/<c>#elif SQLSERVER_PROVIDER</c>/
    /// <c>#elif POSTGRES_PROVIDER</c> branching anywhere in this file, even for Bug 1 (Postgres-only in substance):
    /// that test instead branches at runtime on <see cref="ITwDatabaseManager.GetType"/>'s <see cref="Type.Name"/>,
    /// exactly as <see cref="BootstrapSeedRegressionTests"/>'s own Bug 4 test does, and per this task's own
    /// instruction - a documented no-op under SQLite/SqlServer. The same compiled test binary runs three times:
    /// <c>dotnet test</c> (SQLite, default), <c>dotnet test -p:DataProvider=SqlServer</c>,
    /// <c>dotnet test -p:DataProvider=Postgres</c> (Database-Providers-Testing-Plan.md chapter 5.5).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Runs against the shared, persistent test database (Database-Providers-Testing-Plan.md chapter 5.3),
    /// deliberately placed in the <b>same xunit collection as <see cref="UsersRepositoryPermissionAuthTests"/></b>
    /// (rather than a brand-new, independent collection the way <see cref="BootstrapSeedRegressionTests"/> uses) -
    /// not stylistic, but required for correctness: <see cref="Postgres_CryptoCheckAndAdminPwCheck_RawSqlInsertsSucceedWithQuotedColumnNames"/>
    /// writes to the single, real Users.AdminPwCheck row via <see cref="ITwUsersRepository.SetAdminPasswordIsDefault"/>/
    /// <see cref="ITwUsersRepository.SetAdminPasswordIsChanged"/>/<see cref="ITwUsersRepository.SetAdminPasswordClear"/>
    /// - the exact same single shared row <see cref="UsersRepositoryPermissionAuthTests.AdminPasswordStatus_SetAdminPasswordClear_SetAdminPasswordIsDefault_SetAdminPasswordIsChanged_StateMachine_RoundTrip"/>
    /// exercises as its own multi-step state machine (write, then immediately assert via
    /// <see cref="ITwUsersRepository.AdminPasswordStatus"/>). Two different xunit collections run in parallel by
    /// default, so without this shared collection an interleaved write from this class could make that other
    /// test's own state assertions fail for a reason unrelated to either bug. Every other test in this class is
    /// either purely read-only (Bug 2/3a/3b/4) or writes to a structurally separate, idempotent single row
    /// (Config.CryptoCheck, the other half of Bug 1 - see that test's own remarks for why no collision with
    /// <see cref="ConfigurationRepositoryTests.CryptoCheck_SetThenGet_RoundTrips_AndIsFirstRunReportsFalseAfterwards"/>
    /// is possible there), so sharing this one collection costs only a little parallelism, never correctness.
    /// </para>
    /// <para>
    /// <b>Why Bug 1's test asserts "did not throw" rather than reading back through <see
    /// cref="ITwUsersRepository.AdminPasswordStatus"/>:</b> that member has its own documented, process-wide
    /// "sticky true" cache with no per-test segmentation at all (see
    /// <see cref="UsersRepositoryPermissionAuthTests.AdminPasswordStatus_SetAdminPasswordClear_SetAdminPasswordIsDefault_SetAdminPasswordIsChanged_StateMachine_RoundTrip"/>'s
    /// own remarks for the full writeup) - reading it here would add a second, redundant assertion of state this
    /// class does not need, while carrying real risk of tripping over that same cache. The bug this test targets
    /// was a hard SQL failure on the <c>INSERT</c> itself (Postgres error 42703, "column ... does not exist") - a
    /// column-name mismatch either makes every single one of those three <c>Set*</c> calls throw, or it doesn't;
    /// there is no scenario where the pre-fix code would insert successfully with silently wrong data. "Awaited
    /// without throwing" is therefore already the complete, correct regression signal for this specific bug -
    /// confirmed by reading the fix's own diff (3d8512de), which touches only how the column identifier is
    /// resolved/quoted, nothing about the value written.
    /// </para>
    /// </remarks>
    [Collection("Users Repository Permission Auth Tests")]
    public class DataIntegrityRegressionTests(TwEngineFixture fixture)
        : IClassFixture<TwEngineFixture>
    {
        /// <summary>
        /// Bug 1 (commit 3d8512de) - see this class's own remarks for the full writeup, including why this
        /// asserts "did not throw" rather than reading back through the sticky-cached
        /// <see cref="ITwUsersRepository.AdminPasswordStatus"/>.
        /// </summary>
        [Fact]
        public async Task Postgres_CryptoCheckAndAdminPwCheck_RawSqlInsertsSucceedWithQuotedColumnNames()
        {
            var databaseManager = fixture.Artifacts.DatabaseManager;

            //Runtime provider check (not a compile-time #if), per this task's own instruction - the pre-fix bug
            //only ever manifested on Postgres (unquoted identifiers fold to lowercase there; SQL Server's
            //unquoted identifiers are case-insensitive, so the same unquoted-column bug was harmless there - see
            //this class's own remarks). Deliberate, documented no-op under SQLite/SqlServer.
            if (databaseManager.GetType().Name != "PostgresDatabaseManager")
            {
                return;
            }

            //Config.CryptoCheck half (EfConfigurationRepository.SetCryptoCheck) - the raw INSERT's "Content"
            //column identifier. Safe to round-trip fully here (unlike the AdminPwCheck half below): GetCryptoCheck
            //carries no cache of its own, and any concurrently-running SetCryptoCheck elsewhere (e.g.
            //ConfigurationRepositoryTests) always writes an equally-valid encrypted marker, so a raced write can
            //never make this assertion observe a wrong-but-not-absent value.
            var configRepo = databaseManager.ConfigurationRepository;
            await configRepo.SetCryptoCheck();
            Assert.True(await configRepo.GetCryptoCheck());

            //Users.AdminPwCheck half (EfUsersRepository's private SetAdminPwCheckValueAsync, reached via
            //SetAdminPasswordIsDefault/SetAdminPasswordIsChanged) - the raw INSERT's "Value" column identifier.
            //Pre-fix, either awaited call below would throw a PostgresException (SqlState 42703, "column ... does
            //not exist") from inside the raw INSERT itself - awaiting both without throwing is the complete
            //regression signal (see this class's own remarks for why AdminPasswordStatus is deliberately not
            //read back here).
            var usersRepo = databaseManager.UsersRepository;
            try
            {
                await usersRepo.SetAdminPasswordIsDefault();
                await usersRepo.SetAdminPasswordIsChanged();
            }
            finally
            {
                //Restores the shared Users.AdminPwCheck row to "no row" (NeedsToBeSet) - same steady-state
                //cleanup UsersRepositoryPermissionAuthTests' own state-machine test performs in its own finally
                //block, for the same reason (its own remarks: re-poisoning the sticky AdminPasswordStatus cache
                //for the next process's fixture-construction-time bootstrap otherwise).
                await usersRepo.SetAdminPasswordClear();
            }
        }

        /// <summary>
        /// Bug 2 (commit 8c02b34d) - see this class's own remarks for the full writeup.
        /// <see cref="ITwEmojiRepository.GetEmojiByName"/> (<c>EfEmojiRepository</c>: a plain <c>e.Name == name</c>
        /// LINQ filter, translated to a literal SQL equality comparison) relies entirely on the Emoji.Name column's
        /// own collation to be case-insensitive - exactly the SQLite <c>COLLATE NOCASE</c> semantics
        /// <see cref="TightWiki.Data.EfCore.TightWikiDbContext"/>'s <c>StripNonSqliteNoCaseCollation</c> exists to
        /// reproduce on every other provider (citext on Postgres, the default database collation on SQL Server -
        /// see <c>EmojiConfiguration</c>'s own <c>UseCollation("NOCASE")</c> on <see cref="Type.Name"/>). "airplane"
        /// is a real, stable, already-seeded emoji name (confirmed by direct inspection of the shipped seed
        /// content - part of the built-in emoji set every provider ships identically), not incidental test data.
        /// Provider-agnostic and unconditional (no runtime branch) - per this task's own brief, this must hold on
        /// every provider, not just Postgres/SQL Server, and does: SQLite's <c>NOCASE</c> was never broken by
        /// either bug this class covers.
        /// </summary>
        [Fact]
        public async Task GetEmojiByName_CaseInsensitiveLookup_MatchesRegardlessOfCase_OnEveryProvider()
        {
            const string seededEmojiName = "airplane";

            var repo = fixture.Artifacts.DatabaseManager.EmojiRepository;

            var exact = await repo.GetEmojiByName(seededEmojiName)
                ?? throw new Exception($"Could not find the seeded emoji named '{seededEmojiName}'.");

            var upper = await repo.GetEmojiByName(seededEmojiName.ToUpperInvariant());
            Assert.NotNull(upper);
            Assert.Equal(exact.Id, upper!.Id);

            var mixedCase = await repo.GetEmojiByName("AiRpLaNe");
            Assert.NotNull(mixedCase);
            Assert.Equal(exact.Id, mixedCase!.Id);

            //A name that really does not exist under any casing still correctly returns null - proves the
            //citext/collation change didn't turn the lookup into an accidental "matches everything" substring or
            //fuzzy search, only case-insensitive exact matching.
            Assert.Null(await repo.GetEmojiByName($"no-such-emoji-{Guid.NewGuid():N}"));
        }

        /// <summary>
        /// Bug 3a (commit a71ee1a7) - see this class's own remarks for the full writeup. Confirmed by direct
        /// inspection of the shipped seed content: the root namespace (<c>""</c>) contains the "Home" page (among
        /// others), and "Sandbox" contains "Sandbox :: Default" - both real, stable, built-in seed content, not
        /// incidental test data (the same "Sandbox :: Default" page <see cref="PageRepositoryMetadataTests"/>/
        /// <see cref="PageRepositoryListingTests"/>/<see cref="PageRepositorySearchTests"/>/<c>StatisticsRepositoryTests</c>
        /// all read from). Purely read-only against <see cref="ITwPageRepository.GetAllNamespaces"/>/
        /// <see cref="ITwPageRepository.GetAllNamespacePagesPaged"/> - safe to run any number of times, no
        /// <c>finally</c> cleanup needed.
        /// </summary>
        [Fact]
        public async Task GetAllNamespaces_And_GetAllNamespacePagesPaged_IncludeRootAndSandboxNamespaces()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            var namespaces = await pageRepo.GetAllNamespaces();
            //The root namespace is the empty string (TwPage.Namespace's own doc comment: "Returns an empty
            //string if no namespace is present") - pre-fix, GetDefaultDefaultWikiPages.sql's hardcoded
            //('Builtin','Include','Wiki Help') filter meant this namespace, and "Sandbox", were never seeded into
            //Seed/tightwiki.seed.zip at all on the MSSQL/Postgres path.
            Assert.Contains(string.Empty, namespaces);
            Assert.Contains("Sandbox", namespaces);

            var rootPages = await pageRepo.GetAllNamespacePagesPaged(1, string.Empty);
            Assert.NotEmpty(rootPages);
            Assert.Contains(rootPages, p => p.Name == "Home");
            Assert.All(rootPages, p => Assert.Equal(string.Empty, p.Namespace));

            var sandboxPages = await pageRepo.GetAllNamespacePagesPaged(1, "Sandbox");
            Assert.NotEmpty(sandboxPages);
            Assert.Contains(sandboxPages, p => p.Name == "Sandbox :: Default");
            Assert.All(sandboxPages, p => Assert.Equal("Sandbox", p.Namespace));
        }

        /// <summary>
        /// Bug 3b (commit a71ee1a7) - see this class's own remarks for the full writeup. The seeded "Home" page
        /// (root namespace, Pages.Page.Id 1 on the SQLite reference) carries a real embedded image attachment,
        /// "MC Music.png" - confirmed by direct inspection of the shipped seed content (both the SQLite
        /// <c>Data/pages.db</c> source and, since the fix, <c>Seed/tightwiki.seed.zip</c>'s own
        /// <c>DefaultPageFileAttachments/</c> manifest) to be real, stable built-in content, not incidental test
        /// data. Pre-fix, <c>GetDefaultPageFileAttachments.sql</c> did not exist and nothing in the seed pipeline
        /// ever referenced <c>Pages.PageFile</c>/<c>Pages.PageFileRevision</c> at all, so this attachment (and
        /// every other seeded one) was silently absent from a freshly seeded MSSQL/Postgres database. Purely
        /// read-only - no <c>finally</c> cleanup needed.
        /// </summary>
        [Fact]
        public async Task SeededHomePage_ImageAttachment_IsReadableViaPageFileRepositoryMethods()
        {
            var pageRepo = fixture.Artifacts.DatabaseManager.PageRepository;

            var navigation = TwNamespaceNavigation.CleanAndValidate("Home");
            var page = await pageRepo.GetPageInfoByNavigation(navigation)
                ?? throw new Exception($"Could not find the seeded page at navigation '{navigation}'.");

            var files = await pageRepo.GetPageFilesInfoByPageId(page.Id);
            var attachmentInfo = Assert.Single(files, f => f.Name == "MC Music.png");
            Assert.Equal(1, attachmentInfo.FileRevision);
            Assert.Equal("image/png", attachmentInfo.ContentType);

            var attachment = await pageRepo.GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(
                page.Navigation, attachmentInfo.FileNavigation, fileRevision: 1);
            Assert.NotNull(attachment);
            Assert.Equal("image/png", attachment!.ContentType);

            //A real PNG file signature - confirms this is genuine, readable image content (not empty/corrupted),
            //independently of Bug 4's own GZip-corruption concern (page file attachments were never
            //GZip-compressed in the first place, unlike Emoji.ImageData - see EfPageRepository, which has no
            //Compress/Decompress call anywhere).
            byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            Assert.True(attachment.Data.Length >= pngSignature.Length,
                $"Expected at least {pngSignature.Length} bytes, got {attachment.Data.Length}.");
            Assert.Equal(pngSignature, attachment.Data.Take(pngSignature.Length).ToArray());
        }

        /// <summary>
        /// Bug 3c (commit a71ee1a7) - documented as a pointer to existing coverage rather than a new test, per
        /// this task's own explicit fallback instruction ("pokud existující testy tenhle bug fakticky už
        /// regresně kryjí, napiš do komentáře proč a nepiš to znovu duplicitně, jen odkaž"). Confirmed by reading
        /// both the fix's own diff and the tests below that every one of the four metadata tables this bug left
        /// empty pre-fix is already exercised, non-trivially, against real seeded content:
        /// <list type="bullet">
        /// <item><description><b>Pages.PageToken</b> - <see cref="PageRepositorySearchTests.GetPageIdsByTokens_MatchesAllTokensSemantics"/>
        /// asserts <see cref="ITwPageRepository.GetPageIdsByTokens"/> resolves the seeded "Sandbox :: Default"
        /// page from its own real, seed-time-generated "sandbox"/"default" tokens - impossible if
        /// <see cref="ITwPageRepository.RefreshPageMetadata"/> (this bug's actual fix - see the fix's own commit
        /// message) never ran at seed time, since that page is never otherwise re-saved by anything in this test
        /// suite (<see cref="PageRepositorySearchTests"/>'s own remarks confirm it is read-only there).</description></item>
        /// <item><description><b>Pages.PageTag</b> - <see cref="PageRepositorySearchTests.GetAssociatedTags_ReturnsCoOccurringTags_ForSeededDraftTag"/>
        /// asserts the same seeded page's real "Draft" tag is queryable via
        /// <see cref="ITwPageRepository.GetAssociatedTags"/>/<see cref="ITwPageRepository.GetPageTagsById"/> - same
        /// "never re-saved, so this only exists if seed-time tokenization ran" reasoning.</description></item>
        /// <item><description><b>Pages.PageProcessingInstruction</b> - <see cref="PageRepositoryListingTests.GetAllDeletedPagesPaged_GetMissingPagesPaged_GetAllPagesByInstructionPaged_ReadOnlyChecks"/>
        /// asserts <see cref="ITwPageRepository.GetAllPagesByInstructionPaged"/> returns a non-empty result for the
        /// real <c>"protect"</c> instruction, baked into the seeded "Wiki Help :: Protect Instruction" page's own
        /// markup.</description></item>
        /// <item><description><b>Pages.PageReference</b> - the same <see cref="PageRepositoryListingTests"/> test
        /// asserts <see cref="ITwPageRepository.GetMissingPagesPaged"/> (which reads exclusively from
        /// Pages.PageReference - see <c>EfPageRepository.GetMissingPagesPaged</c>'s own remarks) is non-empty,
        /// reflecting a genuine broken reference baked into the seeded "Wiki Help :: Links" page's own
        /// markup.</description></item>
        /// </list>
        /// Every one of those four assertions runs against every provider, including SqlServer/Postgres, exactly
        /// the surface this bug broke - a regression of this exact bug (seeding skipping
        /// <see cref="ITwPageRepository.RefreshPageMetadata"/> again) would fail all four, not silently pass.
        /// Writing a fifth, narrower test here that only re-checked "is this table non-empty" would assert
        /// strictly less than what those four already do (they assert specific, real content, not just row
        /// presence), so it is deliberately not duplicated.
        /// </summary>
        [Fact(Skip = "Bug 3c is already regression-covered, non-trivially, by four existing assertions across " +
            "PageRepositorySearchTests/PageRepositoryListingTests - see this test's own doc comment for exactly " +
            "which ones and why a new, narrower test here would be strictly redundant. Documented pointer, not a " +
            "skipped/not-yet-implemented check, per this task's own explicit fallback instruction for this one " +
            "sub-bug.")]
        public Task Bug3c_SeededPageMetadataTokenization_AlreadyCoveredElsewhere_DocumentedNotDuplicated()
        {
            return Task.CompletedTask;
        }

        /// <summary>
        /// Bug 4 (commit 7eb2c329) - see this class's own remarks for the full writeup. Mirrors
        /// <see cref="DefaultsRepositoryTests.GetDefaultPageFileAttachments_GetDefaultEmojis_GetDefaultEmojiCategories_GetDefaultMenuItems_RespectDocumentedSqliteEmptyBehavior"/>'s
        /// own image-bytes-round-trip tail end (same reflection-based <c>ReadEmojiImageBytes</c> call, needed for
        /// the same reason - keeping this file <c>#if</c>-free and buildable under every <c>DataProvider</c>, per
        /// that test's own remarks), deliberately duplicated here (unlike Bug 3c above) because this task's own
        /// brief explicitly asks for a dedicated Bug 4 regression test, and because this version also positively
        /// asserts the bytes are <i>not</i> still GZip-wrapped (the literal, direct failure mode of the pre-fix
        /// bug - <c>SeedPackageGenerator</c> wrote <c>Emoji.ImageData</c>'s raw, GZip-compressed column bytes
        /// straight into the zip entry), not just that they happen to match a PNG signature (which that other
        /// test's own remarks note is already mutually exclusive with the GZip magic number, but does not spell
        /// out as its own explicit assertion the way this one does).
        /// </summary>
        [Fact]
        public async Task GetDefaultEmojis_ReadEmojiImageBytes_ReturnsValidUncompressedImageData_NotStillGZipCompressed()
        {
            var repo = fixture.Artifacts.DatabaseManager.DefaultsRepository;

            var emojis = await repo.GetDefaultEmojis();
            if (emojis.Count == 0)
            {
                //SQLite reference: ITwDefaultsRepository.GetDefaultEmojis is hardcoded to always return an empty
                //list (DefaultsRepositoryTests' own remarks) - the bug this test targets only ever affected
                //Seed/tightwiki.seed.zip, which SQLite never reads, so there is nothing to verify here.
                return;
            }

            //Not part of ITwDefaultsRepository itself (see DefaultsRepositoryTests' own remarks on
            //EfDefaultsRepository.ReadEmojiImageBytes for why), so located/invoked via reflection rather than a
            //direct, compile-time reference - keeps this file buildable under the default SQLite build, which
            //never references TightWiki.Data.EfCore at all.
            var readImageMethod = repo.GetType().GetMethod("ReadEmojiImageBytes", BindingFlags.Public | BindingFlags.Instance)
                ?? throw new Exception($"Could not find 'ReadEmojiImageBytes' on '{repo.GetType()}'.");

            var samplePngEmoji = emojis.First(e => e.ImageEntry.EndsWith(".png", StringComparison.OrdinalIgnoreCase));
            var imageBytesTask = (Task<byte[]>)readImageMethod.Invoke(repo, [samplePngEmoji.ImageEntry])!;
            var imageBytes = await imageBytesTask;

            Assert.NotEmpty(imageBytes);

            //The literal, direct failure mode of the pre-fix bug: SeedPackageGenerator wrote Emoji.ImageData's
            //raw, GZip-compressed column bytes straight into the zip entry, so pre-fix this would start with the
            //GZip magic number (0x1F 0x8B) instead of real image content.
            byte[] gzipSignature = [0x1F, 0x8B];
            var startsWithGZipSignature = imageBytes.Length >= gzipSignature.Length
                && imageBytes.Take(gzipSignature.Length).SequenceEqual(gzipSignature);
            Assert.False(startsWithGZipSignature,
                "Expected the seed package's emoji image bytes to be decompressed, not still GZip-wrapped.");

            //Positive confirmation: a real PNG file signature - not corrupted/empty, and (the two magic numbers
            //are mutually exclusive by construction) independently proves the GZip check above.
            byte[] pngSignature = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
            Assert.True(imageBytes.Length >= pngSignature.Length,
                $"Expected at least {pngSignature.Length} bytes, got {imageBytes.Length}.");
            Assert.Equal(pngSignature, imageBytes.Take(pngSignature.Length).ToArray());
        }
    }
}
