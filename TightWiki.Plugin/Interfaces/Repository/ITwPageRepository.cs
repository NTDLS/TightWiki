using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    ///  Data access for pages, page revisions, page comments, page processing instructions, page tags, page references, and related data.
    /// </summary>
    public interface ITwPageRepository
    {
        SqliteManagedFactory PagesFactory { get; }
        SqliteManagedFactory DeletedPagesFactory { get; }
        SqliteManagedFactory DeletedPageRevisionsFactory { get; }

        Task<List<TwPage>> AutoCompletePage(string? searchText);
        Task<List<string>> AutoCompleteNamespace(string? searchText);
        Task<TwPage?> GetPageRevisionInfoById(int pageId, int? revision = null);
        Task<TwProcessingInstructionCollection> GetPageProcessingInstructionsByPageId(int pageId);
        Task<List<TwPageTag>> GetPageTagsById(int pageId);
        Task<List<TwPageRevision>> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        Task<List<TwPageRevision>> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount);
        Task<string?> GetPageNavigationByPageId(int pageId);
        Task<List<TwPage>> GetTopRecentlyModifiedPagesInfo(int topCount);
        Task<List<TwPage>> GetTopRecentlyCreatedPagesInfo(int topCount);
        Task<List<TwPage>> GetTopViewedPagesInfo(int topCount);
        Task<List<TwPage>> GetTopEditedPagesInfo(int topCount);
        Task<List<TwPage>> PageSearch(List<string> searchTerms);
        Task<List<TwPage>> PageSearchPaged(List<string> searchTerms, int pageNumber, int? pageSize = null, bool? allowFuzzyMatching = null);
        Task<List<TwRelatedPage>> GetSimilarPagesPaged(int pageId, int similarity, int pageNumber, int? pageSize = null);
        Task<List<TwRelatedPage>> GetRelatedPagesPaged(int pageId, int pageNumber, int? pageSize = null);
        Task FlushPageCache(int pageId);
        Task InsertPageComment(int pageId, Guid userId, string body);
        Task DeletePageCommentById(int pageId, int commentId);
        Task DeletePageCommentByUserAndId(int pageId, Guid userId, int commentId);
        Task<int> GetTotalPageCommentCount(int pageId);
        Task<List<TwPageComment>> GetPageCommentsPaged(string navigation, int pageNumber);
        Task<List<TwNonexistentPage>> GetMissingPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        Task UpdateSinglePageReference(string pageNavigation, int pageId);
        Task UpdatePageReferences(int pageId, List<TwPageReference> referencesPageNavigations);
        Task<List<TwPage>> GetAllPagesByInstructionPaged(int pageNumber, string? instruction = null);
        Task<List<int>> GetDeletedPageIdsByTokens(List<string>? tokens);
        Task<List<int>> GetPageIdsByTokens(List<string>? tokens);
        Task<List<TwPage>> GetAllNamespacePagesPaged(int pageNumber, string namespaceName, string? orderBy = null, string? orderByDirection = null);
        /// <summary>
        /// Unlike the search, this method returns all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        Task<List<TwPage>> GetAllPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);
        /// <summary>
        /// Unlike the search, this method returns all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        Task<List<TwPage>> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);
        Task<List<TwNamespaceStat>> GetAllNamespacesPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        Task<List<string>> GetAllNamespaces();
        Task<List<TwPage>> GetAllPages();
        Task<List<TwPage>> GetAllTemplatePages();
        Task<List<TwFeatureTemplate>> GetAllFeatureTemplates();
        Task UpdatePageProcessingInstructions(int pageId, List<string> instructions);
        Task<TwPage?> GetPageRevisionById(int pageId, int? revision = null);
        Task SavePageSearchTokens(List<TwPageToken> items);
        Task TruncateAllPageRevisions(string confirm);
        Task<int> GetCurrentPageRevision(int pageId);
        Task<int> GetCurrentPageRevision(SqliteManagedInstance connection, int pageId);
        Task<TwPage?> GetLimitedPageInfoByIdAndRevision(int pageId, int? revision = null);
        /// <summary>
        /// Gets the page info without the content.
        /// </summary>
        Task<TwPage?> GetPageInfoByNavigation(string navigation);
        Task<int> GetPageRevisionCountByPageId(int pageId);
        Task RestoreDeletedPageByPageId(int pageId);
        Task MovePageRevisionToDeletedById(int pageId, int revision, Guid userId);
        Task MovePageToDeletedById(int pageId, Guid userId);
        Task PurgeDeletedPageByPageId(int pageId);
        Task PurgeDeletedPages();
        Task<int> GetCountOfPageAttachmentsById(int pageId);
        Task<TwPage?> GetDeletedPageById(int pageId);
        Task<TwPage?> GetLatestPageRevisionById(int pageId);
        Task<int> GetPageNextRevision(int pageId, int revision);
        Task<int> GetPagePreviousRevision(int pageId, int revision);
        Task<List<TwDeletedPageRevision>> GetDeletedPageRevisionsByIdPaged(int pageId, int pageNumber, string? orderBy = null, string? orderByDirection = null);
        Task PurgeDeletedPageRevisions();
        Task PurgeDeletedPageRevisionsByPageId(int pageId);
        Task PurgeDeletedPageRevisionByPageIdAndRevision(int pageId, int revision);
        Task RestoreDeletedPageRevisionByPageIdAndRevision(int pageId, int revision);
        Task<TwDeletedPageRevision?> GetDeletedPageRevisionById(int pageId, int revision);
        Task<TwPage?> GetPageRevisionByNavigation(TwNamespaceNavigation navigation, int? revision = null);
        Task<TwPage?> GetPageRevisionByNavigation(string givenNavigation, int? revision = null, bool refreshCache = false);
        Task<List<TwTagAssociation>> GetAssociatedTags(string tag);
        Task<List<TwPage>> GetPageInfoByNamespaces(List<string> namespaces);
        Task<List<TwPage>> GetPageInfoByTags(IEnumerable<string> tags);
        Task<List<TwPage>> GetPageInfoByTag(string tag);
        Task UpdatePageTags(int pageId, List<string> tags);

        /// <summary>
        /// Inserts a new page if Page.Id == 0, other wise updates the page. All metadata is written to the database.
        /// </summary>
        Task<int> UpsertPage(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null);

        /// <summary>
        /// Rebuilds the page and writes all aspects to the database.
        /// </summary>
        Task RefreshPageMetadata(ITwEngine wikifier, ITwSharedLocalizationText localizer, TwPage page, ITwSessionState? sessionState = null);

        Task<List<TwAggregatedSearchToken>> ParsePageTokens(ITwEngineState state);

        #region Page File.

        Task DetachPageRevisionAttachment(string pageNavigation, string fileNavigation, int pageRevision);
        Task<List<TwOrphanedPageAttachment>> GetOrphanedPageAttachmentsPaged(int pageNumber, string? orderBy = null, string? orderByDirection = null);
        Task PurgeOrphanedPageAttachments();
        Task PurgeOrphanedPageAttachment(int pageFileId, int revision);
        Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageNavigationAndPageRevisionPaged(string pageNavigation, int pageNumber, int? pageSize = null, int? pageRevision = null);
        Task<TwPageFileAttachmentInfo?> GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null);
        Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationFileRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? fileRevision = null);
        Task<TwPageFileAttachment?> GetPageFileAttachmentByPageNavigationPageRevisionAndFileNavigation(string pageNavigation, string fileNavigation, int? pageRevision = null);
        Task<List<TwPageFileAttachmentInfo>> GetPageFileAttachmentRevisionsByPageAndFileNavigationPaged(string pageNavigation, string fileNavigation, int pageNumber);
        Task<List<TwPageFileAttachmentInfo>> GetPageFilesInfoByPageId(int pageId);
        Task<TwPageFileRevisionAttachmentInfo?> GetPageFileInfoByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation);
        Task<TwPageFileRevisionAttachmentInfo?> GetPageCurrentRevisionAttachmentByFileNavigation(SqliteManagedInstance connection, int pageId, string fileNavigation);
        Task UpsertPageFile(TwPageFileAttachment item, Guid userId);

        #endregion
    }
}
