using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Library;
using TightWiki.Plugin.Models;

namespace TightWiki.Plugin.Interfaces.Repository
{
    public interface IPageRepository
    {
        Task<List<TwPage>> AutoCompletePage(string? searchText);
        Task<List<string>> AutoCompleteNamespace(string? searchText);
        Task<TwPage?> GetPageRevisionInfoById(int pageId, int? revision = null);
        Task<TwProcessingInstructionCollection> GetPageProcessingInstructionsByPageId(int pageId);
        Task<List<TwPageTag>> GetPageTagsById(int pageId);
        Task<List<TwPageRevision>> GetPageRevisionsInfoByNavigationPaged(string navigation, int pageNumber, string? orderBy = null, string? orderByDirection = null, int? pageSize = null);
        Task<List<TwPageRevision>> GetTopRecentlyModifiedPagesInfoByUserId(Guid userId, int topCount);
        Task<string?> GetPageNavigationByPageId(int pageId);
        Task<List<TwPage>> GetTopRecentlyModifiedPagesInfo(int topCount);
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
        Task<List<TwPage>> GetAllPagesPaged(int pageNumber,string? orderBy = null, string? orderByDirection = null, List<string>? searchTerms = null);
        /// <summary>
        /// Unlike the search, this method returns all pages and allows them to be paired down using the search terms.
        /// Whereas the search requires a search term to get results. The matching here is also exact, no score based matching.
        /// </summary>
        Task<List<TwPage>> GetAllDeletedPagesPaged(int pageNumber, string? orderBy = null,string? orderByDirection = null, List<string>? searchTerms = null);
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
    }
}
