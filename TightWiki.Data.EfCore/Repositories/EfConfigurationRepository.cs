using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using NTDLS.Helpers;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Library.Security;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using ConfigEntities = TightWiki.Data.EfCore.Entities.Config;

namespace TightWiki.Data.EfCore.Repositories
{
    /// <summary>
    /// Provider-agnostic (SQL Server/Postgres, per Database-Providers-Plan.md chapter 3) LINQ-over-EF-Core
    /// implementation of <see cref="ITwConfigurationRepository"/>. Lives in the shared <c>TightWiki.Data.EfCore</c>
    /// project rather than a per-provider driver project, because - unlike raw SQL - LINQ against
    /// <see cref="TightWikiDbContext"/> is translated to the active provider's dialect by EF Core itself; the
    /// only provider-specific concern here (<see cref="GetQuotedCryptoCheckTableName"/>, for the one place this
    /// class has to fall back to raw SQL) is resolved dynamically via <see cref="ISqlGenerationHelper"/>, not
    /// hardcoded. Originally landed as a <c>SqlServerConfigurationRepository</c> stub under
    /// <c>TightWiki.Data.EfCore.SqlServer/Repositories/</c> in phase 2a.1; moved and implemented for real here in
    /// phase 2a.6 once it became clear the SQL Server driver project should stay a thin bootstrap/migrations
    /// shell (see chapter 3's "Tenký projekt" description of <c>TightWiki.Data.EfCore.SqlServer</c>).
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reference semantics throughout are the SQLite implementation, <c>TightWiki.Repository.ConfigurationRepository</c>,
    /// and its backing <c>Scripts\Get*.sql</c>/<c>Scripts\Save*.sql</c>/<c>Scripts\*MenuItem*.sql</c> files - see
    /// each method's doc comment below for the specific script it mirrors, including two behavioral quirks that
    /// are deliberately preserved rather than "fixed":
    /// <list type="bullet">
    /// <item><description><see cref="GetConfigurationEntryValuesByGroupNameAndEntryName"/> never decrypts its
    /// result even for an entry with <c>IsEncrypted = true</c>, because the SQL it mirrors
    /// (GetConfigurationEntryValuesByGroupNameAndEntryName.sql) never selects <c>IsEncrypted</c> in the first
    /// place - unlike GetConfigurationEntryValuesByGroupName.sql, which does. Since <see cref="Get{T}(string,string)"/>/
    /// <see cref="Get{T}(string,string,T)"/> are both built on top of this method, this means encrypted values are
    /// only ever correctly readable via <see cref="GetConfigurationEntryValuesByGroupName"/>.</description></item>
    /// <item><description><see cref="SaveConfigurationEntryValueByGroupAndEntry"/> does not clear the
    /// <see cref="MemCache.Category.Configuration"/> cache category after writing - unlike
    /// <see cref="DeleteMenuItemById"/>/<see cref="UpdateMenuItemById"/>/<see cref="InsertMenuItem"/>, which do -
    /// because the SQLite reference doesn't either.</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/>/<see cref="Func{ApplicationDbContext}"/> pair rather than an
    /// injected context instance, mirroring how <c>SqlServerDatabaseManager</c>'s own <c>CreateDbContext</c>/
    /// <c>CreateApplicationDbContext</c> private methods are used everywhere else in that class (a fresh, short-lived
    /// context per operation, disposed immediately after) - <c>SqlServerDatabaseManager</c> passes its own
    /// <c>CreateDbContext</c>/<c>CreateApplicationDbContext</c> method groups in as those two delegates. The second
    /// delegate is needed only by <see cref="GetWikiDatabaseMetrics"/>, which - like its SQLite reference
    /// (<c>ConfigurationRepository.GetWikiDatabaseMetrics</c>, which attaches <c>users.db</c>) - reads a user count
    /// that lives in ASP.NET Core Identity's <c>AspNetUsers</c> table, i.e. a different
    /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/> (<see cref="ApplicationDbContext"/>) than the rest of
    /// this class's members use.
    /// </para>
    /// </remarks>
    public sealed class EfConfigurationRepository : ITwConfigurationRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly Func<ApplicationDbContext> _createIdentityContext;

        public EfConfigurationRepository(Func<TightWikiDbContext> createContext, Func<ApplicationDbContext> createIdentityContext)
        {
            _createContext = createContext;
            _createIdentityContext = createIdentityContext;
        }

