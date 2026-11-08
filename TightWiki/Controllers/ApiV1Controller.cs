using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using System.Security.Claims;
using TightWiki.Library.Api;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Controllers
{
    /// <summary>
    /// Read-only JSON API. Requests are authenticated only by a personal API token ("Authorization: Bearer [token]"),
    /// never by the sign-in cookie. Without a token the request is anonymous.
    /// Errors are returned as { "error": "..." } with the status code 401, 403, 404 or 500.
    /// </summary>
    [Route("v1")]
    [Produces("application/json")]
    public class ApiV1Controller(
            ILogger<ITwEngine> logger,
            ITwEngine tightEngine,
            ITwSharedLocalizationText localizer,
            SignInManager<IdentityUser> signInManager,
            TwConfiguration wikiConfiguration,
            UserManager<IdentityUser> userManager,
            ITwDatabaseManager databaseManager
        )
        : TwController<ApiV1Controller>(logger, signInManager, userManager, localizer, wikiConfiguration, databaseManager)
    {
        private ApiReadService CreateReadService() => new(SessionState, tightEngine, Localizer);

        [NonAction]
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            if (!WikiConfiguration.EnableApi)
            {
                context.Result = Error(StatusCodes.Status404NotFound, Localize("Not found."));
                return;
            }

            // Only the authentication and the session are guarded here, so that a failure is answered with JSON
            // instead of the redirect of the exception middleware. The action itself runs outside of the try
            // and handles its own exceptions in Respond.
            try
            {
                // The default authentication is the sign-in cookie, which must not be accepted here.
                var authentication = await HttpContext.AuthenticateAsync(TwApiTokenAuthenticationHandler.SchemeName);
                if (authentication.Failure != null)
                {
                    Response.Headers.WWWAuthenticate = "Bearer";
                    context.Result = Error(StatusCodes.Status401Unauthorized, Localize("The API token is not valid."));
                    return;
                }

                HttpContext.User = authentication.Principal ?? new ClaimsPrincipal(new ClaimsIdentity());

                if (!ModelState.IsValid)
                {
                    context.Result = Error(StatusCodes.Status400BadRequest, Localize("A query parameter is not valid."));
                    return;
                }

                await SessionState.Hydrate(Logger, SignInManager, this, WikiConfiguration, DatabaseManager);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in ApiV1Controller while authenticating the request");
                context.Result = Error(StatusCodes.Status500InternalServerError, Localize("The request failed."));
                return;
            }

            await next();
        }

        [HttpGet("search")]
        public async Task<IActionResult> Search([FromQuery] string? q, [FromQuery] int page = 1)
        {
            return await Respond(async () => await CreateReadService().SearchPages(q, page), "searching pages");
        }

        [HttpGet("pages/{givenCanonical}")]
        public async Task<IActionResult> GetPage(string givenCanonical, [FromQuery] int? revision, [FromQuery] ApiPageFormat format = ApiPageFormat.Markup)
        {
            return await Respond(async () => await CreateReadService().GetPage(givenCanonical, revision, format), "reading page " + givenCanonical);
        }

        [HttpGet("pages/{givenCanonical}/revisions")]
        public async Task<IActionResult> GetPageRevisions(string givenCanonical, [FromQuery] int page = 1)
        {
            return await Respond(async () => await CreateReadService().GetPageRevisions(givenCanonical, page), "reading revisions of page " + givenCanonical);
        }

        [HttpGet("namespaces")]
        public async Task<IActionResult> GetNamespaces()
        {
            return await Respond(async () => await CreateReadService().GetNamespaces(), "listing namespaces");
        }

        [HttpGet("namespaces/{name}/pages")]
        public async Task<IActionResult> GetNamespacePages(string name)
        {
            return await Respond(async () => await CreateReadService().GetNamespacePages(name), "listing pages of namespace " + name);
        }

        [HttpGet("tags/{tag}/pages")]
        public async Task<IActionResult> GetTagPages(string tag)
        {
            return await Respond(async () => await CreateReadService().GetTagPages(tag), "listing pages of tag " + tag);
        }

        /// <summary>
        /// Runs the read and maps the outcome: the result is 200, null is 404, a missing permission is 403.
        /// </summary>
        private async Task<IActionResult> Respond(Func<Task<object?>> read, string description)
        {
            try
            {
                var result = await read();
                if (result == null)
                {
                    return Error(StatusCodes.Status404NotFound, Localize("Not found."));
                }

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Error(StatusCodes.Status403Forbidden, Localize("You do not have permission to read this."));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in ApiV1Controller while {Description}", description);
                return Error(StatusCodes.Status500InternalServerError, Localize("The request failed."));
            }
        }

        private ObjectResult Error(int statusCode, string message)
        {
            return StatusCode(statusCode, new { error = message });
        }
    }
}
