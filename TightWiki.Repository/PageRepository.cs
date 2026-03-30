using DuoVia.FuzzyStrings;
using NTDLS.Helpers;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Caching;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;
using static TightWiki.Plugin.TwConstants;

namespace TightWiki.Repository
{
    public static class PageRepository
    {
        public static async Task<List<TwPage>> AutoCompletePage(string? searchText)
            => await ManagedDataStorage.Pages.QueryAsync<TwPage>("AutoCompletePage.sql", new { SearchText = searchText ?? string.Empty });

        public static async Task<List<string>> AutoCompleteNamespace(string? searchText)
            => await ManagedDataStorage.Pages.QueryAsync<string>("AutoCompleteNamespace.sql", new { SearchText = searchText ?? string.Empty });

        public static async Task<TwPage?> GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return await ManagedDataStorage.Pages.QuerySingleOrDefaultAsync<TwPage>("GetPageRevisionInfoById.sql", param);
        }

        public static async Task<TwProcessingInstructionCollection> GetPageProcessingInstructionsByPageId(int pageId)
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
                    Collection = (await ManagedDataStorage.Pages.QueryAsync<TwProcessingInstruction>("GetPageProcessingInstructionsByPageId.sql", param)).ToList()
                };
            })).EnsureNotNull();
        }

        public static async Task<List<TwPageTag>> GetPageTagsById(int pageId)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId]);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    PageId = pageId
                };

                return await ManagedDataStorage.Pages.QueryAsync<TwPageTag>("GetPageTagsById.sql", param);
            })).EnsureNotNull();
        }

        public static async Task<List<TwPageRevision>> GetPageRevisionsInfoByNavigationPaged(
            string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                Navigation = navigation,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var query = RepositoryHelpers.TransposeOrderby("GetPageRevisionsInfoByNavigationPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwPageRevision>(query, param);
            });
        }

        public static async Task<List<TwPageRevision>> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
        {
            var param = new
            {
                UserId = userId,
                TopCount = topCount
            };

            return await ManagedDataStorage.Pages.QueryAsync<TwPageRevision>("GetTopRecentlyModifiedPagesInfoByUserId.sql", param);
        }

        public static async Task<string?> GetPageNavigationByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await ManagedDataStorage.Pages.ExecuteScalarAsync<string>("GetPageNavigationByPageId.sql", param);
        }

        public static async Task<List<TwPage>> GetTopRecentlyModifiedPagesInfo(int topCount)
        {
            var param = new
            {
                TopCount = topCount
            };

            return await ManagedDataStorage.Pages.QueryAsync<TwPage>("GetTopRecentlyModifiedPagesInfo.sql", param);
        }

        private static async Task<List<TwPageSearchToken>> GetFuzzyPageSearchTokens(List<TwPageToken> tokens, double minimumMatchScore)
        {
            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        private static async Task<List<TwPageSearchToken>> GetExactPageSearchTokens(List<TwPageToken> tokens, double minimumMatchScore)
        {
            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        private static async Task<List<TwPageSearchToken>> GetMeteredPageSearchTokens(List<string> searchTerms, bool allowFuzzyMatching)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Search, [string.Join(',', searchTerms), allowFuzzyMatching]);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var minimumMatchScore = await ConfigurationRepository.Get<float>("Search", "Minimum Match Score");

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

        public static async Task<List<TwPage>> PageSearch(List<string> searchTerms)
        {
            if (searchTerms.Count == 0)
            {
                return new List<TwPage>();
            }

            bool allowFuzzyMatching = await ConfigurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");
            var meteredSearchTokens = await GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching == true);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<TwPage>();
            }

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        public static async Task<List<TwPage>> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
        {
            if (searchTerms.Count == 0)
            {
                return new List<TwPage>();
            }

            pageSize ??= await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");
            allowFuzzyMatching ??= await ConfigurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");

            var meteredSearchTokens = await GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching == true);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<TwPage>();
            }

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        public static async Task<List<TwRelatedPage>> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageId = pageId,
                Similarity = similarity,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await ManagedDataStorage.Pages.QueryAsync<TwRelatedPage>("GetSimilarPagesPaged.sql", param);
        }

        public static async Task<List<TwRelatedPage>> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageId = pageId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return await ManagedDataStorage.Pages.QueryAsync<TwRelatedPage>("GetRelatedPagesPaged.sql", param);
        }

        public static async Task FlushPageCache(int pageId)
        {
            var pageNavigation = await GetPageNavigationByPageId(pageId);
            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [pageNavigation]));
            TwCache.ClearCategory(TwCacheKey.Build(TwCache.Category.Page, [pageId]));
        }

        public static async Task InsertPageComment(int pageId, Guid userId, string body)
        {
            var param = new
            {
                PageId = pageId,
                UserId = userId,
                Body = body,
                CreatedDate = DateTime.UtcNow
            };

            await ManagedDataStorage.Pages.ExecuteAsync("InsertPageComment.sql", param);

            await FlushPageCache(pageId);
        }

        public static async Task DeletePageCommentById(int pageId, int commentId)
        {
            var param = new
            {
                PageId = pageId,
                CommentId = commentId
            };

            await ManagedDataStorage.Pages.ExecuteAsync("DeletePageCommentById.sql", param);

            await FlushPageCache(pageId);
        }

        public static async Task DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId)
        {
            var param = new
            {
                PageId = pageId,
                UserId = userId,
                CommentId = commentId
            };

            await ManagedDataStorage.Pages.ExecuteAsync("DeletePageCommentByUserAndId.sql", param);

            await FlushPageCache(pageId);
        }

        public static async Task<int> GetTotalPageCommentCount(int pageId)
            => await ManagedDataStorage.Pages.ExecuteScalarAsync<int>("GetTotalPageCommentCount.sql", new { PageId = pageId });

        public static async Task<List<TwPageComment>> GetPageCommentsPaged(string navigation, int pageNumber)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [navigation, pageNumber, paginationSize]);

            return (await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    Navigation = navigation,
                    PageNumber = pageNumber,
                    PageSize = paginationSize
                };

                return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    return await o.QueryAsync<TwPageComment>("GetPageCommentsPaged.sql", param);
                });
            })).EnsureNotNull();
        }

        public static async Task<List<TwNonexistentPage>> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetMissingPagesPaged.sql", orderBy, orderByDirection);
            return await ManagedDataStorage.Pages.QueryAsync<TwNonexistentPage>(query, param);
        }

        public static async Task UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            var param = new
            {
                @PageId = pageId,
                @PageNavigation = pageNavigation
            };

            await ManagedDataStorage.Pages.ExecuteAsync("UpdateSinglePageReference.sql", param);

            await FlushPageCache(pageId);
        }

        public static async Task UpdatePageReferences(int pageId, List<TwPageReference> referencesPageNavigations)
        {
            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        public static async Task<List<TwPage>> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize,
                Instruction = instruction
            };

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QueryAsync<TwPage>("GetAllPagesByInstructionPaged.sql", param);
            });
        }

        public static async Task<List<int>> GetDeletedPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            return await ManagedDataStorage.DeletedPages.EphemeralAsync(async o =>
            {
                var param = new
                {
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempTokens", tokens);
                return await o.QueryAsync<int>("GetDeletedPageIdsByTokens.sql", param);
            });
        }

        public static async Task<List<int>> GetPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                var param = new
                {
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempTokens", tokens);
                return await o.QueryAsync<int>("GetPageIdsByTokens.sql", param);
            });
        }

        public static async Task<List<TwPage>> GetAllNamespacePagesPaged(int pageNumber, string namespaceName,
            string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize,
                Namespace = namespaceName
            };

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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
        public static async Task<List<TwPage>> GetAllPagesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            if (searchTerms?.Count > 0)
            {
                var pageIds = await GetPageIdsByTokens(searchTerms);

                return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    using var deletedpagerevisions_db = o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db");
                    using var tempTable = o.CreateTempTableFrom("TempPageIds", pageIds);

                    var query = RepositoryHelpers.TransposeOrderby("GetAllPagesByPageIdPaged.sql", orderBy, orderByDirection);
                    return await o.QueryAsync<TwPage>(query, param);
                });
            }

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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
        public static async Task<List<TwPage>> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null,
            string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            if (searchTerms?.Count > 0)
            {
                var pageIds = await GetDeletedPageIdsByTokens(searchTerms);
                return await ManagedDataStorage.DeletedPages.EphemeralAsync(async o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    using var tempTable = o.CreateTempTableFrom("TempPageIds", pageIds);

                    var query = RepositoryHelpers.TransposeOrderby("GetAllDeletedPagesByPageIdPaged.sql", orderBy, orderByDirection);
                    return await o.QueryAsync<TwPage>(query, param);
                });
            }

            return await ManagedDataStorage.DeletedPages.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                var query = RepositoryHelpers.TransposeOrderby("GetAllDeletedPagesPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwPage>(query, param);
            });
        }

        public static async Task<List<TwNamespaceStat>> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            var query = RepositoryHelpers.TransposeOrderby("GetAllNamespacesPaged.sql", orderBy, orderByDirection);

            return await ManagedDataStorage.Pages.QueryAsync<TwNamespaceStat>(query, param);
        }

        public static async Task<List<string>> GetAllNamespaces()
            => await ManagedDataStorage.Pages.QueryAsync<string>("GetAllNamespaces.sql");

        public static async Task<List<TwPage>> GetAllPages()
            => await ManagedDataStorage.Pages.QueryAsync<TwPage>("GetAllPages.sql");

        public static async Task<List<TwPage>> GetAllTemplatePages()
            => await ManagedDataStorage.Pages.QueryAsync<TwPage>("GetAllTemplatePages.sql");

        public static async Task<List<TwFeatureTemplate>> GetAllFeatureTemplates()
        {
            return (await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Configuration), async () =>
            {
                return (await ManagedDataStorage.Pages.QueryAsync<TwFeatureTemplate>("GetAllFeatureTemplates.sql")).ToList();
            })).EnsureNotNull();
        }

        public static async Task UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        public static async Task<TwPage?> GetPageRevisionById(int pageId, int? revision = null)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId, revision]), async () =>
            {
                var param = new
                {
                    PageId = pageId,
                    Revision = revision
                };

                return await ManagedDataStorage.Pages.QuerySingleOrDefaultAsync<TwPage>("GetPageRevisionById.sql", param);
            });
        }

        public static async Task SavePageSearchTokens(List<TwPageToken> items)
        {
            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTokens", items.Distinct());
                return await o.QueryAsync<TwPage>("SavePageSearchTokens.sql");
            });
        }

        public static async Task TruncateAllPageRevisions(string confirm)
        {
            if (confirm != "YES") //Are you REALLY sure?
            {
                return;
            }
            else
            {
                await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        public static async Task<int> GetCurrentPageRevision(int pageId)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId]), async () =>
            {
                var param = new
                {
                    PageId = pageId,
                };

                return await ManagedDataStorage.Pages.ExecuteScalarAsync<int>("GetCurrentPageRevision.sql", param);
            });
        }

        public static async Task<int> GetCurrentPageRevision(SqliteManagedInstance connection, int pageId)
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

        public static async Task<TwPage?> GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null)
        {
            var cacheKey = TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId, revision]);

            return await TwCache.AddOrGetAsync(cacheKey, async () =>
            {
                var param = new
                {
                    PageId = pageId,
                    Revision = revision
                };

                return await ManagedDataStorage.Pages.QuerySingleOrDefaultAsync<TwPage>("GetLimitedPageInfoByIdAndRevision.sql", param);
            });
        }

        /// <summary>
        /// Creates a new page or updates an existing page and its revision history in the data store.
        /// 
        /// DO NOT USE DIRECLTY: Use RepositoryHelpers.UpsertPage() instead.
        /// </summary>
        /// <remarks>If the page content or metadata has changed, a new revision is created and all
        /// attachments are associated with the latest revision. The method automatically manages transaction handling
        /// and revision incrementing.</remarks>
        /// <param name="page">The page to create or update. The page's properties determine whether a new page is created or an existing
        /// page is updated. Cannot be null.</param>
        /// <returns>The unique identifier of the created or updated page.</returns>
        internal static async Task<int> SavePage(TwPage page)
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

            var newDataHash = Security.Helpers.Crc32(page.Body ?? string.Empty);

            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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
        public static async Task<TwPage?> GetPageInfoByNavigation(string navigation)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [navigation]), async () =>
            {
                var param = new
                {
                    Navigation = navigation
                };

                return await ManagedDataStorage.Pages.QuerySingleOrDefaultAsync<TwPage?>("GetPageInfoByNavigation.sql", param);
            });
        }

        public static async Task<int> GetPageRevisionCountByPageId(int pageId)
        {
            return await TwCache.AddOrGetAsync(TwCacheKeyFunction.Build(TwCache.Category.Page, [pageId]), async () =>
            {
                var param = new
                {
                    PageId = pageId
                };

                return await ManagedDataStorage.Pages.ExecuteScalarAsync<int>("GetPageRevisionCountByNavigation.sql", param);
            });
        }

        public static async Task RestoreDeletedPageByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        public static async Task MovePageRevisionToDeletedById(int pageId, int revision, Guid userId)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision,
                DeletedByUserId = userId,
                DeletedDate = DateTime.UtcNow
            };

            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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

        public static async Task MovePageToDeletedById(int pageId, Guid userId)
        {
            var param = new
            {
                PageId = pageId,
                DeletedByUserId = userId,
                DeletedDate = DateTime.UtcNow
            };

            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                var transaction = o.BeginTransaction();
                try
                {
                    using var deletedpages_db = o.Attach("deletedpages.db", "deletedpages_db");

                    await o.ExecuteAsync("MovePageToDeletedById.sql", param);
                    transaction.Commit();
                    await ManagedDataStorage.Statistics.ExecuteAsync("DeletePageStatisticsByPageId.sql", param);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            await FlushPageCache(pageId);
        }

        public static async Task PurgeDeletedPageByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            await ManagedDataStorage.DeletedPages.ExecuteAsync("PurgeDeletedPageByPageId.sql", param);

            await PurgeDeletedPageRevisionsByPageId(pageId);

            await FlushPageCache(pageId);
        }

        public static async Task PurgeDeletedPages()
        {
            await ManagedDataStorage.DeletedPages.ExecuteAsync("PurgeDeletedPages.sql");

            await PurgeDeletedPageRevisions();
        }

        public static async Task<int> GetCountOfPageAttachmentsById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await ManagedDataStorage.Pages.ExecuteScalarAsync<int>("GetCountOfPageAttachmentsById.sql", param);
        }

        public static async Task<TwPage?> GetDeletedPageById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await ManagedDataStorage.DeletedPages.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QuerySingleOrDefaultAsync<TwPage>("GetDeletedPageById.sql", param);
            });
        }

        public static async Task<TwPage?> GetLatestPageRevisionById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QuerySingleOrDefaultAsync<TwPage>("GetLatestPageRevisionById.sql", param);
            });
        }

        public static async Task<int> GetPageNextRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return await ManagedDataStorage.Pages.ExecuteScalarAsync<int>("GetPageNextRevision.sql", param);
        }

        public static async Task<int> GetPagePreviousRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return await ManagedDataStorage.Pages.ExecuteScalarAsync<int>("GetPagePreviousRevision.sql", param);
        }

        public static async Task<List<TwDeletedPageRevision>> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var paginationSize = await ConfigurationRepository.Get<int>(WikiConfigurationGroup.Customization, "Pagination Size");

            var param = new
            {
                PageId = pageId,
                PageNumber = pageNumber,
                PageSize = paginationSize
            };

            return await ManagedDataStorage.DeletedPageRevisions.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var query = RepositoryHelpers.TransposeOrderby("GetDeletedPageRevisionsByIdPaged.sql", orderBy, orderByDirection);
                return await o.QueryAsync<TwDeletedPageRevision>(query, param);
            });
        }

        public static async Task PurgeDeletedPageRevisions()
        {
            await ManagedDataStorage.DeletedPageRevisions.ExecuteAsync("PurgeDeletedPageRevisions.sql");
        }

        public static async Task PurgeDeletedPageRevisionsByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            await ManagedDataStorage.DeletedPageRevisions.ExecuteAsync("PurgeDeletedPageRevisionsByPageId.sql", param);

            await FlushPageCache(pageId);
        }

        public static async Task PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            await ManagedDataStorage.DeletedPageRevisions.ExecuteAsync("PurgeDeletedPageRevisionByPageIdAndRevision.sql", param);

            await FlushPageCache(pageId);
        }

        public static async Task RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            await ManagedDataStorage.DeletedPageRevisions.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");
                await o.ExecuteAsync("RestoreDeletedPageRevisionByPageIdAndRevision.sql", param);
            });

            await FlushPageCache(pageId);
        }

        public static async Task<TwDeletedPageRevision?> GetDeletedPageRevisionById(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return await ManagedDataStorage.DeletedPageRevisions.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QueryFirstOrDefaultAsync<TwDeletedPageRevision>("GetDeletedPageRevisionById.sql", param);
            });
        }

        public static async Task<TwPage?> GetPageRevisionByNavigation(TwNamespaceNavigation navigation, int? revision = null)
        {
            var param = new
            {
                Navigation = navigation.Canonical,
                Revision = revision
            };

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return await o.QuerySingleOrDefaultAsync<TwPage>("GetPageRevisionByNavigation.sql", param);
            });
        }

        public static async Task<TwPage?> GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false)
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
                return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    return await o.QuerySingleOrDefaultAsync<TwPage>("GetPageRevisionByNavigation.sql", param);
                });
            });
        }

        #region Tags.

        public static async Task<List<TwTagAssociation>> GetAssociatedTags(string tag)
        {
            var param = new
            {
                @Tag = tag
            };

            return await ManagedDataStorage.Pages.QueryAsync<TwTagAssociation>("GetAssociatedTags.sql", param);
        }

        public static async Task<List<TwPage>> GetPageInfoByNamespaces(List<string> namespaces)
        {
            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempNamespaces", namespaces);
                return await o.QueryAsync<TwPage>("GetPageInfoByNamespaces.sql");
            });
        }

        public static async Task<List<TwPage>> GetPageInfoByTags(IEnumerable<string> tags)
        {
            var cleanedTags = tags.Select(o => TwNavigation.Clean(o));

            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTags", cleanedTags);
                return await o.QueryAsync<TwPage>("GetPageInfoByTags.sql");
            });
        }

        public static async Task<List<TwPage>> GetPageInfoByTag(string tag)
        {
            return await ManagedDataStorage.Pages.EphemeralAsync(async o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTags", new List<string> { TwNavigation.Clean(tag) });
                return await o.QueryAsync<TwPage>("GetPageInfoByTags.sql");
            });
        }

        public static async Task UpdatePageTags(int pageId, List<string> tags)
        {
            await ManagedDataStorage.Pages.EphemeralAsync(async o =>
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
    }
}
