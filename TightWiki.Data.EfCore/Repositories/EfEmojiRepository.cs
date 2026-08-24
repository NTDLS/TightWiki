using ImageMagick;
using Microsoft.EntityFrameworkCore;
using System.Runtime.Caching;
using TightWiki.Library;
using TightWiki.Library.Caching;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Models;
using EmojiEntities = TightWiki.Data.EfCore.Entities.Emoji;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Data.EfCore.Repositories
{
    /// <summary>
    /// Provider-agnostic (SQL Server/Postgres, per Database-Providers-Plan.md chapter 3) LINQ-over-EF-Core
    /// implementation of <see cref="ITwEmojiRepository"/>. Lives in the shared <c>TightWiki.Data.EfCore</c>
    /// project rather than a per-provider driver project, for the same reason as <see cref="EfConfigurationRepository"/>/
    /// <see cref="EfLoggingRepository"/> (see those classes' doc comments): plain LINQ against
    /// <see cref="TightWikiDbContext"/> needs no provider-specific code here at all. Originally landed as a
    /// <c>SqlServerEmojiRepository</c> stub under <c>TightWiki.Data.EfCore.SqlServer/Repositories/</c> in phase
    /// 2a.1; moved and implemented for real here in phase 2a.8.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Reference semantics throughout are the SQLite implementation, <c>TightWiki.Repository.EmojiRepository</c>,
    /// and its backing <c>Scripts\*Emoji*.sql</c> files - see each method's doc comment below for the specific
    /// script it mirrors, including behavioral quirks that are deliberately preserved rather than "fixed" (and one
    /// that is <b>not</b> preserved - see <see cref="UpsertEmoji"/>'s doc comment).
    /// </para>
    /// <para>
    /// <see cref="Entities.Emoji.Emoji.ImageData"/> is stored GZip-compressed in the database, on every provider
    /// (see commit 7eb2c329 / Database-Providers-Plan.md's task context for this phase). The runtime
    /// (<c>TightWiki/Controllers/FileController.cs</c>, <see cref="Utility.Decompress"/>) always decompresses on
    /// read, so every read method here (<see cref="GetEmojiByName"/>) returns <see cref="TwEmoji.ImageData"/>
    /// exactly as stored - raw, still compressed - and <see cref="UpsertEmoji"/> compresses new image bytes with
    /// <see cref="Utility.Compress"/> before writing, matching the SQLite reference exactly. No other method here
    /// touches <c>ImageData</c> at all (not even <see cref="GetAllEmojis"/>/<see cref="GetAllEmojisPaged"/>,
    /// mirroring their scripts not selecting the column).
    /// </para>
    /// <para>
    /// Takes a <see cref="Func{TightWikiDbContext}"/> rather than an injected context instance, mirroring
    /// <see cref="EfConfigurationRepository"/>/<see cref="EfLoggingRepository"/> - see those classes' doc comments
    /// for why. Also takes an <see cref="ITwConfigurationRepository"/> instance directly (not another
    /// <see cref="Func{TResult}"/>), mirroring the SQLite reference constructor's own
    /// <c>ConfigurationRepository configurationRepository</c> parameter - <see cref="GetAllEmojisPaged"/> is the
    /// only member that needs it, to read the "Pagination Size" customization setting.
    /// </para>
    /// </remarks>
    public sealed class EfEmojiRepository : ITwEmojiRepository
    {
        private readonly Func<TightWikiDbContext> _createContext;
        private readonly ITwConfigurationRepository _configurationRepository;

        public EfEmojiRepository(Func<TightWikiDbContext> createContext, ITwConfigurationRepository configurationRepository)
        {
            _createContext = createContext;
            _configurationRepository = configurationRepository;
        }

        /// <summary>
        /// Mirrors GetAllEmojis.sql - every Emoji row (Id/Name/MimeType only, no <c>ImageData</c>/categories),
        /// with <see cref="TwEmoji.Shortcut"/> computed as <c>"%%" + lower(Name) + "%%"</c>, ordered by Name.
        /// </summary>
        public async Task<List<TwEmoji>> GetAllEmojis()
        {
            using var context = _createContext();

            return await context.Emojis
                .OrderBy(e => e.Name)
                .Select(e => new TwEmoji
                {
                    Id = e.Id,
                    Name = e.Name,
                    MimeType = e.MimeType ?? string.Empty,
                    Shortcut = "%%" + e.Name.ToLower() + "%%",
                })
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors AutoCompleteEmoji.sql - Emoji names whose own Name contains <paramref name="term"/>, or that
        /// have at least one category whose name contains <paramref name="term"/> (an <c>EXISTS</c> subquery in
        /// the original SQL, an equivalent <see cref="Queryable.Any{TSource}(IQueryable{TSource})"/> here),
        /// ordered by Name and capped at 25 rows (<c>LIMIT 25</c>).
        /// </summary>
        public async Task<List<string>> AutoCompleteEmoji(string term)
        {
            using var context = _createContext();

            return await context.Emojis
                .Where(e => e.Name.Contains(term)
                    || context.EmojiCategories.Any(ec => ec.EmojiId == e.Id && ec.Category.Contains(term)))
                .OrderBy(e => e.Name)
                .Select(e => e.Name)
                .Take(25)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetEmojisByCategory.sql - every distinct Emoji belonging to the given <paramref name="category"/>
        /// (exact match, unlike <see cref="AutoCompleteEmoji"/>'s substring match), ordered by Name.
        /// </summary>
        public async Task<List<TwEmoji>> GetEmojisByCategory(string category)
        {
            using var context = _createContext();

            return await (
                from e in context.Emojis
                join ec in context.EmojiCategories on e.Id equals ec.EmojiId
                where ec.Category == category
                select new TwEmoji
                {
                    Id = e.Id,
                    Name = e.Name,
                    MimeType = e.MimeType ?? string.Empty,
                    Shortcut = "%%" + e.Name.ToLower() + "%%",
                })
                .Distinct()
                .OrderBy(e => e.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetEmojiCategoriesGrouped.sql: every distinct category name that has at least one Emoji/
        /// EmojiCategory pairing where the Emoji's own Name does <b>not</b> contain the category name as a
        /// substring (the script's <c>E.[Name] NOT LIKE '%' || EC.Category || '%'</c> filter - kept verbatim,
        /// including its odd "exclude self-describing categories" effect, since it is exactly what the SQLite
        /// reference does), ordered by Category. <see cref="TwEmojiCategory.EmojiCount"/> is a separate,
        /// <b>unfiltered</b> count of every Emoji/EmojiCategory pairing for that category name (mirroring the
        /// script's correlated subquery, which has no such filter) - computed as one grouped query rather than the
        /// per-row subquery the script uses, to avoid N+1 round trips.
        /// </summary>
        public async Task<List<TwEmojiCategory>> GetEmojiCategoriesGrouped()
        {
            using var context = _createContext();

            var countsByCategory = await (
                from iec in context.EmojiCategories
                join ie in context.Emojis on iec.EmojiId equals ie.Id
                group iec by iec.Category into g
                select new { Category = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Category, x => x.Count, StringComparer.OrdinalIgnoreCase);

            var filteredCategories = await (
                from ec in context.EmojiCategories
                join e in context.Emojis on ec.EmojiId equals e.Id
                where !e.Name.Contains(ec.Category)
                select ec.Category)
                .Distinct()
                .OrderBy(c => c)
                .ToListAsync();

            return filteredCategories.Select(category => new TwEmojiCategory
            {
                Category = category,
                EmojiCount = (countsByCategory.TryGetValue(category, out var count) ? count : 0).ToString(),
            }).ToList();
        }

        /// <summary>
        /// Mirrors SearchEmojiCategoryIds.sql: for every entry in <paramref name="categories"/>, finds
        /// EmojiCategory rows whose Category starts with that entry (a prefix match - the script's
        /// <c>EC.Category LIKE SS.[value] || '%'</c> - via a per-term <see cref="Queryable.Concat{TSource}"/>,
        /// i.e. the SQL equivalent of the script's <c>INNER JOIN</c> against the caller's search-term list), then
        /// groups the matches by EmojiId and keeps only groups with at least <paramref name="categories"/>.Count
        /// matches (the script's <c>HAVING COUNT(0) &gt;= @SearchTokenCount</c> - so an EmojiId whose categories
        /// satisfy every requested term, counting one match per term/category pairing exactly like the SQL join
        /// does).
        /// </summary>
        /// <remarks>
        /// The script's own <c>SELECT EC.Id ... GROUP BY EmojiId</c> selects a bare, non-aggregated column - a
        /// SQLite-specific extension that returns an arbitrary matching row's Id per group. The only consumer of
        /// this method's result (<see cref="GetAllEmojisPaged"/>, mirroring GetAllEmojisPagedByCategories.sql's
        /// <c>EC.Id IN (SELECT Value FROM TempEmojiCategoryIds)</c>) only ever uses the returned values to identify
        /// *which EmojiId groups matched* - it never depends on *which* EmojiCategory row's Id was picked for a
        /// given EmojiId - so <c>MIN(Id)</c> per group is a fully equivalent, deterministic (and SQL-translatable)
        /// stand-in for SQLite's implementation-defined choice.
        /// </remarks>
        public async Task<List<int>> SearchEmojiCategoryIds(List<string> categories)
        {
            if (categories == null || categories.Count == 0)
            {
                return new List<int>();
            }

            using var context = _createContext();

            var searchTokenCount = categories.Count;

            IQueryable<EmojiEntities.EmojiCategory>? matches = null;
            foreach (var term in categories)
            {
                var termMatches = context.EmojiCategories.Where(ec => ec.Category.StartsWith(term));
                matches = matches == null ? termMatches : matches.Concat(termMatches);
            }

            return await matches!
                .GroupBy(ec => ec.EmojiId)
                .Where(g => g.Count() >= searchTokenCount)
                .Select(g => g.Min(ec => ec.Id))
                .ToListAsync();
        }

        /// <summary>
        /// Mirrors GetEmojiCategoriesByName.sql - every EmojiCategory row belonging to the Emoji named
        /// <paramref name="name"/>, populating only <see cref="TwEmojiCategory.EmojiId"/>/<see cref="TwEmojiCategory.Category"/>
        /// (the script selects an <c>Id</c> column too, but <see cref="TwEmojiCategory"/> has no matching property,
        /// so - like the Dapper reference silently ignoring unmapped columns - it is simply not read here;
        /// <see cref="TwEmojiCategory.EmojiCount"/> is left at its default, matching the script not selecting it
        /// either). No explicit ordering, same as the script.
        /// </summary>
        public async Task<List<TwEmojiCategory>> GetEmojiCategoriesByName(string name)
        {
            using var context = _createContext();

            return await (
                from e in context.Emojis
                join ec in context.EmojiCategories on e.Id equals ec.EmojiId
                where e.Name == name
                select new TwEmojiCategory
                {
                    EmojiId = ec.EmojiId,
                    Category = ec.Category,
                }).ToListAsync();
        }

        /// <summary>
        /// Mirrors DeleteEmojiById.sql ("DELETE FROM EmojiCategory WHERE EmojiId = @Id; DELETE FROM Emoji WHERE Id
        /// = @Id;") via two LINQ bulk <c>ExecuteDeleteAsync</c> calls wrapped in an explicit transaction, so the
        /// two deletes commit or roll back together the same way the SQLite reference's single multi-statement
        /// batch does.
        /// </summary>
        public async Task DeleteById(int id)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            await context.EmojiCategories.Where(ec => ec.EmojiId == id).ExecuteDeleteAsync();
            await context.Emojis.Where(e => e.Id == id).ExecuteDeleteAsync();

            await transaction.CommitAsync();
        }

        /// <summary>
        /// Mirrors GetEmojiByName.sql - the single Emoji row named <paramref name="name"/> (Name is unique, see
        /// <c>EmojiConfiguration</c>, so <see cref="Queryable.SingleOrDefaultAsync{TSource}(IQueryable{TSource},System.Threading.CancellationToken)"/>
        /// is equivalent to the script's <c>QuerySingleOrDefaultAsync</c>), including <see cref="TwEmoji.ImageData"/>
        /// exactly as stored (GZip-compressed - see this class's doc comment) - the one read path in this class
        /// that returns image bytes at all.
        /// </summary>
        public async Task<TwEmoji?> GetEmojiByName(string name)
        {
            using var context = _createContext();

            return await context.Emojis
                .Where(e => e.Name == name)
                .Select(e => new TwEmoji
                {
                    Id = e.Id,
                    Name = e.Name,
                    MimeType = e.MimeType ?? string.Empty,
                    Shortcut = "%%" + e.Name.ToLower() + "%%",
                    ImageData = e.ImageData,
                })
                .SingleOrDefaultAsync();
        }

        /// <summary>
        /// Mirrors InsertEmoji.sql/UpdateEmoji.sql/UpsertEmojiCategories.sql together, wrapped in a single
        /// transaction (the SQLite reference's own <c>o.BeginTransaction()</c>/<c>Commit()</c>/<c>Rollback()</c>):
        /// inserts a new Emoji row (no <paramref name="emoji"/>.Id, or Id 0) or updates an existing one by Id, then
        /// reconciles its EmojiCategory rows against <paramref name="emoji"/>.Categories, then returns the Emoji's
        /// Id. New/replacement image bytes are GZip-compressed via <see cref="Utility.Compress"/> before being
        /// written (see this class's doc comment); on update, a <see langword="null"/> <paramref name="emoji"/>.ImageData
        /// leaves the existing stored bytes untouched (UpdateEmoji.sql's <c>ImageData = Coalesce(@ImageData,
        /// ImageData)</c>).
        /// </summary>
        /// <remarks>
        /// UpsertEmojiCategories.sql's own DELETE statement (removing categories that are no longer desired) has a
        /// pre-existing bug in the SQLite reference: it filters on the literal <c>EC.EmojiId = 1</c> instead of
        /// the intended <c>@EmojiId</c> parameter (confirmed unique to this script - no other script in
        /// <c>TightWiki.Repository/Scripts/</c> uses a bare numeric literal this way), so on SQLite stale
        /// categories are only ever pruned for whichever Emoji happens to have Id 1, never for the Emoji actually
        /// being edited. Unlike this class's other documented "deliberately preserved" quirks, this one is
        /// <b>not</b> reproduced here - replicating it would mean this method could never prune stale categories
        /// for any Emoji other than Id 1, and could actively corrupt Id 1's categories from an unrelated edit. This
        /// method instead implements the evidently-intended behavior: delete only the querying Emoji's own
        /// EmojiCategory rows whose Category is not in <paramref name="emoji"/>.Categories, matching what the same
        /// script's INSERT half already correctly scopes to <c>@EmojiId</c>. Category matching (both the removal
        /// and the "already exists" check before insertion) is case-insensitive, matching the NOCASE-ish collation
        /// EmojiCategory.Category carries in the real schema (<c>EmojiCategoryConfiguration</c>).
        /// </remarks>
        public async Task<int> UpsertEmoji(TwUpsertEmoji emoji)
        {
            using var context = _createContext();
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                int emojiId;

                if (emoji.Id == null || emoji.Id == 0)
                {
                    var newEmoji = new EmojiEntities.Emoji
                    {
                        Name = emoji.Name,
                        ImageData = emoji.ImageData == null ? null : Utility.Compress(emoji.ImageData),
                        MimeType = emoji.MimeType,
                    };
                    context.Emojis.Add(newEmoji);
                    await context.SaveChangesAsync();
                    emojiId = newEmoji.Id;
                }
                else
                {
                    emojiId = emoji.Id.Value;
                    var compressedImageData = emoji.ImageData == null ? null : Utility.Compress(emoji.ImageData);

                    await context.Emojis
                        .Where(e => e.Id == emojiId)
                        .ExecuteUpdateAsync(setters => setters
                            .SetProperty(e => e.Name, emoji.Name)
                            .SetProperty(e => e.MimeType, emoji.MimeType)
                            .SetProperty(e => e.ImageData, e => compressedImageData ?? e.ImageData));
                }

                var desiredCategories = emoji.Categories ?? new List<string>();

                var existingCategories = await context.EmojiCategories
                    .Where(ec => ec.EmojiId == emojiId)
                    .ToListAsync();

                var toRemove = existingCategories
                    .Where(ec => !desiredCategories.Contains(ec.Category, StringComparer.OrdinalIgnoreCase))
                    .ToList();
                if (toRemove.Count > 0)
                {
                    context.EmojiCategories.RemoveRange(toRemove);
                }

                var existingCategoryNames = existingCategories
                    .Select(ec => ec.Category)
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var category in desiredCategories)
                {
                    if (existingCategoryNames.Add(category))
                    {
                        context.EmojiCategories.Add(new EmojiEntities.EmojiCategory
                        {
                            EmojiId = emojiId,
                            Category = category,
                        });
                    }
                }

                await context.SaveChangesAsync();

                await transaction.CommitAsync();

                return emojiId;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        /// <summary>
        /// Mirrors GetAllEmojisPaged.sql (no <paramref name="categories"/>) / GetAllEmojisPagedByCategories.sql
        /// (<paramref name="categories"/> given) - a page of Emoji rows (Id/Name/MimeType/Shortcut, no
        /// <c>ImageData</c>), with <see cref="TwEmoji.PaginationPageCount"/> computed via the scripts' own
        /// ceiling-division formula (<c>(Count(0) + (@PageSize - 1)) / @PageSize</c>) against the total matching
        /// row count. When <paramref name="categories"/> is given, the matching Emoji set is exactly the one
        /// <see cref="SearchEmojiCategoryIds"/> identifies - mirroring the script's
        /// <c>EC.Id IN (SELECT Value FROM TempEmojiCategoryIds)</c> join, which (since <see cref="SearchEmojiCategoryIds"/>
        /// returns one representative EmojiCategory.Id per matching Emoji) naturally yields one row per matching
        /// Emoji with no separate <c>DISTINCT</c> needed. Ordering mirrors <c>RepositoryHelpers.TransposeOrderby</c>
        /// against both scripts' identical <c>--CONFIG::</c> mapping ("Name=E.[Name]", "MimeType=E.[MimeType]",
        /// "Shortcut=E.[Name]"): no <paramref name="orderBy"/> falls back to the scripts' own un-transposed
        /// "ORDER BY E.[Name]"; an unrecognized <paramref name="orderBy"/> throws the same "No order by mapping..."
        /// message the helper throws; direction is ascending only when <paramref name="orderByDirection"/> is
        /// exactly "asc" (case-insensitively), descending for anything else including null.
        /// </summary>
        public async Task<List<TwEmoji>> GetAllEmojisPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? categories = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(TwConfigGroup.Customization, "Pagination Size");

            using var context = _createContext();

            if (categories == null || categories.Count == 0)
            {
                var totalCount = await context.Emojis.CountAsync();
                var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

                var ordered = ApplyOrder(context.Emojis, orderBy, orderByDirection, "GetAllEmojisPaged.sql");

                return await ordered
                    .Skip((pageNumber - 1) * paginationSize)
                    .Take(paginationSize)
                    .Select(e => new TwEmoji
                    {
                        Id = e.Id,
                        Name = e.Name,
                        MimeType = e.MimeType ?? string.Empty,
                        Shortcut = "%%" + e.Name.ToLower() + "%%",
                        PaginationPageCount = paginationPageCount,
                    }).ToListAsync();
            }
            else
            {
                var emojiCategoryIds = await SearchEmojiCategoryIds(categories);

                var filtered =
                    from e in context.Emojis
                    join ec in context.EmojiCategories on e.Id equals ec.EmojiId
                    where emojiCategoryIds.Contains(ec.Id)
                    select e;

                var totalCount = await filtered.CountAsync();
                var paginationPageCount = (totalCount + (paginationSize - 1)) / paginationSize;

                var ordered = ApplyOrder(filtered, orderBy, orderByDirection, "GetAllEmojisPagedByCategories.sql");

                return await ordered
                    .Skip((pageNumber - 1) * paginationSize)
                    .Take(paginationSize)
                    .Select(e => new TwEmoji
                    {
                        Id = e.Id,
                        Name = e.Name,
                        MimeType = e.MimeType ?? string.Empty,
                        Shortcut = "%%" + e.Name.ToLower() + "%%",
                        PaginationPageCount = paginationPageCount,
                    }).ToListAsync();
            }
        }

        /// <summary>
        /// Shared ordering logic for both branches of <see cref="GetAllEmojisPaged"/> - see that method's doc
        /// comment for the field-mapping/direction rules this implements.
        /// </summary>
        private static IOrderedQueryable<EmojiEntities.Emoji> ApplyOrder(
            IQueryable<EmojiEntities.Emoji> query, string? orderBy, string? orderByDirection, string scriptName)
        {
            if (string.IsNullOrEmpty(orderBy))
            {
                return query.OrderBy(e => e.Name);
            }

            bool ascending = string.Equals(orderByDirection, "asc", StringComparison.InvariantCultureIgnoreCase);

            return orderBy.ToUpperInvariant() switch
            {
                "NAME" => ascending ? query.OrderBy(e => e.Name) : query.OrderByDescending(e => e.Name),
                "MIMETYPE" => ascending ? query.OrderBy(e => e.MimeType) : query.OrderByDescending(e => e.MimeType),
                "SHORTCUT" => ascending ? query.OrderBy(e => e.Name) : query.OrderByDescending(e => e.Name),
                _ => throw new InvalidOperationException(
                    $"No order by mapping was found in '{scriptName}' for the field '{orderBy}'."),
            };
        }

        /// <summary>
        /// Mirrors <c>EmojiRepository.ReloadEmojis</c> exactly (no SQL script of its own - pure C# glue): clears
        /// the <see cref="MemCache.Category.Emoji"/> cache category, re-reads every Emoji via <see cref="GetAllEmojis"/>,
        /// and - when <paramref name="preloadAnimatedEmojis"/> is set - kicks off a background thread that
        /// pre-renders and caches a scaled copy of every animated (<c>image/gif</c>) emoji at
        /// <paramref name="defaultEmojiHeight"/>, reading each one's (still GZip-compressed) image bytes via
        /// <see cref="GetEmojiByName"/> and decompressing with <see cref="Utility.Decompress"/> before handing them
        /// to ImageMagick - same fire-and-forget threading model as the SQLite reference.
        /// </summary>
        public async Task<List<TwEmoji>> ReloadEmojis(bool preloadAnimatedEmojis, int defaultEmojiHeight)
        {
            MemCache.ClearCategory(MemCache.Category.Emoji);
            var emojis = await GetAllEmojis();

            if (preloadAnimatedEmojis)
            {
                new Thread(async () =>
                {
                    var parallelOptions = new ParallelOptions
                    {
                        MaxDegreeOfParallelism = Environment.ProcessorCount / 2 < 2 ? 2 : Environment.ProcessorCount / 2
                    };

                    await Parallel.ForEachAsync(emojis, parallelOptions, async (emoji, cancellationToken) =>
                    {
                        if (emoji.MimeType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                        {
                            var imageCacheKey = MemCacheKey.Build(MemCache.Category.Emoji, [emoji.Shortcut]);
                            emoji.ImageData = (await GetEmojiByName(emoji.Name))?.ImageData;

                            if (emoji.ImageData != null)
                            {
                                var scaledImageCacheKey = MemCacheKey.Build(MemCache.Category.Emoji, [emoji.Shortcut, "100"]);
                                var decompressedImageBytes = Utility.Decompress(emoji.ImageData);
                                var img = new MagickImage(decompressedImageBytes);

                                int customScalePercent = 100;

                                var (Width, Height) = ImagesUtility.ScaleToMaxOf((int)img.Width, (int)img.Height, defaultEmojiHeight);

                                //Adjust to any specified scaling.
                                Height = (int)(Height * (customScalePercent / 100.0));
                                Width = (int)(Width * (customScalePercent / 100.0));

                                //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                                //  dimension to become very small (or even negative). So here we will check the height and width
                                //  to ensure they are both at least n pixels and adjust both dimensions.
                                if (Height < 16)
                                {
                                    Height += 16 - Height;
                                    Width += 16 - Height;
                                }
                                if (Width < 16)
                                {
                                    Height += 16 - Width;
                                    Width += 16 - Width;
                                }

                                //These are hard to generate, so just keep it forever.
                                var resized = ImagesUtility.ResizeGifImage(decompressedImageBytes, Width, Height);
                                var itemCache = new TwImageCacheItem(resized, "image/gif");
                                MemCache.Set(scaledImageCacheKey, itemCache, new CacheItemPolicy());
                            }
                        }
                    });
                }).Start();
            }

            return emojis;
        }
    }
}
