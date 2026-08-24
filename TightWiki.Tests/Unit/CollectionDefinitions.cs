namespace TightWiki.Tests.Unit
{
    /// <summary>
    /// Central home for every xunit <c>[CollectionDefinition]</c> declaration used by <c>TightWiki.Tests</c>, so
    /// they don't have to be hunted down one-per-file across ~20 test classes. All test classes in this project
    /// share the same underlying, persistent seeded database (via <see cref="TwEngineFixture"/> - see chapter 5.3
    /// of <c>Database-Providers-Testing-Plan.md</c>, not a fresh-empty database, and not per-test transaction
    /// rollback), and xunit v2 by default runs different collections concurrently with each other (only tests
    /// *within* one collection are serialized). Several classes (e.g. <c>PageRepositoryDeleteRestoreTests</c>,
    /// <c>PageRepositoryCrudAttachmentTests</c>) mutate that shared database, while other, nominally read-only
    /// classes (e.g. <c>FullPageTests</c>, <c>PageRepositoryListingTests</c>) read unfiltered/whole-table listings
    /// (<c>RecentlyCreated</c>/<c>RecentlyModified</c>/<c>MostEdited</c>/<c>GetAllPagesPaged</c> and similar) with
    /// no scoping to rows the test itself created - those reads can observe a transient, in-flight state from a
    /// concurrently-running mutating collection and fail non-deterministically (documented, reproduced case:
    /// <c>PageRepositoryListingTests.GetAllPagesPaged_DefaultAndExplicitOrdering_ReturnsConsistentPagination</c>,
    /// finding #5 in <c>Database-Providers-Testing-Findings.md</c>). Every collection below therefore opts out of
    /// cross-collection parallelism via <c>DisableParallelization = true</c>, trading the suite's inter-collection
    /// parallelism for deterministic, non-flaky results - tests within a collection were already sequential, so
    /// this only serializes collections against *each other*, not individual test methods against themselves.
    /// </summary>
    [CollectionDefinition("Bootstrap Seed Regression Tests", DisableParallelization = true)]
    public class BootstrapSeedRegressionTestsDefinition { }

    [CollectionDefinition("Defaults Repository Tests", DisableParallelization = true)]
    public class DefaultsRepositoryTestsDefinition { }

    [CollectionDefinition("Database Tests", DisableParallelization = true)]
    public class DatabaseTestsDefinition { }

    [CollectionDefinition("Emoji Repository Tests", DisableParallelization = true)]
    public class EmojiRepositoryTestsDefinition { }

    [CollectionDefinition("Fresh Database And Idempotence Tests", DisableParallelization = true)]
    public class FreshDatabaseAndIdempotenceTestsDefinition { }

    /// <summary>
    /// Shared by both <c>UsersRepositoryPermissionAuthTests</c> and <c>DataIntegrityRegressionTests</c> - the
    /// latter deliberately joins this collection rather than defining its own (see its own class-level remarks).
    /// </summary>
    [CollectionDefinition("Users Repository Permission Auth Tests", DisableParallelization = true)]
    public class UsersRepositoryPermissionAuthTestsDefinition { }

    [CollectionDefinition("Full Page Tests", DisableParallelization = true)]
    public class FullPageTestsDefinition { }

    [CollectionDefinition("Configuration Repository Tests", DisableParallelization = true)]
    public class ConfigurationRepositoryTestsDefinition { }

    [CollectionDefinition("Markup Tests", DisableParallelization = true)]
    public class MarkupTestsDefinition { }

    [CollectionDefinition("Logging Repository Tests", DisableParallelization = true)]
    public class LoggingRepositoryTestsDefinition { }

    [CollectionDefinition("Page Repository Crud Attachment Tests", DisableParallelization = true)]
    public class PageRepositoryCrudAttachmentTestsDefinition { }

    [CollectionDefinition("Page Repository Listing Tests", DisableParallelization = true)]
    public class PageRepositoryListingTestsDefinition { }

    [CollectionDefinition("Page Repository Metadata Tests", DisableParallelization = true)]
    public class PageRepositoryMetadataTestsDefinition { }

    [CollectionDefinition("Page Repository Delete Restore Tests", DisableParallelization = true)]
    public class PageRepositoryDeleteRestoreTestsDefinition { }

    [CollectionDefinition("Page Repository Search Tests", DisableParallelization = true)]
    public class PageRepositorySearchTestsDefinition { }

    [CollectionDefinition("Statistics Repository Tests", DisableParallelization = true)]
    public class StatisticsRepositoryTestsDefinition { }

    [CollectionDefinition("Spanned Repository Admin Operations Tests", DisableParallelization = true)]
    public class SpannedRepositoryAdminOperationsTestsDefinition { }

    [CollectionDefinition("Users Repository Profile Tests", DisableParallelization = true)]
    public class UsersRepositoryProfileTestsDefinition { }

    [CollectionDefinition("Users Repository Role Tests", DisableParallelization = true)]
    public class UsersRepositoryRoleTestsDefinition { }
}
