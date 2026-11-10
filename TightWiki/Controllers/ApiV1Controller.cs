using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using NTDLS.Helpers;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Library.Api;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Controllers
{
    /// <summary>
    /// JSON API. Requests are authenticated only by a personal API token ("Authorization: Bearer [token]"),
    /// never by the sign-in cookie, so no antiforgery token is needed. Without a token the request is anonymous and may only read.
    /// Pages can be created and edited when "Allow API Writes" is enabled.
    /// Errors are returned as { "error": "..." } with the status code 400, 401, 403, 404, 409 or 500.
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
                    context.Result = Error(StatusCodes.Status400BadRequest, Localize("The request is not valid."));
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

        [HttpPost("pages")]
        public async Task<IActionResult> CreatePage([FromBody] ApiPageWriteRequest request)
        {
            var notAllowed = RejectWrite();
            if (notAllowed != null)
            {
                return notAllowed;
            }

            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return Error(StatusCodes.Status400BadRequest, Localize("The page name is required."));
            }

            try
            {
                var result = await CreateWriteService().CreatePage(request.Name, request.Body, request.Description, request.ChangeSummary);
                if (result.Error != PageSaveError.None)
                {
                    return SaveError(result);
                }

                return StatusCode(StatusCodes.Status201Created, ToSavedPage(result));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in ApiV1Controller while creating page {Name}", request.Name);
                return Error(StatusCodes.Status500InternalServerError, Localize("The request failed."));
            }
        }

        [HttpPut("pages/{givenCanonical}")]
        public async Task<IActionResult> UpdatePage(string givenCanonical, [FromBody] ApiPageWriteRequest request)
        {
            var notAllowed = RejectWrite();
            if (notAllowed != null)
            {
                return notAllowed;
            }

            if (request.Revision == null)
            {
                return Error(StatusCodes.Status400BadRequest, Localize("The revision that the edit is based on is required."));
            }

            if (request.Body == null)
            {
                return Error(StatusCodes.Status400BadRequest, Localize("The page body is required."));
            }

            try
            {
                var result = await CreateWriteService().UpdatePage(givenCanonical, request.Revision.Value, request.Body, request.Description, request.ChangeSummary);
                if (result == null)
                {
                    return Error(StatusCodes.Status404NotFound, Localize("Not found."));
                }

                if (result.Error != PageSaveError.None)
                {
                    return SaveError(result);
                }

                return Ok(ToSavedPage(result));
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in ApiV1Controller while updating page {Navigation}", givenCanonical);
                return Error(StatusCodes.Status500InternalServerError, Localize("The request failed."));
            }
        }

        private ApiWriteService CreateWriteService() => new(SessionState, tightEngine, Localizer, ApiWriteService.ApiSource);

        /// <summary>
        /// Returns the error when the request may not write: 403 when writes are disabled (the endpoint exists, so it is not 404),
        /// 401 when the request has no token (an anonymous session has no profile to be the author).
        /// </summary>
        private ObjectResult? RejectWrite()
        {
            if (!WikiConfiguration.AllowApiWrites)
            {
                return Error(StatusCodes.Status403Forbidden, Localize("Writing through the API is not enabled."));
            }

            if (!SessionState.IsAuthenticated)
            {
                Response.Headers.WWWAuthenticate = "Bearer";
                return Error(StatusCodes.Status401Unauthorized, Localize("An API token is required to write."));
            }

            return null;
        }

        /// <summary>
        /// Maps a failed save: a missing permission is 403, a revision conflict is 409 with the current revision,
        /// an invalid input is 400. A page name that already exists is 409, because it is a conflict with the current state.
        /// </summary>
        private ObjectResult SaveError(PageSaveResult result)
        {
            switch (result.Error)
            {
                case PageSaveError.ChangeSummaryRequired:
                    return Error(StatusCodes.Status400BadRequest, Localize("A change summary is required for page edits."));
                case PageSaveError.NameContainsUnsafeCharacters:
                    return Error(StatusCodes.Status400BadRequest, Localize("The page name contains characters which are disallowed: {0}.",
                        string.Join(' ', Utility.UnsafePageNameCharacters)));
                case PageSaveError.NameContainsMultipleNamespaceSeparators:
                    return Error(StatusCodes.Status400BadRequest, Localize("The characters '::' are used to denote a namespace name. A page name cannot contain more than one set of these characters."));
                case PageSaveError.CreateNotPermitted:
                    return Error(StatusCodes.Status403Forbidden, Localize("You do not have permission to perform the action: {0}", Localize(TwPermission.Create.ToString())));
                case PageSaveError.EditNotPermitted:
                    return Error(StatusCodes.Status403Forbidden, Localize("You do not have permission to perform the action: {0}", Localize(TwPermission.Edit.ToString())));
                case PageSaveError.PageProtected:
                    return Error(StatusCodes.Status403Forbidden, Localize("The page is protected and cannot be modified except by a moderator or an administrator unless the protection is removed."));
                case PageSaveError.NameAlreadyExists:
                    return Error(StatusCodes.Status409Conflict, Localize("The page name you entered already exists."));
                case PageSaveError.RevisionConflict:
                    return StatusCode(StatusCodes.Status409Conflict, new
                    {
                        error = Localize("This page was modified by someone else while you were editing it. Please reload the page to get the latest version before saving."),
                        currentRevision = result.Page.EnsureNotNull().Revision
                    });
                default:
                    return Error(StatusCodes.Status500InternalServerError, Localize("The request failed."));
            }
        }

        private static object ToSavedPage(PageSaveResult result)
        {
            var page = result.Page.EnsureNotNull();
            return new { name = page.Name, navigation = page.Navigation, revision = page.Revision };
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
