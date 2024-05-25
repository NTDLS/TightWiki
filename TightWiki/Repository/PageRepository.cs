using DuoVia.FuzzyStrings;
using NTDLS.SqliteDapperWrapper;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Repository
{
    public static class PageRepository
    {
        public static Page? GetPageRevisionInfoById(int pageId, int? revision = null)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<Page>("GetPageRevisionInfoById.sql", param);
        }

        public static Page? GetPageInfoById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<Page>("GetPageInfoById.sql", param);
        }

        public static List<ProcessingInstruction> GetPageProcessingInstructionsByPageId(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.Query<ProcessingInstruction>("GetPageProcessingInstructionsByPageId.sql", param).ToList();
        }

        public static List<PageTag> GetPageTagsById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.Query<PageTag>("GetPageTagsById.sql", param).ToList();
        }

        public static List<PageRevision> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                Navigation = navigation,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.Query<PageRevision>("GetPageRevisionsInfoByNavigationPaged.sql", param).ToList();
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
                    TokenCount = tokens.Count()
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
                    TokenCount = tokens.Count()
                };

                using var tempTable = o.CreateTempTableFrom("TempSearchTerms", tokens.Distinct());
                return o.Query<PageSearchToken>("GetExactPageSearchTokens.sql", param).ToList();
            });
        }

        private static List<PageSearchToken> GetMeteredPageSearchTokens(List<string> searchTerms, bool allowFuzzyMatching)
        {
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

            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");
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

        public static List<RelatedPage> GetSimilarPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageId = pageId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Query<RelatedPage>("GetSimilarPagesPaged.sql", param).ToList();
        }

        public static List<RelatedPage> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageId = pageId,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Query<RelatedPage>("GetRelatedPagesPaged.sql", param).ToList();
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
        }

        public static void DeletePageCommentById(int pageId, int commentId)
        {
            var param = new
            {
                PageId = pageId,
                CommentId = commentId
            };

            ManagedDataStorage.Pages.Execute("DeletePageCommentById.sql", param);
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
        }

        public static List<PageComment> GetPageCommentsPaged(string navigation, int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                Navigation = navigation,
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.Query<PageComment>("GetPageCommentsPaged.sql", param).ToList();
            });
        }

        public static List<NonexistentPage> GetNonexistentPagesPaged(int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Query<NonexistentPage>("GetNonexistentPagesPaged.sql", param).ToList();
        }

        public static void UpdateSinglePageReference(string pageNavigation, int pageId)
        {
            var param = new
            {
                @PageId = pageId,
                @PageNavigation = pageNavigation
            };

            ManagedDataStorage.Pages.Execute("UpdateSinglePageReference.sql", param);
        }

        public static void UpdatePageReferences(int pageId, List<NameNav> referencesPageNavigations)
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
        }

        public static List<Page> GetAllPagesByInstructionPaged(int pageNumber, int? pageSize = null, string? instruction = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize,
                Instruction = instruction
            };

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.Query<Page>("GetAllPagesByInstructionPaged.sql", param).ToList();
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

        /// <summary>
        /// Unlike the search, this method retunrs all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        /// <param name="pageNumber"></param>
        /// <param name="pageSize"></param>
        /// <param name="searchTerms"></param>
        /// <returns></returns>
        public static List<Page> GetAllPagesPaged(int pageNumber, int? pageSize = null, List<string>? searchTerms = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            var pageIds = GetPageIdsByTokens(searchTerms);
            if (pageIds.Count > 0)
            {
                return ManagedDataStorage.Pages.Ephemeral(o =>
                {
                    using var users_db = o.Attach("users.db", "users_db");
                    using var tempTable = o.CreateTempTableFrom("TempPageIds", pageIds);
                    return o.Query<Page>("GetAllPagesByPageIdPaged", param).ToList();
                });
            }

            return ManagedDataStorage.Pages.Ephemeral(o =>
            {
                using var users_db = o.Attach("users.db", "users_db");
                return o.Query<Page>("GetAllPagesPaged.sql", param).ToList();
            });
        }

        public static List<NamespaceStat> GetAllNamespacesPaged(int pageNumber, int? pageSize = null)
        {
            pageSize ??= ConfigurationRepository.Get<int>("Customization", "Pagination Size");

            var param = new
            {
                PageNumber = pageNumber,
                PageSize = pageSize
            };

            return ManagedDataStorage.Pages.Query<NamespaceStat>("GetAllNamespacesPaged.sql", param).ToList();
        }

        public static List<Page> GetAllPages()
        {
            return ManagedDataStorage.Pages.Query<Page>("GetAllPages.sql").ToList();
        }

        public static void UpdatePageProcessingInstructions(int pageId, List<string> instructions)
        {
            ManagedDataStorage.Pages.Ephemeral(o =>
            {
                var param = new
                {
                    PageId = pageId
                };

                using var tempTable = o.CreateTempTableFrom("TempInstructions", instructions);
                return o.Query<Page>("UpdatePageProcessingInstructions.sql", param).ToList();
            });
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

        public static int GetCurrentPageRevision(ManagedDataStorageInstance connection, int pageId)
        {
            var param = new
            {
                PageId = pageId,
            };

            return connection.ExecuteScalar<int>("GetCurrentPageRevision.sql", param);
        }

        public static PageInfoAndHash GetPageInfoAndBodyByIdAndRevision(int pageId, int? revision = null)
        {
            var param = new
            {
                PageId = pageId,
                Revision = revision
            };

            return ManagedDataStorage.Pages.QuerySingle<PageInfoAndHash>("GetPageInfoByIdAndRevision.sql", param);
        }

        public static int SavePage(Page page)
        {
            var pageUpsertParam = new
            {
                PageId = page.Id,
                Name = page.Name,
                Navigation = NamespaceNavigation.CleanAndValidate(page.Name),
                Description = page.Description,
                Body = page.Body,
                Namespace = page.Namespace,
                CreatedByUserId = page.CreatedByUserId,
                CreatedDate = page.CreatedDate,
                ModifiedByUserId = page.ModifiedByUserId,
                ModifiedDate = DateTime.UtcNow
            };

            int newDataHash = Utility.SimpleChecksum(page.Body);

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
                        var currentRevisionInfo = GetPageInfoAndBodyByIdAndRevision(page.Id);
                        currentPageRevision = currentRevisionInfo.Revision;

                        //Update the existing page.
                        o.Execute("UpdatePage.sql", pageUpsertParam);

                        //Determine if anyhting has actually changed.
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

                        var InsertPageRevisionParam = new
                        {
                            PageId = page.Id,
                            Name = page.Name,
                            Namespace = page.Namespace,
                            Description = page.Description,
                            Body = page.Body,
                            DataHash = newDataHash,
                            PageRevision = currentPageRevision,
                            ModifiedByUserId = page.ModifiedByUserId,
                            ModifiedDate = DateTime.UtcNow,
                        };
                        //Insert the new actual page revision entry (this is the data).
                        o.Execute("InsertPageRevision.sql", InsertPageRevisionParam);

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
        /// <param name="navigation"></param>
        /// <returns></returns>
        public static Page? GetPageInfoByNavigation(string navigation)
        {
            var param = new
            {
                Navigation = navigation
            };

            return ManagedDataStorage.Pages.QuerySingleOrDefault<Page>("GetPageInfoByNavigation.sql", param);
        }

        public static void DeletePageById(int pageId)
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
                    o.Execute("DeletePageByPageId.sql", param);
                    transaction.Commit();
                    ManagedDataStorage.Statistics.Execute("DeletePageStatisticsByPageId.sql", param);
                }
                catch
                {
                    transaction.Rollback();
                    throw;
                }
            });
        }

        public static int GetCountOfPageAttachmentsById(int pageId)
        {
            var param = new
            {
                PageId = pageId
            };

            return ManagedDataStorage.Pages.ExecuteScalar<int>("GetCountOfPageAttachmentsById.sql", param);
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
                var result = WikiCache.Get<Page>(cacheKey);
                if (result == null)
                {
                    result = GetPageRevisionByNavigation(navigation.Canonical, revision, false);
                    if (result != null)
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

        public static List<TagAssociation> GetAssociatedTags(string groupName)
        {
            var param = new
            {
                GroupName = groupName
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
