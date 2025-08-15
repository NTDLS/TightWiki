using DuoVia.FuzzyStrings;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Caching;
using TightWiki.Engine.Library;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;

namespace TightWiki.Repository
{
    public static class PageRepository
    {
        public static IEnumerable<Page> AutoComplete(string? searchText)
            => ManagedDataStorage.Pages.Query<Page>("PageAutoComplete.sql", new { SearchText = searchText ?? string.Empty });

        public static Page? GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<Page>("GetPageRevisionInfoById.sql", param);
        }

        public static ProcessingInstructionCollection GetPageProcessingInstructionsByPageId(int pageId, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]);
                if (!WikiCache.TryGet<ProcessingInstructionCollection>(cacheKey, out var result))
                {
                    result = GetPageProcessingInstructionsByPageId(pageId, false);
                    WikiCache.Put(cacheKey, result);
                }

                return result;
            }

            var param = new
            {
                PageId = pageId
            };

            return new ProcessingInstructionCollection()
            {
                Collection = ManagedDataStorage.Pages.Query<ProcessingInstruction>("GetPageProcessingInstructionsByPageId.sql", param).ToList()
            };
        }

        public static List<PageTag> GetPageTagsById(int pageId, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId]);
                if (!WikiCache.TryGet<List<PageTag>>(cacheKey, out var result))
                {
                    result = GetPageTagsById(pageId, false);
                    WikiCache.Put(cacheKey, result);
                }

                return result;
            }

            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.Query<PageTag>("GetPageTagsById.sql", param).ToList();
        }

        public static List<PageRevision> GetPageRevisionsInfoByNavigationPaged(
            string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;

            var param = new
            {
                Navigation = navigation,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var query = RepositoryHelper.TransposeOrderby("GetPageRevisionsInfoByNavigationPaged.sql", orderBy, orderByDirection);
                return o.Query<PageRevision>(query, param).ToList();
            });
        }

        public static List<PageRevision> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount)
        {
            var param = new
            {
                UserId = userId,
                TopCount = topCount
            };

            return ManagedDataStorage.Pages.Query<PageRevision>("GetTopRecentlyModifiedPagesInfoByUserId.sql", param).ToList();
        }

        public static string? GetPageNavigationByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.ExecuteScalar<string>("GetPageNavigationByPageId.sql", param);
        }

        public static List<Page> GetTopRecentlyModifiedPagesInfo(int topCount)
        {
            var param = new
            {
                TopCount = topCount
            };

            return ManagedDataStorage.Pages.Query<Page>("GetTopRecentlyModifiedPagesInfo.sql", param).ToList();
        }

        private static List<PageSearchToken> GetFuzzyPageSearchTokens(List<PageToken> tokens, double minimumMatchScore)
        {
            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var param = new
                {
                    MinimumMatchScore = minimumMatchScore,
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", tokens.Distinct());
                return o.Query<PageSearchToken>("GetFuzzyPageSearchTokens.sql", param).ToList();
            });
        }

        private static List<PageSearchToken> GetExactPageSearchTokens(List<PageToken> tokens, double minimumMatchScore)
        {
            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var param = new
                {
                    MinimumMatchScore = minimumMatchScore,
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", tokens.Distinct());
                return o.Query<PageSearchToken>("GetExactPageSearchTokens.sql", param).ToList();
            });
        }

        private static List<PageSearchToken> GetMeteredPageSearchTokens(List<string> searchTerms, bool allowFuzzyMatching, bool allowCache = true)
        {
            if (allowCache)
            {
                //This caching is really just used for paging - so we don't have to do a token search for every click of next/previous.
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Search, [string.Join(',', searchTerms), allowFuzzyMatching]);
                if (!WikiCache.TryGet<List<PageSearchToken>>(cacheKey, out var result))
                {
                    result = GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching, false);
                    WikiCache.Put(cacheKey, result);
                }

                return result;
            }

            var minimumMatchScore = ConfigurationRepository.Get<float>("Search", "Minimum Match Score");

            var searchTokens = (from o in searchTerms
                                select new PageToken
                                {
                                    Token = o,
                                    DoubleMetaphone = o.ToDoubleMetaphone()
                                }).ToList();

            if (allowFuzzyMatching == true)
            {
                var allTokens = GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
                var fuzzyTokens = GetFuzzyPageSearchTokens(searchTokens, minimumMatchScore / 2.0);

                allTokens.AddRange(fuzzyTokens);

                return allTokens
                        .GroupBy(token => token.PageId)
                        .Where(group => group.Sum(g => g.Score) >= minimumMatchScore) // Filtering groups
                        .Select(group => new PageSearchToken
                        {
                            PageId = group.Key,
                            Match = group.Max(g => g.Match),
                            Weight = group.Max(g => g.Weight),
                            Score = group.Max(g => g.Score)
                        }).ToList();
            }
            else
            {
                return GetExactPageSearchTokens(searchTokens, minimumMatchScore / 2.0);
            }
        }

        public static List<Page> PageSearch(List<string> searchTerms)
        {
            if (searchTerms.Count == 0)
            {
                return new List<Page>();
            }

            bool allowFuzzyMatching = ConfigurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");
            var meteredSearchTokens = GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching == true);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<Page>();
            }

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var param = new
                {
                    MaximumScore = meteredSearchTokens.Max(t => t.Score)
                };

                using var users_db = o.Attach("users.db", "users_db");
                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", meteredSearchTokens);
                return o.Query<Page>("PageSearch.sql", param).ToList();
            });
        }

        public static List<Page> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null)
        {
            if (searchTerms.Count == 0)
            {
                return new List<Page>();
            }

            pageSize ??= GlobalConfiguration.PaginationSize;
            allowFuzzyMatching ??= ConfigurationRepository.Get<bool>("Search", "Allow Fuzzy Matching");

            var meteredSearchTokens = GetMeteredPageSearchTokens(searchTerms, allowFuzzyMatching == true);
            if (meteredSearchTokens.Count == 0)
            {
                return new List<Page>();
            }

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var param = new
                {
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    MaximumScore = meteredSearchTokens.Max(t => t.Score)
                };

                using var users_db = o.Attach("users.db", "users_db");
                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", meteredSearchTokens);
                var results = o.Query<Page>("PageSearchPaged.sql", param).ToList();
                return results;
            });
        }

        public static List<RelatedPage> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;

            var param = new
            {
                PageId = pageId,
                Similarity = similarity,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Query<RelatedPage>("GetSimilarPagesPaged.sql", param).ToList();
        }

        public static List<RelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= GlobalConfiguration.PaginationSize;

            var param = new
            {
                PageId = pageId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Query<RelatedPage>("GetRelatedPagesPaged.sql", param).ToList();
        }

        public static void FlushPageCache(int pageId)
        {
            var pageNavigation = GetPageNavigationByPageId(pageId);
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [pageNavigation]));
            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [pageId]));
        }

        public static void InsertPageComment(int pageId, Guid userId, string body)
        {
            var param = new
            {
                PageId = pageId,
                UserId = userId,
                Body = body,
                CreatedDate = DateTime.UtcNow
            };

            ManagedDataStorage.Pages.Execute("InsertPageComment.sql", param);

            FlushPageCache(pageId);
        }

        public static void DeletePageCommentById(int pageId, int commentId)
        {
            var param = new
            {
                PageId = pageId,
                CommentId = commentId
            };

            ManagedDataStorage.Pages.Execute("DeletePageCommentById.sql", param);

            FlushPageCache(pageId);
        }

        public static void DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId)
        {
            var param = new
            {
                PageId = pageId,
                UserId = userId,
                CommentId = commentId
            };

            ManagedDataStorage.Pages.Execute("DeletePageCommentByUserAndId.sql", param);

            FlushPageCache(pageId);
        }

        public static List<PageComment> GetPageCommentsPaged(string navigation, int pageNumber, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [navigation, pageNumber, GlobalConfiguration.PaginationSize]);
                if (!WikiCache.TryGet<List<PageComment>>(cacheKey, out var result))
                {
                    result = GetPageCommentsPaged(navigation, pageNumber, false);
                    WikiCache.Put(cacheKey, result);
                }

                return result;
            }

            var param = new
            {
                Navigation = navigation,
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.Query<PageComment>("GetPageCommentsPaged.sql", param).ToList();
            });
        }

        public static List<NonexistentPage> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            var query = RepositoryHelper.TransposeOrderby("GetMissingPagesPaged.sql", orderBy, orderByDirection);
            return ManagedDataStorage.Pages.Query<NonexistentPage>(query, param).ToList();
        }

        public static void UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            var param = new
            {
                @PageId = pageId,
                @PageNavigation = pageNavigation
            };

            ManagedDataStorage.Pages.Execute("UpdateSinglePageReference.sql", param);

            FlushPageCache(pageId);
        }

        public static void UpdatePageReferences(int pageId, List<PageReference> referencesPageNavigations)
        {
            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var param = new
                {
                    PageId = pageId
                };

                using var tempTable = o.CreateTempTableFrom("TempReferences", referencesPageNavigations.Distinct());
                return o.Query<Page>("UpdatePageReferences.sql", param).ToList();
            });

            FlushPageCache(pageId);
        }

        public static List<Page> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize,
                Instruction = instruction
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.Query<Page>("GetAllPagesByInstructionPaged.sql", param).ToList();
            });
        }

        public static List<int> GetDeletedPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            return ManagedDataStorage.DeletedPages.Ephemeral(o =>
            {
                var param = new
                {
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempTokens", tokens);
                return o.Query<int>("GetDeletedPageIdsByTokens.sql", param).ToList();
            });
        }

        public static List<int> GetPageIdsByTokens(List<string>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return new List<int>();
            }

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var param = new
                {
                    TokenCount = tokens.Count
                };

                using var tempTable = o.CreateTempTableFrom("TempTokens", tokens);
                return o.Query<int>("GetPageIdsByTokens.sql", param).ToList();
            });
        }

        public static List<Page> GetAllNamespacePagesPaged(int pageNumber, string namespaceName,
            string? orderBy = null, string? orderByDirection = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize,
                Namespace = namespaceName
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                var query = RepositoryHelper.TransposeOrderby("GetAllNamespacePagesPaged.sql", orderBy, orderByDirection);
                return o.Query<Page>(query, param).ToList();
            });
        }

        /// <summary>
        /// Unlike the search, this method returns all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        public static List<Page> GetAllPagesPaged(int pageNumber,
            string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            if (searchTerms?.Count > 0)
            {
                var pageIds = GetPageIdsByTokens(searchTerms);

                return ManagedDataStorage.Pages.Ephemeral(o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    using var deletedpagerevisions_db = o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db");
                    using var tempTable = o.CreateTempTableFrom("TempPageIds", pageIds);

                    var query = RepositoryHelper.TransposeOrderby("GetAllPagesByPageIdPaged.sql", orderBy, orderByDirection);
                    return o.Query<Page>(query, param).ToList();
                });
            }

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                using var deletedpagerevisions_db = o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db");

                var query = RepositoryHelper.TransposeOrderby("GetAllPagesPaged.sql", orderBy, orderByDirection);
                return o.Query<Page>(query, param).ToList();
            });
        }

        /// <summary>
        /// Unlike the search, this method returns all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        public static List<Page> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null,
            string? orderByDirection = null, List<string>? searchTerms = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            if (searchTerms?.Count > 0)
            {
                var pageIds = GetDeletedPageIdsByTokens(searchTerms);
                return ManagedDataStorage.DeletedPages.Ephemeral(o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    using var tempTable = o.CreateTempTableFrom("TempPageIds", pageIds);

                    var query = RepositoryHelper.TransposeOrderby("GetAllDeletedPagesByPageIdPaged.sql", orderBy, orderByDirection);
                    return o.Query<Page>(query, param).ToList();
                });
            }

            return ManagedDataStorage.DeletedPages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                var query = RepositoryHelper.TransposeOrderby("GetAllDeletedPagesPaged.sql", orderBy, orderByDirection);
                return o.Query<Page>(query, param).ToList();
            });
        }

        public static List<NamespaceStat> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null)
        {
            var param = new
            {
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            var query = RepositoryHelper.TransposeOrderby("GetAllNamespacesPaged.sql", orderBy, orderByDirection);

            return ManagedDataStorage.Pages.Query<NamespaceStat>(query, param).ToList();
        }

        public static List<string> GetAllNamespaces()
            => ManagedDataStorage.Pages.Query<string>("GetAllNamespaces.sql").ToList();

        public static List<Page> GetAllPages()
            => ManagedDataStorage.Pages.Query<Page>("GetAllPages.sql").ToList();

        public static List<Page> GetAllTemplatePages()
            => ManagedDataStorage.Pages.Query<Page>("GetAllTemplatePages.sql").ToList();

        public static List<FeatureTemplate> GetAllFeatureTemplates()
        {
            var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page);
            if (!WikiCache.TryGet<List<FeatureTemplate>>(cacheKey, out var result))
            {
                result = ManagedDataStorage.Pages.Query<FeatureTemplate>("GetAllFeatureTemplates.sql").ToList();
                WikiCache.Put(cacheKey, result);
            }
            return result;
        }

        public static void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var param = new
                {
                    PageId = pageId
                };

                instructions = instructions.Select(o => o.ToLowerInvariant()).Distinct().ToList();

                using var tempTable = o.CreateTempTableFrom("TempInstructions", instructions);
                return o.Query<Page>("UpdatePageProcessingInstructions.sql", param).ToList();
            });

            FlushPageCache(pageId);
        }

        public static Page? GetPageRevisionById(int pageId, int? revision = null)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<Page>("GetPageRevisionById.sql", param);
        }

        public static void SavePageSearchTokens(List<PageToken> items)
        {
            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTokens", items.Distinct());
                return o.Query<Page>("SavePageSearchTokens.sql").ToList();
            });
        }

        public static void TruncateAllPageRevisions(string confirm)
        {
            if (confirm != "YES") //Are you REALLY sure?
            {
                return;
            }
            else
            {
                ManagedDataStorage.Pages.Ephemeral(o =>
                {
                    var transaction = o.BeginTransaction();
                    try
                    {
                        o.Execute("TruncateAllPageRevisions.sql");
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

        public static int GetCurrentPageRevision(int pageId)
        {
            var param = new
            {
                PageId = pageId,
            };

            return ManagedDataStorage.Pages.ExecuteScalar<int>("GetCurrentPageRevision.sql", param);
        }

        public static int GetCurrentPageRevision(SqliteManagedInstance connection, int pageId)
        {
            var param = new
            {
                PageId = pageId,
            };

            return connection.ExecuteScalar<int>("GetCurrentPageRevision.sql", param);
        }

        public static Page? GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null, bool allowCache = true)
        {
            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [pageId, revision]);
                if (!WikiCache.TryGet<Page>(cacheKey, out var result))
                {
                    if ((result = GetLimitedPageInfoByIdAndRevision(pageId, revision, false)) != null)
                    {
                        WikiCache.Put(cacheKey, result);
                    }
                }
                return result;
            }

            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<Page>("GetLimitedPageInfoByIdAndRevision.sql", param);
        }

        public static int SavePage(Page page)
        {
            var pageUpsertParam = new
            {
                PageId = page.Id,
                Name = page.Name,
                Navigation = NamespaceNavigation.CleanAndValidate(page.Name),
                Description = page.Description,
                Body = page.Body ?? string.Empty,
                Namespace = page.Namespace,
                CreatedByUserId = page.CreatedByUserId,
                CreatedDate = page.CreatedDate,
                ModifiedByUserId = page.ModifiedByUserId,
                ModifiedDate = DateTime.UtcNow
            };

            var newDataHash = Security.Helpers.Crc32(page.Body ?? string.Empty);

            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var transaction = o.BeginTransaction();

                try
                {
                    int currentPageRevision = 0;
                    bool hasPageChanged = false;

                    if (page.Id == 0)
                    {
                        //This is a new page, just insert it.
                        page.Id = o.ExecuteScalar<int>("CreatePage.sql", pageUpsertParam);
                        hasPageChanged = true;
                    }
                    else
                    {
                        //Get current page so we can determine if anything has changed.
                        var currentRevisionInfo = GetLimitedPageInfoByIdAndRevision(page.Id)
                            ?? throw new Exception("The page could not be found.");

                        currentPageRevision = currentRevisionInfo.Revision;

                        //Update the existing page.
                        o.Execute("UpdatePage.sql", pageUpsertParam);

                        //Determine if anything has actually changed.
                        hasPageChanged = currentRevisionInfo.Name != page.Name
                            || currentRevisionInfo.Namespace != page.Namespace
                            || currentRevisionInfo.Description != page.Description
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
                        o.Execute("UpdatePageRevisionNumber.sql", updatePageRevisionNumberParam);

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
                        o.Execute("InsertPageRevision.sql", insertPageRevisionParam);

                        var reassociateAllPageAttachmentsParam = new
                        {
                            PageId = page.Id,
                            PageRevision = currentPageRevision,
                        };
                        //Associate all page attachments with the latest revision.
                        o.Execute("ReassociateAllPageAttachments.sql", reassociateAllPageAttachmentsParam);
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
        public static Page? GetPageInfoByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<Page>("GetPageInfoByNavigation.sql", param);
        }

        public static int GetPageRevisionCountByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.ExecuteScalar<int>("GetPageRevisionCountByNavigation.sql", param);
        }

        public static void RestoreDeletedPageByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var transaction = o.BeginTransaction();
                try
                {
                    using var deletedpages_db = o.Attach("deletedpages.db", "deletedpages_db");
                    o.Execute("RestoreDeletedPageByPageId.sql", param);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            FlushPageCache(pageId);
        }

        public static void MovePageRevisionToDeletedById(int pageId, int revision, Guid userId)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision,
                DeletedByUserId = userId,
                DeletedDate = DateTime.UtcNow
            };

            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var transaction = o.BeginTransaction();
                try
                {
                    using var deletedpagerevisions_db = o.Attach("deletedpagerevisions.db", "deletedpagerevisions_db");
                    o.Execute("MovePageRevisionToDeletedById.sql", param);
                    transaction.Commit();
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            FlushPageCache(pageId);
        }

        public static void MovePageToDeletedById(int pageId, Guid userId)
        {
            var param = new
            {
                PageId = pageId,
                DeletedByUserId = userId,
                DeletedDate = DateTime.UtcNow
            };

            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var transaction = o.BeginTransaction();
                try
                {
                    using var deletedpages_db = o.Attach("deletedpages.db", "deletedpages_db");

                    o.Execute("MovePageToDeletedById.sql", param);
                    transaction.Commit();
                    ManagedDataStorage.Statistics.Execute("DeletePageStatisticsByPageId.sql", param);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });

            FlushPageCache(pageId);
        }

        public static void PurgeDeletedPageByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            ManagedDataStorage.DeletedPages.Execute("PurgeDeletedPageByPageId.sql", param);

            PurgeDeletedPageRevisionsByPageId(pageId);

            FlushPageCache(pageId);
        }

        public static void PurgeDeletedPages()
        {
            ManagedDataStorage.DeletedPages.Execute("PurgeDeletedPages.sql");

            PurgeDeletedPageRevisions();
        }

        public static int GetCountOfPageAttachmentsById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.ExecuteScalar<int>("GetCountOfPageAttachmentsById.sql", param);
        }

        public static Page? GetDeletedPageById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.DeletedPages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.QuerySingleOrDefault<Page>("GetDeletedPageById.sql", param);
            });
        }

        public static Page? GetLatestPageRevisionById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.QuerySingleOrDefault<Page>("GetLatestPageRevisionById.sql", param);
            });
        }

        public static int GetPageNextRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return ManagedDataStorage.Pages.ExecuteScalar<int>("GetPageNextRevision.sql", param);
        }

        public static int GetPagePreviousRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return ManagedDataStorage.Pages.ExecuteScalar<int>("GetPagePreviousRevision.sql", param);
        }

        public static List<DeletedPageRevision> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber,
            string? orderBy = null, string? orderByDirection = null)
        {
            var param = new
            {
                PageId = pageId,
                PageNumber = pageNumber,
                PageSize = GlobalConfiguration.PaginationSize
            };

            return ManagedDataStorage.DeletedPageRevisions.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");

                var query = RepositoryHelper.TransposeOrderby("GetDeletedPageRevisionsByIdPaged.sql", orderBy, orderByDirection);
                return o.Query<DeletedPageRevision>(query, param).ToList();
            });
        }

        public static void PurgeDeletedPageRevisions()
        {
            ManagedDataStorage.DeletedPageRevisions.Execute("PurgeDeletedPageRevisions.sql");
        }

        public static void PurgeDeletedPageRevisionsByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            ManagedDataStorage.DeletedPageRevisions.Execute("PurgeDeletedPageRevisionsByPageId.sql", param);

            FlushPageCache(pageId);
        }

        public static void PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            ManagedDataStorage.DeletedPageRevisions.Execute("PurgeDeletedPageRevisionByPageIdAndRevision.sql", param);

            FlushPageCache(pageId);
        }

        public static void RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            ManagedDataStorage.DeletedPageRevisions.Ephemeral(o =>
            {
                using var users_db = o.Attach("pages.db", "pages_db");
                o.Execute("RestoreDeletedPageRevisionByPageIdAndRevision.sql", param);
            });

            FlushPageCache(pageId);
        }

        public static DeletedPageRevision? GetDeletedPageRevisionById(int pageId, int revision)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return ManagedDataStorage.DeletedPageRevisions.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.Query<DeletedPageRevision>("GetDeletedPageRevisionById.sql", param).FirstOrDefault();
            });
        }

        public static Page? GetPageRevisionByNavigation(NamespaceNavigation navigation, int? revision = null)
        {
            var param = new
            {
                Navigation = navigation.Canonical,
                Revision = revision
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.QuerySingleOrDefault<Page>("GetPageRevisionByNavigation.sql", param);
            });
        }

        public static Page? GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool allowCache = true)
        {
            var navigation = new NamespaceNavigation(givenNavigation);

            if (allowCache)
            {
                var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [navigation.Canonical, revision]);
                if (!WikiCache.TryGet<Page>(cacheKey, out var result))
                {
                    if ((result = GetPageRevisionByNavigation(navigation.Canonical, revision, false)) != null)
                    {
                        WikiCache.Put(cacheKey, result);
                    }
                }

                return result;
            }

            var param = new
            {
                Navigation = navigation.Canonical,
                Revision = revision
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.QuerySingleOrDefault<Page>("GetPageRevisionByNavigation.sql", param);
            });
        }

        #region Tags.

        public static List<TagAssociation> GetAssociatedTags(string tag)
        {
            var param = new
            {
                @Tag = tag
            };

            return ManagedDataStorage.Pages.Query<TagAssociation>("GetAssociatedTags.sql", param).ToList();
        }

        public static List<Page> GetPageInfoByNamespaces(List<string> namespaces)
        {
            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempNamespaces", namespaces);
                return o.Query<Page>("GetPageInfoByNamespaces.sql").ToList();
            });
        }

        public static List<Page> GetPageInfoByTags(List<string> tags)
        {
            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTags", tags);
                return o.Query<Page>("GetPageInfoByTags.sql").ToList();
            });
        }

        public static List<Page> GetPageInfoByTag(string tag)
        {
            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var tempTable = o.CreateTempTableFrom("TempTags", new List<string> { tag });
                return o.Query<Page>("GetPageInfoByTags.sql").ToList();
            });
        }

        public static void UpdatePageTags(int pageId, List<string> tags)
        {
            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                tags = tags.Select(o => o.ToLowerInvariant()).Distinct().ToList();

                using var tempTable = o.CreateTempTableFrom("TempTags", tags);

                var param = new
                {
                    PageId = pageId
                };

                return o.Query<Page>("UpdatePageTags.sql", param).ToList();
            });
        }

        #endregion
    }
}
