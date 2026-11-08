using Microsoft.AspNetCore.Identity;
using ModelContextProtocol;
using ModelContextProtocol.Server;
using System.ComponentModel;
using TightWiki.Library.Api;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki
{
    /// <summary>
    /// The read-only MCP tools. They are thin wrappers over <see cref="ApiReadService"/>, so they return the same data
    /// as the /v1/ API and see only what the owner of the API token may read.
    /// </summary>
    [McpServerToolType]
    public class TwMcpReadTools(
            IHttpContextAccessor httpContextAccessor,
            ILogger<ITwEngine> logger,
            ITwEngine tightEngine,
            ITwSharedLocalizationText localizer,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            ITwDatabaseManager databaseManager)
    {
        [McpServerTool(Name = "search_pages"), Description("Searches the wiki pages and returns a page of the matching ones.")]
        public async Task<object> SearchPages(
            [Description("The text to search for.")] string query,
            [Description("The number of the result page, starting at 1.")] int page = 1)
        {
            return await Read(service => service.SearchPages(query, page));
        }

        [McpServerTool(Name = "get_page"), Description("Gets a wiki page, by default as the wiki markup it was saved with.")]
        public async Task<object> GetPage(
            [Description("The navigation of the page, for example \"Home\" or \"Namespace :: Page\".")] string navigation,
            [Description("The revision to get, the current revision if omitted.")] int? revision = null,
            [Description("Return the rendered plain text instead of the wiki markup.")] bool plainText = false)
        {
            var format = plainText ? ApiPageFormat.Text : ApiPageFormat.Markup;
            return await Read(service => service.GetPage(navigation, revision, format));
        }

        [McpServerTool(Name = "get_page_revisions"), Description("Lists the revisions of a wiki page, newest first.")]
        public async Task<object> GetPageRevisions(
            [Description("The navigation of the page.")] string navigation,
            [Description("The number of the result page, starting at 1.")] int page = 1)
        {
            return await Read(service => service.GetPageRevisions(navigation, page));
        }

        [McpServerTool(Name = "list_namespaces"), Description("Lists the namespaces in which at least one page can be read.")]
        public async Task<object> ListNamespaces()
        {
            return await Read(service => service.GetNamespaces());
        }

        [McpServerTool(Name = "list_namespace_pages"), Description("Lists the readable pages of a namespace.")]
        public async Task<object> ListNamespacePages(
            [Description("The name of the namespace.")] string name)
        {
            return await Read(service => service.GetNamespacePages(name));
        }

        [McpServerTool(Name = "list_tag_pages"), Description("Lists the readable pages that have a tag.")]
        public async Task<object> ListTagPages(
            [Description("The tag.")] string tag)
        {
            return await Read(service => service.GetTagPages(tag));
        }

        /// <summary>
        /// Runs the read for the user of the request and maps the outcome: the result is returned, null is
        /// "not found" and a missing permission is "forbidden". The SDK reports the thrown exception as a tool error.
        /// </summary>
        private async Task<object> Read<T>(Func<ApiReadService, Task<T>> read)
        {
            var httpContext = httpContextAccessor.HttpContext ?? throw new InvalidOperationException("There is no HTTP request.");
            var sessionState = await new TwSessionState().Hydrate(logger, signInManager, httpContext, wikiConfiguration, databaseManager);

            try
            {
                var result = await read(new ApiReadService(sessionState, tightEngine, localizer));
                return result ?? throw new McpException(Localize("Not found."));
            }
            catch (UnauthorizedAccessException)
            {
                throw new McpException(Localize("You do not have permission to read this."));
            }
            catch (Exception ex) when (ex is not McpException)
            {
                logger.LogError(ex, "Error in TwMcpReadTools while reading");
                throw;
            }
        }
    }
}
