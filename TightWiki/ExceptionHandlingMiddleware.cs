using System.Text;
using TightWiki.Exceptions;
using TightWiki.Models;
using TightWiki.Repository;

namespace TightWiki
{
    /// <summary>
    /// Intercepts exceptions so that we can throw "UnauthorizedException" from controllers to simplify permissions.
    /// </summary>
    public class ExceptionHandlingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ExceptionHandlingMiddleware> _logger;

        public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (UnauthorizedException ex)
            {
                _logger.LogError(ex, ex.Message);
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(string.Empty);
            }
            catch (Exception ex)
            {
                string request = $"{context.Request.Path}{context.Request.QueryString}";
                var routeValues = new StringBuilder();

                foreach (var rv in context.Request.RouteValues)
                {
                    routeValues.AppendLine($"{rv},");
                }
                if (routeValues.Length > 1) routeValues.Length--; //Trim trailing comma.

                var exceptionText = $"IP Address: {context.Connection.RemoteIpAddress},\r\n Request: {request},\r\n RouteValues: {routeValues}\r\n";

                _logger.LogError(ex, exceptionText);
                ExceptionRepository.InsertException(ex, exceptionText);

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";

                context.Response.Redirect($"{GlobalConfiguration.BasePath}/Utility/Notify?NotifyErrorMessage={Uri.EscapeDataString("An unexpected error has occurred. The details of this exception have been logged.")}");
            }
        }
    }
}
