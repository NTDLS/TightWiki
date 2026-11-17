using NTDLS.Helpers;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Library.Api
{
    /// <summary>
    /// Creates and edits pages for the API and MCP on top of <see cref="PageSaveService"/>, so the checks are the ones of the
    /// web UI. A change summary is always required, and it is prefixed with the source ("[API] ", "[MCP] ") so that the
    /// revisions made this way can be told apart in the page history. A page cannot be renamed.
    /// When a page is saved, <see cref="PageSaveResult.Page"/> is the saved page with its new revision.
    /// </summary>
    public class ApiWriteService(ITwSessionState session, ITwEngine engine, ITwSharedLocalizationText localizer, string source)
    {
        public const string ApiSource = "API";
        public const string McpSource = "MCP";

        private ITwPageRepository PageRepository => engine.DatabaseManager.PageRepository;

        public async Task<PageSaveResult> CreatePage(string name, string? body, string? description, string? changeSummary)
        {
            return await Save(0, 0, name, body, description, changeSummary);
        }

        /// <summary>
        /// Saves a new revision of the page, the body is replaced and the description is kept when it is null.
        /// Returns null when the page does not exist.
        /// When the result is <see cref="PageSaveError.RevisionConflict"/>, <see cref="PageSaveResult.Page"/> is the current revision.
        /// </summary>
        public async Task<PageSaveResult?> UpdatePage(string givenCanonical, int expectedRevision, string? body, string? description, string? changeSummary)
        {
            if (Utility.CountOccurrencesOf(givenCanonical, "::") > 1)
            {
                return new PageSaveResult() { Error = PageSaveError.NameContainsMultipleNamespaceSeparators };
            }

            var navigation = TwNamespaceNavigation.CleanAndValidate(givenCanonical);
            if (await session.HoldsPermission(navigation, TwPermission.Edit) == false)
            {
                return new PageSaveResult() { Error = PageSaveError.EditNotPermitted };
            }

            var existingPage = await PageRepository.GetPageRevisionByNavigation(navigation);
            if (existingPage == null)
            {
                return null;
            }

            var result = await Save(existingPage.Id, expectedRevision, existingPage.Name, body, description ?? existingPage.Description, changeSummary);
            if (result.Error == PageSaveError.RevisionConflict)
            {
                result.Page = await PageRepository.GetPageRevisionById(existingPage.Id);
            }

            return result;
        }

        private async Task<PageSaveResult> Save(int pageId, int expectedRevision, string name, string? body, string? description, string? changeSummary)
        {
            if (string.IsNullOrWhiteSpace(changeSummary))
            {
                return new PageSaveResult() { Error = PageSaveError.ChangeSummaryRequired };
            }

            var saveService = new PageSaveService(session, engine, localizer);
            var result = await saveService.SavePage(pageId, expectedRevision, name, body, description, $"[{source}] {changeSummary.Trim()}");
            if (result.Error == PageSaveError.None)
            {
                result.Page = (await PageRepository.GetPageRevisionById(result.Page.EnsureNotNull().Id)).EnsureNotNull();
            }

            return result;
        }
    }
}
