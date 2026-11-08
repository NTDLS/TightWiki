using Microsoft.AspNetCore.Identity;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using NTDLS.Helpers;
using System.ComponentModel;
using TightWiki.Library;
using TightWiki.Library.Api;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki
{
    /// <summary>
    /// The MCP tools that write. They are thin wrappers over <see cref="ApiWriteService"/>, so they follow the same rules as
    /// the writes of the /v1/ API: "Allow API Writes" must be enabled, an API token is required, a change summary is required
    /// and the revisions are marked with "[MCP] ". The tools are always listed, when writing is not enabled they return an error.
    /// </summary>
    [McpServerToolType]
    public class TwMcpWriteTools(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ITwEngine> logger,
            ITwEngine tightEngine,
            ITwSharedLocalizationText localizer,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            ITwDatabaseManager databaseManager)
    {
        [McpServerTool(Name = "create_page"), Description("Creates a wiki page. A change summary is required. Use get_wiki_syntax_help for the markup syntax.")]
        public async Task<object> CreatePage(
            [Description("The name of the new page, for example \"Page\" or \"Namespace :: Page\".")] string name,
            [Description("The body of the page, as wiki markup.")] string body,
            [Description("A short summary of what has been changed. Required.")] string changeSummary,
            [Description("The description of the page.")] string? description = null)
        {
            return await Save(service =>
            {
                if (string.IsNullOrWhiteSpace(name))
                {
                    throw new McpException(Localize("The page name is required."));
                }

                return service.CreatePage(name, body, description, changeSummary);
            });
        }

        [McpServerTool(Name = "update_page"), Description("Saves a new revision of a wiki page, the body and the description are replaced. A change summary is required. The revision is the one from get_page that the edit is based on, if the page has been changed since, the tool fails. Use get_wiki_syntax_help for the markup syntax.")]
        public async Task<object> UpdatePage(
            [Description("The navigation of the page.")] string navigation,
            [Description("The revision of the page, from get_page, that the new body is based on.")] int revision,
            [Description("The new body of the page, as wiki markup.")] string body,
            [Description("A short summary of what has been changed. Required.")] string changeSummary,
            [Description("The new description of the page, empty when omitted.")] string? description = null)
        {
            return await Save(service => service.UpdatePage(navigation, revision, body, description, changeSummary));
        }

        /// <summary>
        /// Runs the save for the user of the request and maps the outcome: the saved page is returned, null is "not found"
        /// and a failed save is thrown as the error of the tool. The SDK reports the thrown exception as a tool error.
        /// </summary>
        private async Task<object> Save(Func<ApiWriteService, Task<PageSaveResult?>> save)
        {
            if (!wikiConfiguration.AllowApiWrites)
            {
                throw new McpException(Localize("Writing through the API is not enabled."));
            }

            var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("There is no HTTP request.");
            var sessionState = await new TwSessionState().Hydrate(logger, signInManager, httpContext, wikiConfiguration, databaseManager);
            if (!sessionState.IsAuthenticated)
            {
                throw new McpException(Localize("An API token is required to write."));
            }

            try
            {
                var result = await save(new ApiWriteService(sessionState, tightEngine, localizer, ApiWriteService.McpSource));
                if (result == null)
                {
                    throw new McpException(Localize("Not found."));
                }

                if (result.Error != PageSaveError.None)
                {
                    throw new McpException(SaveErrorMessage(result));
                }

                var page = result.Page.EnsureNotNull();
                return new { name = page.Name, navigation = page.Navigation, revision = page.Revision };
            }
            catch (Exception ex) when (ex is not McpException)
            {
                logger.LogError(ex, "Error in TwMcpWriteTools while writing");
                throw;
            }
        }

        /// <summary>
        /// The same texts as the /v1/ API, a revision conflict also tells the current revision.
        /// </summary>
        private string SaveErrorMessage(PageSaveResult result)
        {
            switch (result.Error)
            {
                case PageSaveError.ChangeSummaryRequired:
                    return Localize("A change summary is required for page edits.");
                case PageSaveError.NameContainsUnsafeCharacters:
                    return Localize("The page name contains characters which are disallowed: {0}.", string.Join(' ', Utility.UnsafePageNameCharacters));
                case PageSaveError.NameContainsMultipleNamespaceSeparators:
                    return Localize("The characters '::' are used to denote a namespace name. A page name cannot contain more than one set of these characters.");
                case PageSaveError.CreateNotPermitted:
                    return Localize("You do not have permission to perform the action: {0}", Localize(TwPermission.Create.ToString()));
                case PageSaveError.EditNotPermitted:
                    return Localize("You do not have permission to perform the action: {0}", Localize(TwPermission.Edit.ToString()));
                case PageSaveError.PageProtected:
                    return Localize("The page is protected and cannot be modified except by a moderator or an administrator unless the protection is removed.");
                case PageSaveError.NameAlreadyExists:
                    return Localize("The page name you entered already exists.");
                case PageSaveError.RevisionConflict:
                    return Localize("The page was changed since the revision you based the edit on. The current revision is {0}.", result.Page.EnsureNotNull().Revision);
                default:
                    return Localize("The request failed.");
            }
        }
    }
}