        /// <summary>
        /// Mirrors GetConfigurationEntryValuesByGroupName.sql - an inner join of ConfigurationEntry to
        /// ConfigurationGroup filtered by group name, decrypting any entry with <c>IsEncrypted = true</c>
        /// (silently returning "" for an entry that fails to decrypt, e.g. because the machine key changed - same
        /// fallback as the SQLite reference). Cached under <see cref="MemCache.Category.Configuration"/>, same
        /// cache key shape as the SQLite reference.
        /// </summary>
        public async Task<TwConfigurationEntries> GetConfigurationEntryValuesByGroupName(string groupName)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Configuration, [groupName]);

            return await MemCache.AddOrGet(cacheKey, async () =>
            {
                using var context = _createContext();

                var entries = await (
                    from entry in context.ConfigurationEntries
                    join grp in context.ConfigurationGroups on entry.ConfigurationGroupId equals grp.Id
                    where grp.Name == groupName
                    select new TwConfigurationEntry
                    {
                        Id = entry.Id,
                        ConfigurationGroupId = entry.ConfigurationGroupId,
                        Name = entry.Name,
                        Value = entry.Value ?? string.Empty,
                        IsEncrypted = entry.IsEncrypted,
                        Description = entry.Description ?? string.Empty,
                    }).ToListAsync();

                foreach (var entry in entries)
                {
                    if (entry.IsEncrypted)
                    {
                        try
                        {
                            entry.Value = SecurityUtility.DecryptString(SecurityUtility.MachineKey, entry.Value);
                        }
                        catch
                        {
                            entry.Value = "";
                        }
                    }
                }

                return new TwConfigurationEntries(entries);
            }).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetAllThemes.sql - every Config.Theme row, with <see cref="TwTheme.DelimitedFiles"/> parsed
        /// into <see cref="TwTheme.Files"/>. Cached under <see cref="MemCache.Category.Configuration"/> (a single,
        /// argument-less cache key, since this method takes no parameters), same as the SQLite reference.
        /// </summary>
        public async Task<List<TwTheme>> GetAllThemes()
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Configuration);

            return await MemCache.AddOrGet(cacheKey, async () =>
            {
                using var context = _createContext();

                var themes = await context.Themes.Select(t => new TwTheme
                {
                    Name = t.Name,
                    DelimitedFiles = t.DelimitedFiles,
                    ClassNavBar = t.ClassNavBar,
                    ClassNavLink = t.ClassNavLink,
                    ClassDropdown = t.ClassDropdown,
                    ClassBranding = t.ClassBranding,
                    EditorTheme = t.EditorTheme,
                }).ToListAsync();

                foreach (var theme in themes)
                {
                    theme.Files = theme.DelimitedFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
                }

                return themes;
            }).EnsureNotNull();
        }

        /// <summary>
        /// Mirrors GetWikiDatabaseStatistics.sql. Unlike the rest of this class, reads from two separate
        /// <see cref="Microsoft.EntityFrameworkCore.DbContext"/>s - <see cref="TightWikiDbContext"/> for
        /// everything Pages/Users-schema, and <see cref="ApplicationDbContext"/> (Identity) for the
        /// <see cref="TwWikiDatabaseStatistics.Users"/> count (AspNetUsers), matching the SQLite reference's
        /// <c>o.Attach("users.db", "users_db")</c> reaching into the Identity database for the same column. Each
        /// count is awaited sequentially rather than concurrently - a single <see cref="Microsoft.EntityFrameworkCore.DbContext"/>
        /// instance does not support overlapping operations.
        /// </summary>
        public async Task<TwWikiDatabaseStatistics> GetWikiDatabaseMetrics()
        {
            using var context = _createContext();
            using var identityContext = _createIdentityContext();

            return new TwWikiDatabaseStatistics
            {
                Pages = await context.Pages_Pages.CountAsync(),
                Namespaces = await context.Pages_Pages.Select(p => p.Namespace).Distinct().CountAsync(),
                IntraLinks = await context.PageReferences.CountAsync(),
                PageRevisions = await context.Pages_PageRevisions.CountAsync(),
                PageAttachments = await context.Pages_PageFiles.CountAsync(),
                PageAttachmentRevisions = await context.Pages_PageFileRevisions.CountAsync(),
                PageTags = await context.Pages_PageTags.CountAsync(),
                PageSearchTokens = await context.Pages_PageTokens.CountAsync(),
                Profiles = await context.Profiles.CountAsync(),
                Users = await identityContext.Users.CountAsync(),
            };
        }

        /// <summary>
        /// Mirrors <c>ConfigurationRepository.IsFirstRun</c> exactly (no SQL script of its own - it's pure C# glue
        /// around <see cref="GetCryptoCheck"/>/<see cref="SetCryptoCheck"/>): if the crypto check fails, writes a
        /// fresh one and reports "yes, first run"; otherwise reports "no".
        /// </summary>
        public async Task<bool> IsFirstRun()
        {
            bool isEncryptionValid = await GetCryptoCheck();
            if (isEncryptionValid == false)
            {
                await SetCryptoCheck();
                return true;
            }
            return false;
        }

        /// <summary>
        /// Mirrors GetCryptoCheck.sql: reads the single-row (possibly absent) Config.CryptoCheck.Content value and
        /// reports whether it decrypts back to the expected marker string.
        /// </summary>
        public async Task<bool> GetCryptoCheck()
        {
            using var context = _createContext();

            //Config.CryptoCheck is modeled with HasNoKey() (see CryptoCheckConfiguration) - a plain query like
            //this is exactly what keyless entity types remain fully capable of.
            var value = await context.CryptoChecks.Select(c => c.Content).FirstOrDefaultAsync() ?? string.Empty;

            try
            {
                value = SecurityUtility.DecryptString(SecurityUtility.MachineKey, value);
                if (value == Constants.CRYPTOCHECK)
                {
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        /// <summary>
        /// Mirrors SetCryptoCheck.sql ("DELETE FROM CryptoCheck; INSERT INTO CryptoCheck(Content) SELECT
        /// @Content"). Config.CryptoCheck is a keyless entity type (<c>HasNoKey()</c>), so it cannot be tracked for
        /// <c>Add</c>/<c>Remove</c> like every other entity in this class - the delete uses EF Core's LINQ bulk
        /// <c>ExecuteDeleteAsync</c> (fully provider-portable, no raw SQL needed), and the insert falls back to a
        /// single parameterized <c>INSERT</c> statement whose table/schema identifier <em>and</em> column
        /// identifier are both resolved and quoted via <see cref="ISqlGenerationHelper"/> (see
        /// <see cref="GetQuotedCryptoCheckTableName"/>/<see cref="GetQuotedCryptoCheckContentColumnName"/>) so it
        /// stays correct for whichever relational provider is active, rather than hardcoding SQL Server's
        /// <c>[bracket]</c> quoting - or, worse, emitting the column name unquoted, which on Postgres (whose
        /// migrations create a quoted, PascalCase <c>"Content"</c> column) resolves to the lowercase-folded
        /// <c>content</c> and fails with "column "content" ... does not exist".
        /// </summary>
        public async Task SetCryptoCheck()
        {
            using var context = _createContext();

            var content = SecurityUtility.EncryptString(SecurityUtility.MachineKey, Constants.CRYPTOCHECK);

            await context.CryptoChecks.ExecuteDeleteAsync();

            //Built via plain string concatenation, not a C# interpolated-string literal, so that EF Core's
            //"don't hand raw SQL an interpolated string" analyzer (EF1002) does not flag this - "{0}" below is a
            //literal placeholder for ExecuteSqlRawAsync's own (safe, provider-parameterized) substitution, not a
            //C# interpolation hole. quotedTable/quotedColumn themselves come only from trusted EF metadata (never
            //user input), so splicing them into the SQL text is not an injection risk.
            var quotedTable = GetQuotedCryptoCheckTableName(context);
            var quotedColumn = GetQuotedCryptoCheckContentColumnName(context);
            var insertSql = "INSERT INTO " + quotedTable + " (" + quotedColumn + ") VALUES ({0})";
            await context.Database.ExecuteSqlRawAsync(insertSql, content);
        }

        private static string GetQuotedCryptoCheckTableName(TightWikiDbContext context)
        {
            var entityType = context.Model.FindEntityType(typeof(ConfigEntities.CryptoCheck))
                ?? throw new InvalidOperationException(
                    $"'{typeof(ConfigEntities.CryptoCheck)}' is not part of the {nameof(TightWikiDbContext)} model.");

            var sqlGenerationHelper = context.GetService<ISqlGenerationHelper>();
            return sqlGenerationHelper.DelimitIdentifier(entityType.GetTableName()!, entityType.GetSchema());
        }

        private static string GetQuotedCryptoCheckContentColumnName(TightWikiDbContext context)
        {
            var entityType = context.Model.FindEntityType(typeof(ConfigEntities.CryptoCheck))
                ?? throw new InvalidOperationException(
                    $"'{typeof(ConfigEntities.CryptoCheck)}' is not part of the {nameof(TightWikiDbContext)} model.");

            var property = entityType.FindProperty(nameof(ConfigEntities.CryptoCheck.Content))
                ?? throw new InvalidOperationException(
                    $"'{nameof(ConfigEntities.CryptoCheck.Content)}' is not part of the {typeof(ConfigEntities.CryptoCheck)} model.");

            var sqlGenerationHelper = context.GetService<ISqlGenerationHelper>();
            return sqlGenerationHelper.DelimitIdentifier(property.GetColumnName());
        }

        /// <summary>
        /// Mirrors SaveConfigurationEntryValueByGroupAndEntry.sql: updates <c>Value</c> on the ConfigurationEntry
        /// row(s) matching both <paramref name="entryName"/> and a ConfigurationGroupId resolved from
        /// <paramref name="groupName"/> (a correlated subquery in the original SQL, an <c>IN</c>-style
        /// <see cref="Enumerable.Contains{TSource}(IEnumerable{TSource},TSource)"/> filter here so this stays a
        /// single-table bulk <c>ExecuteUpdateAsync</c>). Deliberately does <b>not</b> clear
        /// <see cref="MemCache.Category.Configuration"/> afterward - see this class's doc comment.
        /// </summary>
        public async Task SaveConfigurationEntryValueByGroupAndEntry(string groupName, string entryName, string value)
        {
            using var context = _createContext();

            var matchingGroupIds = context.ConfigurationGroups.Where(g => g.Name == groupName).Select(g => g.Id);

            await context.ConfigurationEntries
                .Where(e => e.Name == entryName && matchingGroupIds.Contains(e.ConfigurationGroupId))
                .ExecuteUpdateAsync(setters => setters.SetProperty(e => e.Value, value));
        }

        /// <summary>
        /// Mirrors <c>ConfigurationRepository.GetConfigurationNest</c> (pure C# glue, no SQL script of its own):
        /// re-shapes <see cref="GetFlatConfiguration"/>'s flat rows into one <see cref="TwConfigurationNest"/> per
        /// group, decrypting encrypted entry values along the way (this is the one place decryption happens for
        /// data sourced from <see cref="GetFlatConfiguration"/>/GetFlatConfiguration.sql, which - unlike
        /// GetConfigurationEntryValuesByGroupName.sql - does select <c>IsEncrypted</c>).
        /// </summary>
        public async Task<List<TwConfigurationNest>> GetConfigurationNest()
        {
            var result = new List<TwConfigurationNest>();
            var flatConfig = await GetFlatConfiguration();

            var groups = flatConfig.GroupBy(o => o.GroupId);
            foreach (var group in groups)
            {
                var nest = new TwConfigurationNest
                {
                    Id = group.Key,
                    Name = group.Select(o => o.GroupName).First(),
                    Description = group.Select(o => o.GroupDescription).First()
                };

                foreach (var value in group.OrderBy(o => o.EntryName))
                {
                    string entryValue;
                    if (value.IsEncrypted)
                    {
                        try
                        {
                            entryValue = SecurityUtility.DecryptString(SecurityUtility.MachineKey, value.EntryValue);
                        }
                        catch
                        {
                            entryValue = "";
                        }
                    }
                    else
                    {
                        entryValue = value.EntryValue;
                    }

                    nest.Entries.Add(new TwConfigurationEntry()
                    {
                        Id = value.EntryId,
                        Value = entryValue,
                        Description = value.EntryDescription,
                        Name = value.EntryName,
                        DataType = value.DataType.ToLowerInvariant(),
                        IsEncrypted = value.IsEncrypted,
                        ConfigurationGroupId = group.Key,
                    });
                }
                result.Add(nest);
            }

            return result;
        }

        /// <summary>
        /// Mirrors GetFlatConfiguration.sql - an inner join of ConfigurationEntry, ConfigurationGroup and DataType
        /// (so, like the SQL, an entry whose DataTypeId doesn't resolve to a DataType row is silently excluded),
        /// ordered by group name then entry name.
        /// </summary>
        public async Task<List<TwConfigurationFlat>> GetFlatConfiguration()
        {
            using var context = _createContext();

            var query =
                from entry in context.ConfigurationEntries
                join grp in context.ConfigurationGroups on entry.ConfigurationGroupId equals grp.Id
                join dataType in context.DataTypes on entry.DataTypeId equals dataType.Id
                orderby grp.Name, entry.Name
                select new TwConfigurationFlat
                {
                    GroupId = grp.Id,
                    GroupName = grp.Name,
                    GroupDescription = grp.Description ?? string.Empty,
                    EntryId = entry.Id,
                    EntryName = entry.Name,
                    EntryValue = entry.Value ?? string.Empty,
                    EntryDescription = entry.Description ?? string.Empty,
                    IsEncrypted = entry.IsEncrypted,
                    IsRequired = entry.IsRequired,
                    DataType = dataType.Name ?? string.Empty,
                };

            return await query.ToListAsync();
        }

        /// <summary>
        /// Mirrors GetConfigurationEntryValuesByGroupNameAndEntryName.sql - see this class's doc comment for the
        /// deliberately-preserved "never decrypts" quirk this inherits from that script not selecting
        /// <c>IsEncrypted</c>. Cached under <see cref="MemCache.Category.Configuration"/>, same cache key shape as
        /// the SQLite reference. The (ConfigurationGroupId, Name) uniqueness constraint on ConfigurationEntry (see
        /// <c>ConfigurationEntryConfiguration</c>) plus the uniqueness constraint on ConfigurationGroup.Name (see
        /// <c>ConfigurationGroupConfiguration</c>) together guarantee at most one row can ever match, so
        /// <see cref="Queryable.FirstOrDefaultAsync{TSource}(IQueryable{TSource},System.Threading.CancellationToken)"/>
        /// is equivalent to a "single" lookup here.
        /// </summary>
        public async Task<string?> GetConfigurationEntryValuesByGroupNameAndEntryName(string groupName, string entryName)
        {
            var cacheKey = MemCacheKeyFunction.Build(MemCache.Category.Configuration, [groupName, entryName]);

            return await MemCache.AddOrGetAsync(cacheKey, async () =>
            {
                using var context = _createContext();

                var value = await (
                    from entry in context.ConfigurationEntries
                    join grp in context.ConfigurationGroups on entry.ConfigurationGroupId equals grp.Id
                    where grp.Name == groupName && entry.Name == entryName
                    select entry.Value).FirstOrDefaultAsync();

                return value;
            });
        }

        /// <summary>
        /// Mirrors <c>ConfigurationRepository.Get&lt;T&gt;(string, string)</c>: throws if the entry does not exist
        /// (via <see cref="NTDLS.Helpers.NullSafeExtensions.EnsureNotNull{T}(T?, string?)"/>), otherwise converts
        /// its raw string value to <typeparamref name="T"/>.
        /// </summary>
        public async Task<T?> Get<T>(string groupName, string entryName)
        {
            var value = await GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);
            return Converters.ConvertTo<T>(value.EnsureNotNull());
        }

        /// <summary>
        /// Mirrors <c>ConfigurationRepository.Get&lt;T&gt;(string, string, T)</c>: returns
        /// <paramref name="defaultValue"/> if the entry does not exist, otherwise converts its raw string value to
        /// <typeparamref name="T"/>.
        /// </summary>
        public async Task<T> Get<T>(string groupName, string entryName, T defaultValue)
        {
            var value = await GetConfigurationEntryValuesByGroupNameAndEntryName(groupName, entryName);

            if (value == null)
            {
                return defaultValue;
            }

            return Converters.ConvertTo<T>(value);
        }

        #region Menu Items.

        /// <summary>
        /// Mirrors GetAllMenuItems.sql: defaults to <c>ORDER BY Ordinal</c> (ascending) when
        /// <paramref name="orderBy"/> is omitted; otherwise orders by whichever of Id/Name/Link/Ordinal
        /// <paramref name="orderBy"/> names (case-insensitively - matching the script's
        /// <c>StringComparer.InvariantCultureIgnoreCase</c> field-mapping dictionary, via
        /// <c>RepositoryHelpers.TransposeOrderby</c>), ascending only when <paramref name="orderByDirection"/> is
        /// exactly "asc" (case-insensitively) and descending for anything else, including null - same as the
        /// script's own direction handling.
        /// </summary>
        public async Task<List<TwMenuItem>> GetAllMenuItems(string? orderBy = null, string? orderByDirection = null)
        {
            using var context = _createContext();

            IQueryable<ConfigEntities.MenuItem> query;

            if (string.IsNullOrEmpty(orderBy))
            {
                query = context.MenuItems.OrderBy(m => m.Ordinal);
            }
            else
            {
                bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

                query = orderBy.ToUpperInvariant() switch
                {
                    "ID" => ascending
                        ? context.MenuItems.OrderBy(m => m.Id)
                        : context.MenuItems.OrderByDescending(m => m.Id),
                    "NAME" => ascending
                        ? context.MenuItems.OrderBy(m => m.Name)
                        : context.MenuItems.OrderByDescending(m => m.Name),
                    "LINK" => ascending
                        ? context.MenuItems.OrderBy(m => m.Link)
                        : context.MenuItems.OrderByDescending(m => m.Link),
                    "ORDINAL" => ascending
                        ? context.MenuItems.OrderBy(m => m.Ordinal)
                        : context.MenuItems.OrderByDescending(m => m.Ordinal),
                    _ => throw new InvalidOperationException(
                        $"No order by mapping was found in 'GetAllMenuItems.sql' for the field '{orderBy}'."),
                };
            }

            return await query.Select(m => new TwMenuItem
            {
                Id = m.Id,
                Name = m.Name,
                Link = m.Link,
                Ordinal = m.Ordinal,
            }).ToListAsync();
        }

        /// <summary>
        /// Mirrors GetMenuItemById.sql, via <see cref="Queryable.SingleAsync{TSource}(IQueryable{TSource},System.Threading.CancellationToken)"/> -
        /// Id is the primary key, so at most one row can ever match; like the SQLite reference's
        /// <c>QuerySingleAsync</c>, this throws if no row matches rather than returning null, matching the
        /// interface's non-nullable <see cref="TwMenuItem"/> return type.
        /// </summary>
        public async Task<TwMenuItem> GetMenuItemById(int id)
        {
            using var context = _createContext();

            return await context.MenuItems
                .Where(m => m.Id == id)
                .Select(m => new TwMenuItem
                {
                    Id = m.Id,
                    Name = m.Name,
                    Link = m.Link,
                    Ordinal = m.Ordinal,
                }).SingleAsync();
        }

        /// <summary>
        /// Mirrors DeleteMenuItemById.sql, then clears <see cref="MemCache.Category.Configuration"/> - same as
        /// the SQLite reference.
        /// </summary>
        public async Task DeleteMenuItemById(int id)
        {
            using var context = _createContext();

            await context.MenuItems.Where(m => m.Id == id).ExecuteDeleteAsync();

            MemCache.ClearCategory(MemCache.Category.Configuration);
        }

        /// <summary>
        /// Mirrors UpdateMenuItemById.sql, then clears <see cref="MemCache.Category.Configuration"/> - same as
        /// the SQLite reference. Returns <paramref name="menuItem"/>'s own Id: the SQLite reference's underlying
        /// UPDATE statement has no trailing <c>SELECT</c> (unlike InsertMenuItem.sql's
        /// <c>SELECT last_insert_rowid()</c>) and its return value is never consumed by any caller
        /// (<c>AdminController.cs</c> only awaits it), so there is no meaningful value to reproduce here beyond
        /// the id that was already known going in.
        /// </summary>
        public async Task<int> UpdateMenuItemById(TwMenuItem menuItem)
        {
            using var context = _createContext();

            await context.MenuItems
                .Where(m => m.Id == menuItem.Id)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(m => m.Name, menuItem.Name)
                    .SetProperty(m => m.Link, menuItem.Link)
                    .SetProperty(m => m.Ordinal, menuItem.Ordinal));

            MemCache.ClearCategory(MemCache.Category.Configuration);
            return menuItem.Id;
        }

        /// <summary>
        /// Mirrors InsertMenuItem.sql ("... SELECT last_insert_rowid()"): inserts a new Config.MenuItem row and
        /// returns its generated Id, then clears <see cref="MemCache.Category.Configuration"/> - same as the
        /// SQLite reference.
        /// </summary>
        public async Task<int> InsertMenuItem(TwMenuItem menuItem)
        {
            using var context = _createContext();

            var entity = new ConfigEntities.MenuItem
            {
                Name = menuItem.Name,
                Link = menuItem.Link,
                Ordinal = menuItem.Ordinal,
            };

            context.MenuItems.Add(entity);
            await context.SaveChangesAsync();

            MemCache.ClearCategory(MemCache.Category.Configuration);
            return entity.Id;
        }

        #endregion
    }
}
