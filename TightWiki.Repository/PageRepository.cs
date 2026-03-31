using DuoVia.FuzzyStrings;
using Microsoft.Extensions.Configuration;
using NTDLS.Helpers;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Library.Caching;
using TightWiki.Library.Extensions;
using TightWiki.Library.Security;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using TightWiki.Repository.Helpers;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public class PageRepository
        : ITwPageRepository
    {
        readonly private ITwConfigurationRepository _configurationRepository;
        readonly private ITwStatisticsRepository _statisticsRepository;

        public SqliteManagedFactory PagesFactory { get; private set; }
        public SqliteManagedFactory DeletedPagesFactory { get; private set; }
        public SqliteManagedFactory DeletedPageRevisionsFactory { get; private set; }

        public PageRepository(IConfiguration configuration,
            ITwConfigurationRepository configurationRepository,
            ITwStatisticsRepository statisticsRepository)
        {
            _configurationRepository = configurationRepository;
            _statisticsRepository = statisticsRepository;

            var configDatabaseFile = configurationRepository.ConfigFactory.Ephemeral(o => o.NativeConnection.DataSource);

            PagesFactory = new(configuration.GetDatabaseConnectionString("PagesConnection", "pages.db", configDatabaseFile));
            DeletedPagesFactory = new(configuration.GetDatabaseConnectionString("DeletedPagesConnection", "deletedpages.db", configDatabaseFile));
            DeletedPageRevisionsFactory = new(configuration.GetDatabaseConnectionString("DeletedPageRevisionsConnection", "deletedpagerevisions.db", configDatabaseFile));
        }

        public async Task<List<TwPage>> AutoCompletePage(string? searchText)
            => await PagesFactory.QueryAsync<TwPage>("AutoCompletePage.sql", new { SearchText = searchText ?? string.Empty });

        public async Task<List<string>> AutoCompleteNamespace(string? searchText)
            => await PagesFactory.QueryAsync<string>("AutoCompleteNamespace.sql", new { SearchText = searchText ?? string.Empty });

        public async Task<TwPage?> GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return await PagesFactory.QuerySingleOrDefaultAsync<TwPage>("GetPageRevisionInfoById.sql", param);
        }

        public async Task<TwProcessingInstructionCollection> GetPageProcessingInstructionsByPageId(int pageId)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId]);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    PageId = pageId
                };

                return new TwProcessingInstructionCollection()
                {
                    Collection = (await PagesFactory.QueryAsync<TwProcessingInstruction>("GetPageProcessingInstructionsByPageId.sql", param)).ToList()
                };
            })).EnsureNotNull();
        }

        public async Task<List<TwPageTag>> GetPageTagsById(int pageId)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId]);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    PageId = pageId
                };

                return await PagesFactory.QueryAsync<TwPageTag>("GetPageTagsById.sql", param);
            })).EnsureNotNull();
        }

        public async Task<List<TwPageRevision>> GetPageRevisionsInfoByNavigationPaged(
            string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                Navigation = navigation,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var query = RepositoryHelpers.TransposeOrderby("GetPageRevisionsInfoByNavigationPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwPageRevision>(query, param);
            });
        }

        public async Task<List<TwPageRevision>> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
        {
            var param = new
            {
                UserId = userId,
                TopCount = topCount
            };

            return await PagesFactory.QueryAsync<TwPageRevision>("GetTopRecentlyModifiedPagesInfoByUserId.sql", param);
        }

        public async Task<string?> GetPageNavigationByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await PagesFactory.ExecuteScalarAsync<string>("GetPageNavigationByPageId.sql", param);
        }

        public async Task<List<TwPage>> GetTopRecentlyModifiedPagesInfo(int topCount)
        {
            var param = new
            {
                TopCount = topCount
            };

            return await PagesFactory.QueryAsync<TwPage>("GetTopRecentlyModifiedPagesInfo.sql", param);
        }

        private async Task<List<TwPageSearchToken>> GetFuzzyPageSearchTokens(List<TwPageToken> tokens, double minimumMatchScore)
        {
            return await PagesFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    MinimumMatchScore = minimumMatchScore,
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", tokens.Distinct());
                return await o.QueryAsync<TwPageSearchToken>("GetFuzzyPageSearchTokens.sql", param);
            });
        }

        private async Task<List<TwPageSearchToken>> GetExactPageSearchTokens(List<TwPageToken> tokens, double minimumMatchScore)
        {
            return await PagesFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    MinimumMatchScore = minimumMatchScore,
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", tokens.Distinct());
                return await o.QueryAsync<TwPageSearchToken>("GetExactPageSearchTokens.sql", param);
            });
        }

        private async Task<List<TwPageSearchToken>> GetMeteredPageSearchTokens(List<string> searchTerms, bool allowFuzzyMatching)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Search, [string.Join(',', searchTerms), allowFuzzyMatching]);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var minimumMatchScore = await _configurationRepository.Get<float>("Search", "Minimum Match Score");

                var searchTokens = searchTerms.Select(o =>
                                    new TwPageToken
                                    {
                                        Token = o,
                                        DoubleMetaphone = o.ToDoubleMetaphone()
                                    }).ToList();

                if (allowFuzzyMatching == true)
                {
                    var allTokens = await GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                    var fuzzyTokens = await GetFuzzyPageSearchTokens(searchTokens, minimumMatchScore / 2.0);

                    allTokens.AddRange(fuzzyTokens);

                    return allTokens
                            .GroupBy(token => token.PageId)
                            .Where(group => group.Sum(g => g.Score) >= minimumMatchScore) // Filtering groups
                            .Select(group => new TwPageSearchToken
                            {
                                PageId = group.Key,
                                Match = group.Max(g => g.Match),
                                Weight = group.Max(g => g.Weight),
                                Score = group.Max(g => g.Score)
                            }).ToList();
                }
                else
                {
                    return await GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                }
            })).EnsureNotNull();
        }

        public async Task<List<TwPage>> PageSearch(List<string> searchTerms)
        {
            if (searchTerms.Count == 0)
            {
                return new List<TwPage>();
            }

            bool allowFuzzyMatching = await _configurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");
            var meteredSearchTokens = await GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching == true);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<TwPage>();
            }

            return await PagesFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    MaximumScore = meteredSearchTokens.Max(t => t.Score)
                };

                using var users_db = o.Attach("users.db", "users_db");
                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", meteredSearchTokens);
                return await o.QueryAsync<TwPage>("PageSearch.sql", param);
            });
        }

        public async Task<List<TwPage>> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
        {
            if (searchTerms.Count == 0)
            {
                return new List<TwPage>();
            }

            pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");
            allowFuzzyMatching ??= await _configurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");

            var meteredSearchTokens = await GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching == true);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<TwPage>();
            }

            return await PagesFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    MaximumScore = meteredSearchTokens.Max(t => t.Score)
                };

                using var users_db = o.Attach("users.db", "users_db");
                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", meteredSearchTokens);
                var results = await o.QueryAsync<TwPage>("PageSearchPaged.sql", param);
                return results;
            });
        }

        public async Task<List<TwRelatedPage>> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageId = pageId,
                Similarity = similarity,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await PagesFactory.QueryAsync<TwRelatedPage>("GetSimilarPagesPaged.sql", param);
        }

        public async Task<List<TwRelatedPage>> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageId = pageId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await PagesFactory.QueryAsync<TwRelatedPage>("GetRelatedPagesPaged.sql", param);
        }

        public async Task FlushPageCache(int pageId)
        {
            var pageNavigation = await GetPageNavigationByPageId(pageId);
            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [pageNavigation]));
            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [pageId]));
        }

        public async Task InsertPageComment(int pageId, Guid userId, string body)
        {
            var param = new
            {
                PageId = pageId,
                UserId = userId,
                Body = body,
                CreatedDate = DateTime.UtcNow
            };

            await PagesFactory.ExecuteAsync("InsertPageComment.sql", param);

            await FlushPageCache(pageId);
        }

        public async Task DeletePageCommentById(int pageId, int commentId)
        {
            var param = new
            {
                PageId = pageId,
                CommentId = commentId
            };

            await PagesFactory.ExecuteAsync("DeletePageCommentById.sql", param);

            await FlushPageCache(pageId);
        }

        public async Task DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId)
        {
            var param = new
            {
                PageId = pageId,
                UserId = userId,
                CommentId = commentId
            };

            await PagesFactory.ExecuteAsync("DeletePageCommentByUserAndId.sql", param);

            await FlushPageCache(pageId);
        }

        public async Task<int> GetTotalPageCommentCount(int pageId)
            => await PagesFactory.ExecuteScalarAsync<int>("GetTotalPageCommentCount.sql", new { PageId = pageId });

        public async Task<List<TwPageComment>> GetPageCommentsPaged(string navigation, int pageNumber)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [navigation, pageNumber, paginationSize]);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    Navigation = navigation,
                    PageNumber = pageNumber,
                    PageSize = paginationSize
                };

                return await PagesFactory.EphemeralAsync(async o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    return await o.QueryAsync<TwPageComment>("GetPageCommentsPaged.sql", param);
                });
            })).EnsureNotNull();
        }

        public async Task<List<TwNonexistentPage>> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetMissingPagesPaged.sql", orderBy, orderByDirection);
            return await PagesFactory.QueryAsync<TwNonexistentPage>(query, param);
        }

        public async Task UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            var param = new
            {
                @PageId = pageId,
                @PageNavigation = pageNavigation
            };

            await PagesFactory.ExecuteAsync("UpdateSinglePageReference.sql", param);

            await FlushPageCache(pageId);
        }

        public async Task UpdatePageReferences(int pageId, List<TwPageReference> referencesPageNavigations)
        {
            await PagesFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    PageId = pageId
                };

                using var tempTable = o.CreateTempTableFrom("TempReferences", referencesPageNavigations.Distinct());
                return await o.QueryAsync<TwPage>("UpdatePageReferences.sql", param);
            });

            await FlushPageCache(pageId);
        }

        public async Task<List<TwPage>> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize,
                Instruction = instruction
            };

            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QueryAsync<TwPage>("GetAllPagesByInstructionPaged.sql", param);
            });
        }

        public async Task<List<int>> GetDeletedPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            return await DeletedPagesFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempTokens", tokens);
                return await o.QueryAsync<int>("GetDeletedPageIdsByTokens.sql", param);
            });
        }

        public async Task<List<int>> GetPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            return await PagesFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempTokens", tokens);
                return await o.QueryAsync<int>("GetPageIdsByTokens.sql", param);
            });
        }

        public async Task<List<TwPage>> GetAllNamespacePagesPaged(int pageNumber, string namespaceName,
            string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize,
                Namespace = namespaceName
            };

            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                var query = RepositoryHelpers.TransposeOrderby("GetAllNamespacePagesPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwPage>(query, param);
            });
        }

        /// <summary>
        /// Unlike the search, this method returns all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        public async Task<List<TwPage>> GetAllPagesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            if (searchTerms?.Count > 0)
            {
                var pageIds = await GetPageIdsByTokens(searchTerms);

                return await PagesFactory.EphemeralAsync(async o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    using var deletedpagerevisions_db = o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db");
                    using var tempTable = o.CreateTempTableFrom("TempPageIds", pageIds);

                    var query = RepositoryHelpers.TransposeOrderby("GetAllPagesByPageIdPaged.sql", orderBy, orderByDirection);
                    return await o.QueryAsync<TwPage>(query, param);
                });
            }

            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var deletedpagerevisions_db = o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db");

                var query = RepositoryHelpers.TransposeOrderby("GetAllPagesPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwPage>(query, param);
            });
        }

        /// <summary>
        /// Unlike the search, this method returns all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        public async Task<List<TwPage>> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null,
            string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            if (searchTerms?.Count > 0)
            {
                var pageIds = await GetDeletedPageIdsByTokens(searchTerms);
                return await DeletedPagesFactory.EphemeralAsync(async o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    using var tempTable = o.CreateTempTableFrom("TempPageIds", pageIds);

                    var query = RepositoryHelpers.TransposeOrderby("GetAllDeletedPagesByPageIdPaged.sql", orderBy, orderByDirection);
                    return await o.QueryAsync<TwPage>(query, param);
                });
            }

            return await DeletedPagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                var query = RepositoryHelpers.TransposeOrderby("GetAllDeletedPagesPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwPage>(query, param);
            });
        }

        public async Task<List<TwNamespaceStat>> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetAllNamespacesPaged.sql", orderBy, orderByDirection);

            return await PagesFactory.QueryAsync<TwNamespaceStat>(query, param);
        }

        public async Task<List<string>> GetAllNamespaces()
            => await PagesFactory.QueryAsync<string>("GetAllNamespaces.sql");

        public async Task<List<TwPage>> GetAllPages()
            => await PagesFactory.QueryAsync<TwPage>("GetAllPages.sql");

        public async Task<List<TwPage>> GetAllTemplatePages()
            => await PagesFactory.QueryAsync<TwPage>("GetAllTemplatePages.sql");

        public async Task<List<TwFeatureTemplate>> GetAllFeatureTemplates()
        {
            return (await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Configuration), async () =>
            {
                return (await PagesFactory.QueryAsync<TwFeatureTemplate>("GetAllFeatureTemplates.sql")).ToList();
            })).EnsureNotNull();
        }

        public async Task UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            await PagesFactory.EphemeralAsync(async o =>
            {
                var param = new
                {
                    PageId = pageId
                };

                instructions = instructions.Select(o => o.ToLowerInvariant()).Distinct().ToList();

                using var tempTable = o.CreateTempTableFrom("TempInstructions", instructions);
                return await o.QueryAsync<TwPage>("UpdatePageProcessingInstructions.sql", param);
            });

            await FlushPageCache(pageId);
        }

        public async Task<TwPage?> GetPageRevisionById(int pageId, int? revision = null)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId, revision]), async () =>
            {
                var param = new
                {
                    PageId = pageId,
                    Revision = revision
                };

                return await PagesFactory.QuerySingleOrDefaultAsync<TwPage>("GetPageRevisionById.sql", param);
            });
        }

        public async Task SavePageSearchTokens(List<TwPageToken> items)
        {
            await PagesFactory.EphemeralAsync(async o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTokens", items.Distinct());
                return await o.QueryAsync<TwPage>("SavePageSearchTokens.sql");
            });
        }

        public async Task TruncateAllPageRevisions(string confirm)
        {
            if (confirm != "YES") //Are you REALLY sure?
            {
                return;
            }
            else
            {
                await PagesFactory.EphemeralAsync(async o =>
                {
                    var transaction = o.BeginTransaction();
                    try
                    {
                        await o.ExecuteAsync("TruncateAllPageRevisions.sql");
                        transaction.Commit();
                    }
                    catch
                    {
                        transaction.Rollback();
                        throw;
                    }
                });
            }
        }

        public async Task<int> GetCurrentPageRevision(int pageId)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId]), async () =>
            {
                var param = new
                {
                    PageId = pageId,
                };

                return await PagesFactory.ExecuteScalarAsync<int>("GetCurrentPageRevision.sql", param);
            });
        }

        public async Task<int> GetCurrentPageRevision(SqliteManagedInstance connection, int pageId)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId]), async () =>
            {
                var param = new
                {
                    PageId = pageId,
                };

                return await connection.ExecuteScalarAsync<int>("GetCurrentPageRevision.sql", param);
            });
        }

        public async Task<TwPage?> GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId, revision]);

            return await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    PageId = pageId,
                    Revision = revision
                };

                return await PagesFactory.QuerySingleOrDefaultAsync<TwPage>("GetLimitedPageInfoByIdAndRevision.sql", param);
            });
        }

        /// <summary>
        /// Inserts a new page if Page.Id == 0, other wise updates the page. All metadata is written to the database.
        /// </summary>
        public async Task<int> UpsertPage(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null)
        {
            bool isNewlyCreated = page.Id == 0;

            page.Id = await SavePage(page);

            await RefreshPageMetadata(wikifier, localizer, page, sessionState);

            if (isNewlyCreated)
            {
                //This will update the PageId of references that have been saved to the navigation link.
                await UpdateSinglePageReference(page.Navigation, page.Id);
            }

            return page.Id;
        }

        /// <summary>
        /// Rebuilds the page and writes all aspects to the database.
        /// </summary>
        /// <param name="sessionState"></param>
        /// <param name="query"></param>
        /// <param name="page"></param>
        public async Task RefreshPageMetadata(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null)
        {
            //We omit function calls from the tokenization process because they are too dynamic for static searching.
            var state = await wikifier.Transform(localizer, sessionState, page, null, [WikiMatchType.StandardFunction]);

            await UpdatePageTags(page.Id, state.Tags);
            await UpdatePageProcessingInstructions(page.Id, state.ProcessingInstructions);

            var pageTokens = (await ParsePageTokens(state)).Select(o =>
                      new TwPageToken
                      {
                          PageId = page.Id,
                          Token = o.Token,
                          DoubleMetaphone = o.DoubleMetaphone,
                          Weight = o.Weight
                      }).ToList();

            await SavePageSearchTokens(pageTokens);
            await UpdatePageReferences(page.Id, state.OutgoingLinks);

            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [page.Id]));
            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [page.Navigation]));
        }

        public async Task<List<TwAggregatedSearchToken>> ParsePageTokens(ITwEngineState state)
        {
            var parsedTokens = new List<WeightedSearchToken>();

            parsedTokens.AddRange(await ComputeParsedPageTokens(state.HtmlResult, 1));
            parsedTokens.AddRange(await ComputeParsedPageTokens(state.Page.Description, 1.2));
            parsedTokens.AddRange(await ComputeParsedPageTokens(string.Join(" ", state.Tags), 1.4));
            parsedTokens.AddRange(await ComputeParsedPageTokens(state.Page.Name, 1.6));

            var aggregatedTokens = parsedTokens.GroupBy(o => o.Token).Select(o => new TwAggregatedSearchToken
            {
                Token = o.Key,
                DoubleMetaphone = o.Key.ToDoubleMetaphone(),
                Weight = o.Sum(g => g.Weight)
            }).ToList();

            return aggregatedTokens;
        }

        internal async Task<List<WeightedSearchToken>> ComputeParsedPageTokens(string content, double weightMultiplier)
        {
            var searchConfig = await _configurationRepository.GetConfigurationEntryValuesByGroupName(WikiConfigurationGroup.Membership);

            var exclusionWords = searchConfig?.Value<string>("Word Exclusions")?
                .Split([',', ';'], StringSplitOptions.RemoveEmptyEntries).Distinct() ?? new List<string>();
            var strippedContent = Html.StripHtml(content);

            var tokens = strippedContent.Split([' ', '\n', '\t', '-', '_']).ToList();

            if (searchConfig?.Value<bool>("Split Camel Case") == true)
            {
                var allSplitTokens = new List<string>();

                foreach (var token in tokens)
                {
                    var splitTokens = Text.SplitCamelCase(token);
                    if (splitTokens.Count > 1)
                    {
                        splitTokens.ForEach(t => allSplitTokens.Add(t));
                    }
                }

                tokens.AddRange(allSplitTokens);
            }

            tokens = tokens.ConvertAll(d => d.ToLowerInvariant());

            tokens.RemoveAll(o => exclusionWords.Contains(o));

            var searchTokens = (from w in tokens
                                group w by w into g
                                select new WeightedSearchToken
                                {
                                    Token = g.Key,
                                    Weight = g.Count() * weightMultiplier
                                }).ToList();

            return searchTokens.Where(o => string.IsNullOrWhiteSpace(o.Token) == false).ToList();
        }

        /// <summary>
        /// Creates a new page or updates an existing page and its revision history in the data store.
        /// 
        /// DO NOT USE DIRECLTY: Use UpsertPage() instead.
        /// </summary>
        /// <remarks>If the page content or metadata has changed, a new revision is created and all
        /// attachments are associated with the latest revision. The method automatically manages transaction handling
        /// and revision incrementing.</remarks>
        /// <param name="page">The page to create or update. The page's properties determine whether a new page is created or an existing
        /// page is updated. Cannot be null.</param>
        /// <returns>The unique identifier of the created or updated page.</returns>
        private async Task<int> SavePage(TwPage page)
        {
            var pageUpsertParam = new
            {
                PageId = page.Id,
                Name = page.Name,
                Navigation = TwNamespaceNavigation.CleanAndValidate(page.Name),
                Description = page.Description,
                Body = page.Body ?? string.Empty,
                Namespace = page.Namespace,
                CreatedByUserId = page.CreatedByUserId,
                CreatedDate = page.CreatedDate,
                ModifiedByUserId = page.ModifiedByUserId,
                ModifiedDate = DateTime.UtcNow
            };

            var newDataHash = SecurityUtility.Crc32(page.Body ?? string.Empty);

            await PagesFactory.EphemeralAsync(async o =>
            {
                var transaction = o.BeginTransaction();

                try
                {
                    int currentPageRevision = 0;
                    bool hasPageChanged = false;

                    if (page.Id == 0)
                    {
                        //This is a new page, just insert it.
                        page.Id = await o.ExecuteScalarAsync<int>("CreatePage.sql", pageUpsertParam);
                        hasPageChanged = true;
                    }
                    else
                    {
                        //Get current page so we can determine if anything has changed.
                        var currentRevisionInfo = await GetLimitedPageInfoByIdAndRevision(page.Id)
                            ?? throw new Exception("The page could not be found.");

                        currentPageRevision = currentRevisionInfo.Revision;

                        //Update the existing page.
                        await o.ExecuteAsync("UpdatePage.sql", pageUpsertParam);

                        //Determine if anything has actually changed.
                        hasPageChanged = currentRevisionInfo.Name != page.Name
                            || currentRevisionInfo.Namespace != page.Namespace
                            || currentRevisionInfo.Description != page.Description
                            || currentRevisionInfo.ChangeSummary != page.ChangeSummary
                            || currentRevisionInfo.DataHash != newDataHash;
                    }

                    if (hasPageChanged)
                    {
                        currentPageRevision++;

                        var updatePageRevisionNumberParam = new
                        {
                            PageId = page.Id,
                            PageRevision = currentPageRevision
                        };
                        //The page content has actually changed (according to the checksum), so we will bump the page revision.
                        await o.ExecuteAsync("UpdatePageRevisionNumber.sql", updatePageRevisionNumberParam);

                        var insertPageRevisionParam = new
                        {
                            PageId = page.Id,
                            Name = page.Name,
                            Namespace = page.Namespace,
                            Description = page.Description,
                            Body = page.Body,
                            DataHash = newDataHash,
                            PageRevision = currentPageRevision,
                            ChangeSummary = page.ChangeSummary ?? string.Empty,
                            ModifiedByUserId = page.ModifiedByUserId,
                            ModifiedDate = DateTime.UtcNow,
                        };
                        //Insert the new actual page revision entry (this is the data).
                        await o.ExecuteAsync("InsertPageRevision.sql", insertPageRevisionParam);

                        var reassociateAllPageAttachmentsParam = new
                        {
                            PageId = page.Id,
                            PageRevision = currentPageRevision,
                        };
                        //Associate all page attachments with the latest revision.
                        await o.ExecuteAsync("ReassociateAllPageAttachments.sql", reassociateAllPageAttachmentsParam);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            return page.Id;
        }

        /// <summary>
        /// Gets the page info without the content.
        /// </summary>
        public async Task<TwPage?> GetPageInfoByNavigation(string navigation)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [navigation]), async () =>
            {
                var param = new
                {
                    Navigation = navigation
                };

                return await PagesFactory.QuerySingleOrDefaultAsync<TwPage?>("GetPageInfoByNavigation.sql", param);
            });
        }

        public async Task<int> GetPageRevisionCountByPageId(int pageId)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId]), async () =>
            {
                var param = new
                {
                    PageId = pageId
                };

                return await PagesFactory.ExecuteScalarAsync<int>("GetPageRevisionCountByNavigation.sql", param);
            });
        }

        public async Task RestoreDeletedPageByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            await PagesFactory.EphemeralAsync(async o =>
            {
                var transaction = o.BeginTransaction();
                try
                {
                    using var deletedpages_db = o.Attach("deletedpages.db", "deletedpages_db");
                    await o.ExecuteAsync("RestoreDeletedPageByPageId.sql", param);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            await FlushPageCache(pageId);
        }

        public async Task MovePageRevisionToDeletedById(int pageId, int revision, Guid userId)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision,
                DeletedByUserId = userId,
                DeletedDate = DateTime.UtcNow
            };

            await PagesFactory.EphemeralAsync(async o =>
            {
                var transaction = o.BeginTransaction();
                try
                {
                    using var deletedpagerevisions_db = o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db");
                    await o.ExecuteAsync("MovePageRevisionToDeletedById.sql", param);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            await FlushPageCache(pageId);
        }

        public async Task MovePageToDeletedById(int pageId, Guid userId)
        {
            var param = new
            {
                PageId = pageId,
                DeletedByUserId = userId,
                DeletedDate = DateTime.UtcNow
            };

            await PagesFactory.EphemeralAsync(async o =>
            {
                var transaction = o.BeginTransaction();
                try
                {
                    using var deletedpages_db = o.Attach("deletedpages.db", "deletedpages_db");

                    await o.ExecuteAsync("MovePageToDeletedById.sql", param);
                    transaction.Commit();
                    await _statisticsRepository.DeletePageStatisticsByPageId(pageId);
                }

                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            await FlushPageCache(pageId);
        }

        public async Task PurgeDeletedPageByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            await DeletedPagesFactory.ExecuteAsync("PurgeDeletedPageByPageId.sql", param);

            await PurgeDeletedPageRevisionsByPageId(pageId);

            await FlushPageCache(pageId);
        }

        public async Task PurgeDeletedPages()
        {
            await DeletedPagesFactory.ExecuteAsync("PurgeDeletedPages.sql");

            await PurgeDeletedPageRevisions();
        }

        public async Task<int> GetCountOfPageAttachmentsById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await PagesFactory.ExecuteScalarAsync<int>("GetCountOfPageAttachmentsById.sql", param);
        }

        public async Task<TwPage?> GetDeletedPageById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await DeletedPagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QuerySingleOrDefaultAsync<TwPage>("GetDeletedPageById.sql", param);
            });
        }

        public async Task<TwPage?> GetLatestPageRevisionById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QuerySingleOrDefaultAsync<TwPage>("GetLatestPageRevisionById.sql", param);
            });
        }

        public async Task<int> GetPageNextRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return await PagesFactory.ExecuteScalarAsync<int>("GetPageNextRevision.sql", param);
        }

        public async Task<int> GetPagePreviousRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return await PagesFactory.ExecuteScalarAsync<int>("GetPagePreviousRevision.sql", param);
        }

        public async Task<List<TwDeletedPageRevision>> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageId = pageId,
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            return await DeletedPageRevisionsFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var query = RepositoryHelpers.TransposeOrderby("GetDeletedPageRevisionsByIdPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwDeletedPageRevision>(query, param);
            });
        }

        public async Task PurgeDeletedPageRevisions()
        {
            await DeletedPageRevisionsFactory.ExecuteAsync("PurgeDeletedPageRevisions.sql");
        }

        public async Task PurgeDeletedPageRevisionsByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            await DeletedPageRevisionsFactory.ExecuteAsync("PurgeDeletedPageRevisionsByPageId.sql", param);

            await FlushPageCache(pageId);
        }

        public async Task PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            await DeletedPageRevisionsFactory.ExecuteAsync("PurgeDeletedPageRevisionByPageIdAndRevision.sql", param);

            await FlushPageCache(pageId);
        }

        public async Task RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            await DeletedPageRevisionsFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");
                await o.ExecuteAsync("RestoreDeletedPageRevisionByPageIdAndRevision.sql", param);
            });

            await FlushPageCache(pageId);
        }

        public async Task<TwDeletedPageRevision?> GetDeletedPageRevisionById(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return await DeletedPageRevisionsFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QueryFirstOrDefaultAsync<TwDeletedPageRevision>("GetDeletedPageRevisionById.sql", param);
            });
        }

        public async Task<TwPage?> GetPageRevisionByNavigation(TwNamespaceNavigation navigation, int? revision = null)
        {
            var param = new
            {
                Navigation = navigation.Canonical,
                Revision = revision
            };

            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QuerySingleOrDefaultAsync<TwPage>("GetPageRevisionByNavigation.sql", param);
            });
        }

        public async Task<TwPage?> GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false)
        {
            var navigation = new TwNamespaceNavigation(givenNavigation);

            var param = new
            {
                Navigation = navigation.Canonical,
                Revision = revision
            };

            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [navigation.Canonical, revision]);

            if (refreshCache)
            {
                TwCache.Remove(cacheKey);
            }

            return await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                return await PagesFactory.EphemeralAsync(async o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    return await o.QuerySingleOrDefaultAsync<TwPage>("GetPageRevisionByNavigation.sql", param);
                });
            });
        }

        #region Tags.

        public async Task<List<TwTagAssociation>> GetAssociatedTags(string tag)
        {
            var param = new
            {
                @Tag = tag
            };

            return await PagesFactory.QueryAsync<TwTagAssociation>("GetAssociatedTags.sql", param);
        }

        public async Task<List<TwPage>> GetPageInfoByNamespaces(List<string> namespaces)
        {
            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempNamespaces", namespaces);
                return await o.QueryAsync<TwPage>("GetPageInfoByNamespaces.sql");
            });
        }

        public async Task<List<TwPage>> GetPageInfoByTags(IEnumerable<string> tags)
        {
            var cleanedTags = tags.Select(o => TwNavigation.Clean(o));

            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTags", cleanedTags);
                return await o.QueryAsync<TwPage>("GetPageInfoByTags.sql");
            });
        }

        public async Task<List<TwPage>> GetPageInfoByTag(string tag)
        {
            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTags", new List<string> { TwNavigation.Clean(tag) });
                return await o.QueryAsync<TwPage>("GetPageInfoByTags.sql");
            });
        }

        public async Task UpdatePageTags(int pageId, List<string> tags)
        {
            await PagesFactory.EphemeralAsync(async o =>
            {
                var paramTags = tags
                    .Select(o => new
                    {
                        Tag = o,
                        Navigation = TwNavigation.Clean(o)
                    })
                    .DistinctBy(o => o.Navigation);

                using var tempTable = o.CreateTempTableFrom("TempTags", paramTags);
                var param = new
                {
                    PageId = pageId
                };

                return await o.QueryAsync<TwPage>("UpdatePageTags.sql", param);
            });
        }

        #endregion

        #region Page Files.

        public async Task DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            await PagesFactory.ExecuteAsync("DetachPageRevisionAttachment.sql", param);
        }

        public async Task<List<TwOrphanedPageAttachment>> GetOrphanedPageAttachmentsPaged(
            int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetOrphanedPageAttachments.sql", orderBy, orderByDirection);
            return await PagesFactory.QueryAsync<TwOrphanedPageAttachment>(query, param);
        }

        public async Task PurgeOrphanedPageAttachments()
            => await PagesFactory.ExecuteAsync("PurgeOrphanedPageAttachments.sql");

        public async Task PurgeOrphanedPageAttachment(int pageFileId, int revision)
        {
            var param = new
            {
                PageFileId = pageFileId,
                Revision = revision
            };
            await PagesFactory.ExecuteAsync("PurgeOrphanedPageAttachment.sql", param);
        }

        public async Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null)
        {
            pageSize ??= await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                PageNavigation = pageNavigation,
                PageRevision = pageRevision
            };
            return await PagesFactory.QueryAsync<TwPageFileAttachmentInfo>("GetPageFilesInfoByPageNavigationAndPageRevisionPaged.sql", param);
        }

        public async Task<TwPageFileAttachmentInfo?> GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageRevision = pageRevision
            };

            return await PagesFactory.QuerySingleOrDefaultAsync<TwPageFileAttachmentInfo>("GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation.sql", param);
        }

        public async Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null)
        {
            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                FileRevision = fileRevision
            };

            return await PagesFactory.QuerySingleOrDefaultAsync<TwPageFileAttachment>("GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation.sql", param);
        }

        public async Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [pageNavigation, fileNavigation, pageRevision]);

            return await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    PageNavigation = pageNavigation,
                    FileNavigation = fileNavigation,
                    PageRevision = pageRevision
                };

                return await PagesFactory.QuerySingleOrDefaultAsync<TwPageFileAttachment>(
                    "GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation.sql", param);
            });
        }

        public async Task<List<TwPageFileAttachmentInfo>> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber)
        {
            var paginationSize = await _configurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNavigation = pageNavigation,
                FileNavigation = fileNavigation,
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            return await PagesFactory.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var result = await o.QueryAsync<TwPageFileAttachmentInfo>(
                    "GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged.sql", param);

                return result;
            });
        }

        public async Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await PagesFactory.QueryAsync<TwPageFileAttachmentInfo>("GetPageFilesInfoByPageId.sql", param);
        }

        public async Task<TwPageFileRevisionAttachmentInfo?> GetPageFileInfoByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return await connection.QuerySingleOrDefaultAsync<TwPageFileRevisionAttachmentInfo>("GetPageFileInfoByFileNavigation.sql", param);
        }

        public async Task<TwPageFileRevisionAttachmentInfo?> GetPageCurrentRevisionAttachmentByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation)
        {
            var param = new
            {
                PageId = pageId,
                Navigation = fileNavigation,
            };

            return await connection.QuerySingleOrDefaultAsync<TwPageFileRevisionAttachmentInfo>("GetPageCurrentRevisionAttachmentByFileNavigation.sql", param);
        }

        public async Task UpsertPageFile(TwPageFileAttachment item, Guid userId)
        {
            bool hasFileChanged = false;

            await PagesFactory.EphemeralAsync(async o =>
            {
                var transaction = o.BeginTransaction();

                try
                {
                    var pageFileInfo = await GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation);
                    if (pageFileInfo == null)
                    {
                        //If the page file does not exist, then insert it.

                        var InsertPageFileParam = new
                        {
                            PageId = item.PageId,
                            Name = item.Name,
                            FileNavigation = item.FileNavigation,
                            ContentType = item.ContentType,
                            Size = item.Size,
                            CreatedDate = item.CreatedDate,
                            Data = item.Data
                        };

                        o.Execute("InsertPageFile.sql", InsertPageFileParam);

                        //Get the id of the newly inserted page file.
                        pageFileInfo = await GetPageFileInfoByFileNavigation(o, item.PageId, item.FileNavigation)
                                        ?? throw new Exception("Failed find newly inserted page attachment.");

                        hasFileChanged = true;
                    }


                    int currentFileRevision = 0;
                    var newDataHash = SecurityUtility.Crc32(item.Data);

                    var currentlyAttachedFile = await GetPageCurrentRevisionAttachmentByFileNavigation(o, item.PageId, item.FileNavigation);
                    if (currentlyAttachedFile != null)
                    {
                        //The PageFile exists and a revision of it is attached to this page revision.
                        //Keep track of the file revision, and determine if the file has changed (via the file hash).

                        currentFileRevision = currentlyAttachedFile.Revision;
                        hasFileChanged = currentlyAttachedFile.DataHash != newDataHash;
                    }
                    else
                    {
                        //The file either does not exist or is not attached to the current page revision.
                        hasFileChanged = true;

                        //We determined earlier that the PageFile does exist, so keep track of the file revision.
                        currentFileRevision = pageFileInfo.Revision;
                    }

                    if (hasFileChanged)
                    {
                        currentFileRevision++;

                        //Get the current page revision so that we can associate the page file attachment with the current page revision.
                        int currentPageRevision = await GetCurrentPageRevision(o, item.PageId);

                        var updatePageFileRevisionParam = new
                        {
                            PageFileId = pageFileInfo.PageFileId,
                            FileRevision = currentFileRevision
                        };

                        //The file has changed (or is newly inserted), bump the file revision.
                        await o.ExecuteAsync("UpdatePageFileRevision.sql", updatePageFileRevisionParam);

                        var insertPageFileRevisionParam = new
                        {
                            PageFileId = pageFileInfo.PageFileId,
                            ContentType = item.ContentType,
                            Size = item.Size,
                            CreatedDate = item.CreatedDate,
                            CreatedByUserId = userId,
                            Data = item.Data,
                            FileRevision = currentFileRevision,
                            DataHash = newDataHash,
                        };

                        //Insert the actual file data.
                        await o.ExecuteAsync("InsertPageFileRevision.sql", insertPageFileRevisionParam);

                        var associatePageFileAttachmentWithPageRevisionParam = new
                        {
                            PageId = item.PageId,
                            PageFileId = pageFileInfo.PageFileId,
                            PageRevision = currentPageRevision,
                            FileRevision = currentFileRevision,
                            PreviousFileRevision = currentlyAttachedFile?.Revision //This is so we can disassociate the previous file revision.
                        };

                        //Associate the latest version of the file with the latest version of the page.
                        await o.ExecuteAsync("AssociatePageFileAttachmentWithPageRevision.sql", associatePageFileAttachmentWithPageRevisionParam);
                    }

                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });
        }

        #endregion
    }
}
