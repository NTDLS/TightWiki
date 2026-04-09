using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    /// Data access for pages, page revisions, page comments, page processing instructions, page tags, page references, and related data.
    /// </summary>
    public interface ITwPageRepository
    {
        /// <summary>
        /// SQLite factory used to access the pages database.
        /// </summary>
        SqliteManagedFactory PagesFactory { get; }

        /// <summary>
        /// SQLite factory used to access the deleted pages database.
        /// </summary>
        SqliteManagedFactory DeletedPagesFactory { get; }

        /// <summary>
        /// SQLite factory used to access the deleted page revisions database.
        /// </summary>
        SqliteManagedFactory DeletedPageRevisionsFactory { get; }

        /// <summary>
        /// Returns pages whose titles or navigation match the given search text, for use in autocomplete suggestions.
        /// </summary>
        Task<List<TwPage>> AutoCompletePage(string? searchText);

        /// <summary>
        /// Returns namespaces that match the given search text, for use in autocomplete suggestions.
        /// </summary>
        Task<List<string>> AutoCompleteNamespace(string? searchText);

        /// <summary>
        /// Returns basic page revision info for the specified page and optional revision number.
        /// </summary>
        Task<TwPage?> GetPageRevisionInfoById(int pageId, int? revision = null);

        /// <summary>
        /// Returns all processing instructions associated with the specified page.
        /// </summary>
        Task<TwProcessingInstructionCollection> GetPageProcessingInstructionsByPageId(int pageId);

        /// <summary>
        /// Returns all tags associated with the specified page.
        /// </summary>
        Task<List<TwPageTag>> GetPageTagsById(int pageId);

        /// <summary>
        /// Returns a paged list of revision history entries for the page with the specified navigation path.
        /// </summary>
        Task<List<TwPageRevision>> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);

        /// <summary>
        /// Returns the most recently modified pages for the specified user, up to the given count.
        /// </summary>
        Task<List<TwPageRevision>> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount);

        /// <summary>
        /// Returns the navigation path for the specified page ID.
        /// </summary>
        Task<string?> GetPageNavigationByPageId(int pageId);

        /// <summary>
        /// Returns the most recently modified pages, up to the given count.
        /// </summary>
        Task<List<TwPage>> GetTopRecentlyModifiedPagesInfo(int topCount);

        /// <summary>
        /// Returns the most recently created pages, up to the given count.
        /// </summary>
        Task<List<TwPage>> GetTopRecentlyCreatedPagesInfo(int topCount);

        /// <summary>
        /// Returns the most frequently viewed pages, up to the given count.
        /// </summary>
        Task<List<TwPage>> GetTopViewedPagesInfo(int topCount);

        /// <summary>
        /// Returns the most frequently edited pages, up to the given count.
        /// </summary>
        Task<List<TwPage>> GetTopEditedPagesInfo(int topCount);

        /// <summary>
        /// Returns pages that match all of the specified search terms.
        /// </summary>
        Task<List<TwPage>> PageSearch(List<string> searchTerms);

        /// <summary>
        /// Returns a paged list of pages that match the specified search terms, with optional fuzzy matching.
        /// </summary>
        Task<List<TwPage>> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null);

        /// <summary>
        /// Returns a paged list of pages similar to the specified page, filtered by a minimum similarity score.
        /// </summary>
        Task<List<TwRelatedPage>> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null);

        /// <summary>
        /// Returns a paged list of pages that are related to the specified page via tags or references.
        /// </summary>
        Task<List<TwRelatedPage>> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null);

        /// <summary>
        /// Clears the cached rendered content for the specified page.
        /// </summary>
        Task FlushPageCache(int pageId);

        /// <summary>
        /// Inserts a new comment on the specified page by the specified user.
        /// </summary>
        Task InsertPageComment(int pageId, Guid userId, string body);

        /// <summary>
        /// Deletes a comment by its ID from the specified page.
        /// </summary>
        Task DeletePageCommentById(int pageId, int commentId);

        /// <summary>
        /// Deletes a comment by its ID from the specified page, only if it was authored by the specified user.
        /// </summary>
        Task DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId);

        /// <summary>
        /// Returns the total number of comments on the specified page.
        /// </summary>
        Task<int> GetTotalPageCommentCount(int pageId);

        /// <summary>
        /// Returns a paged list of comments for the page with the specified navigation path.
        /// </summary>
        Task<List<TwPageComment>> GetPageCommentsPaged(string navigation, int pageNumber);

        /// <summary>
        /// Returns a paged list of pages that are referenced but do not yet exist in the wiki.
        /// </summary>
        Task<List<TwNonexistentPage>> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Updates the reference record for a single page by its navigation path and ID.
        /// </summary>
        Task UpdateSinglePageReference(string pageNavigation, int pageId);

        /// <summary>
        /// Replaces all outbound page references for the specified page.
        /// </summary>
        Task UpdatePageReferences(int pageId, List<TwPageReference> referencesPageNavigations);

        /// <summary>
        /// Returns a paged list of all pages that have the specified processing instruction applied.
        /// </summary>
        Task<List<TwPage>> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null);

        /// <summary>
        /// Returns the IDs of deleted pages whose search tokens match any of the specified tokens.
        /// </summary>
        Task<List<int>> GetDeletedPageIdsByTokens(List<string>? tokens);

        /// <summary>
        /// Returns the IDs of active pages whose search tokens match any of the specified tokens.
        /// </summary>
        Task<List<int>> GetPageIdsByTokens(List<string>? tokens);

        /// <summary>
        /// Returns a paged list of all pages within the specified namespace, with optional sorting.
        /// </summary>
        Task<List<TwPage>> GetAllNamespacePagesPaged(int pageNumber, string namespaceName, string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Returns a paged list of all active pages, with optional sorting and exact-match search term filtering.
        /// Unlike the search methods, this returns all pages and does not require a search term. Matching is exact with no score-based ranking.
        /// </summary>
        Task<List<TwPage>> GetAllPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);

        /// <summary>
        /// Returns a paged list of all deleted pages, with optional sorting and exact-match search term filtering.
        /// Unlike the search methods, this returns all pages and does not require a search term. Matching is exact with no score-based ranking.
        /// </summary>
        Task<List<TwPage>> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);

        /// <summary>
        /// Returns a paged list of all namespaces with aggregate statistics, with optional sorting.
        /// </summary>
        Task<List<TwNamespaceStat>> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Returns the names of all namespaces in the wiki.
        /// </summary>
        Task<List<string>> GetAllNamespaces();

        /// <summary>
        /// Returns all active pages in the wiki.
        /// </summary>
        Task<List<TwPage>> GetAllPages();

        /// <summary>
        /// Returns all pages that have the Template processing instruction applied.
        /// </summary>
        Task<List<TwPage>> GetAllTemplatePages();

        /// <summary>
        /// Returns all available feature templates.
        /// </summary>
        Task<List<TwFeatureTemplate>> GetAllFeatureTemplates();

        /// <summary>
        /// Replaces all processing instructions for the specified page.
        /// </summary>
        Task UpdatePageProcessingInstructions(int pageId, List<string> instructions);

        /// <summary>
        /// Returns the full page content for the specified page and optional revision number.
        /// </summary>
        Task<TwPage?> GetPageRevisionById(int pageId, int? revision = null);

        /// <summary>
        /// Saves the search tokens for a batch of pages.
        /// </summary>
        Task SavePageSearchTokens(List<TwPageToken> items);

        /// <summary>
        /// Permanently removes all page revisions. Requires a confirmation string to proceed.
        /// </summary>
        Task TruncateAllPageRevisions(string confirm);

        /// <summary>
        /// Returns the current revision number for the specified page.
        /// </summary>
        Task<int> GetCurrentPageRevision(int pageId);

        /// <summary>
        /// Returns the current revision number for the specified page using an existing database connection.
        /// </summary>
        Task<int> GetCurrentPageRevision(SqliteManagedInstance connection, int pageId);

        /// <summary>
        /// Returns limited page info (excluding full content) for the specified page and optional revision number.
        /// </summary>
        Task<TwPage?> GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null);

        /// <summary>
        /// Returns page metadata without content for the page with the specified navigation path.
        /// </summary>
        Task<TwPage?> GetPageInfoByNavigation(string navigation);

        /// <summary>
        /// Returns the total number of revisions for the specified page.
        /// </summary>
        Task<int> GetPageRevisionCountByPageId(int pageId);

        /// <summary>
        /// Restores a previously deleted page by its ID.
        /// </summary>
        Task RestoreDeletedPageByPageId(int pageId);

        /// <summary>
        /// Moves a specific page revision to the deleted revisions store.
        /// </summary>
        Task MovePageRevisionToDeletedById(int pageId, int revision, Guid userId);

        /// <summary>
        /// Moves a page and all its revisions to the deleted pages store.
        /// </summary>
        Task MovePageToDeletedById(int pageId, Guid userId);

        /// <summary>
        /// Permanently removes a deleted page by its ID.
        /// </summary>
        Task PurgeDeletedPageByPageId(int pageId);

        /// <summary>
        /// Permanently removes all deleted pages.
        /// </summary>
        Task PurgeDeletedPages();

        /// <summary>
        /// Returns the number of file attachments associated with the specified page.
        /// </summary>
        Task<int> GetCountOfPageAttachmentsById(int pageId);

        /// <summary>
        /// Returns a deleted page by its ID, or null if not found.
        /// </summary>
        Task<TwPage?> GetDeletedPageById(int pageId);

        /// <summary>
        /// Returns the most recent revision of the specified page, or null if not found.
        /// </summary>
        Task<TwPage?> GetLatestPageRevisionById(int pageId);

        /// <summary>
        /// Returns the revision number that follows the specified revision for the given page.
        /// </summary>
        Task<int> GetPageNextRevision(int pageId, int revision);

        /// <summary>
        /// Returns the revision number that precedes the specified revision for the given page.
        /// </summary>
        Task<int> GetPagePreviousRevision(int pageId, int revision);

        /// <summary>
        /// Returns a paged list of deleted revisions for the specified page.
        /// </summary>
        Task<List<TwDeletedPageRevision>> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber, string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Permanently removes all deleted page revisions across all pages.
        /// </summary>
        Task PurgeDeletedPageRevisions();

        /// <summary>
        /// Permanently removes all deleted page revisions for the specified page.
        /// </summary>
        Task PurgeDeletedPageRevisionsByPageId(int pageId);

        /// <summary>
        /// Permanently removes a specific deleted page revision by page ID and revision number.
        /// </summary>
        Task PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision);

        /// <summary>
        /// Restores a specific deleted page revision by page ID and revision number.
        /// </summary>
        Task RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision);

        /// <summary>
        /// Returns a specific deleted page revision by page ID and revision number, or null if not found.
        /// </summary>
        Task<TwDeletedPageRevision?> GetDeletedPageRevisionById(int pageId, int revision);

        /// <summary>
        /// Returns the page revision matching the specified namespace navigation and optional revision number.
        /// </summary>
        Task<TwPage?> GetPageRevisionByNavigation(TwNamespaceNavigation navigation, int? revision = null);

        /// <summary>
        /// Returns the page revision matching the specified navigation path and optional revision number, with optional cache refresh.
        /// </summary>
        Task<TwPage?> GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false);

        /// <summary>
        /// Returns all pages that share the specified tag.
        /// </summary>
        Task<List<TwTagAssociation>> GetAssociatedTags(string tag);

        /// <summary>
        /// Returns page metadata for all pages belonging to any of the specified namespaces.
        /// </summary>
        Task<List<TwPage>> GetPageInfoByNamespaces(List<string> namespaces);

        /// <summary>
        /// Returns page metadata for all pages that have any of the specified tags.
        /// </summary>
        Task<List<TwPage>> GetPageInfoByTags(IEnumerable<string> tags);

        /// <summary>
        /// Returns page metadata for all pages that have the specified tag.
        /// </summary>
        Task<List<TwPage>> GetPageInfoByTag(string tag);

        /// <summary>
        /// Replaces all tags associated with the specified page.
        /// </summary>
        Task UpdatePageTags(int pageId, List<string> tags);

        /// <summary>
        /// Inserts a new page if Page.Id == 0, otherwise updates the existing page. All metadata is written to the database.
        /// </summary>
        Task<int> UpsertPage(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null);

        /// <summary>
        /// Rebuilds the page and writes all aspects to the database.
        /// </summary>
        Task RefreshPageMetadata(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null);

        /// <summary>
        /// Parses and returns aggregated search tokens for all pages in the current engine state.
        /// </summary>
        Task<List<TwAggregatedSearchToken>> ParsePageTokens(ITwEngineState state);

        #region Page File.

        /// <summary>
        /// Detaches a file attachment from a specific page revision.
        /// </summary>
        Task DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision);

        /// <summary>
        /// Returns a paged list of file attachments that are no longer associated with any active page revision.
        /// </summary>
        Task<List<TwOrphanedPageAttachment>> GetOrphanedPageAttachmentsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);

        /// <summary>
        /// Permanently removes all orphaned page file attachments.
        /// </summary>
        Task PurgeOrphanedPageAttachments();

        /// <summary>
        /// Permanently removes a specific orphaned page file attachment by its ID and revision.
        /// </summary>
        Task PurgeOrphanedPageAttachment(int pageFileId, int revision);

        /// <summary>
        /// Returns a paged list of file attachment info records for the specified page navigation path and revision.
        /// </summary>
        Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null);

        /// <summary>
        /// Returns the file attachment info for a specific file on a specific page revision, or null if not found.
        /// </summary>
        Task<TwPageFileAttachmentInfo?> GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null);

        /// <summary>
        /// Returns the full file attachment including content, located by page navigation, file navigation, and optional file revision.
        /// </summary>
        Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null);

        /// <summary>
        /// Returns the full file attachment including content, located by page navigation, page revision, and file navigation.
        /// </summary>
        Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null);

        /// <summary>
        /// Returns a paged list of revision history entries for a specific file attachment on a page.
        /// </summary>
        Task<List<TwPageFileAttachmentInfo>> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber);

        /// <summary>
        /// Returns all file attachment info records associated with the specified page ID.
        /// </summary>
        Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageId(int pageId);

        /// <summary>
        /// Returns file revision info for a specific file on a page using an existing database connection.
        /// </summary>
        Task<TwPageFileRevisionAttachmentInfo?> GetPageFileInfoByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation);

        /// <summary>
        /// Returns the current revision attachment info for a specific file on a page using an existing database connection.
        /// </summary>
        Task<TwPageFileRevisionAttachmentInfo?> GetPageCurrentRevisionAttachmentByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation);

        /// <summary>
        /// Inserts or updates a page file attachment record. Associates the upload with the specified user.
        /// </summary>
        Task UpsertPageFile(TwPageFileAttachment item, Guid userId);

        #endregion
    }
}
