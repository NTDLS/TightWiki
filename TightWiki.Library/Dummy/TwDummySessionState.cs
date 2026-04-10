using Microsoft.AspNetCore.Http;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Library.Dummy
{
    public class TwDummySessionState
        : ITwSessionState
    {
        public IQueryCollection? QueryString { get; set; }

        public ITwAccountProfile? Profile { get; set; }

        public Task<bool> HoldsPermission(TwPermission[] permissions) => Task.FromResult(true);

        public Task<bool> HoldsPermission(TwPermission permission) => Task.FromResult(true);

        public Task<bool> HoldsPermission(string? givenCanonical, TwPermission permission) => Task.FromResult(true);

        public Task<bool> HoldsPermission(string? givenCanonical, TwPermission[] permissions) => Task.FromResult(true);

        public Task RequireAuthorizedPermission() => Task.CompletedTask;

        public Task RequirePermission(string? givenCanonical, TwPermission[] permissions) => Task.CompletedTask;

        public Task RequirePermission(string? givenCanonical, TwPermission permission) => Task.CompletedTask;

        public Task RequireAdminPermission() => Task.CompletedTask;

        public DateTime LocalizeDateTime(DateTime datetime)
            => TimeZoneInfo.ConvertTimeFromUtc(datetime, GetPreferredTimeZone());

        public TimeZoneInfo GetPreferredTimeZone()
            => TimeZoneInfo.Utc;
    }
}
