using Microsoft.AspNetCore.Http;
using System.Globalization;
using TightWiki.Library.Interfaces;

namespace TightWiki.Library
{
    public class DummySessionState
        : ISessionState
    {
        public IQueryCollection? QueryString { get; set; }

        public IAccountProfile? Profile { get; set; }

        public Task<bool> HoldsPermission(WikiPermission[] permissions) => Task.FromResult(true);

        public Task<bool> HoldsPermission(WikiPermission permission) => Task.FromResult(true);

        public Task<bool> HoldsPermission(string? givenCanonical, WikiPermission permission) => Task.FromResult(true);

        public Task<bool> HoldsPermission(string? givenCanonical, WikiPermission[] permissions) => Task.FromResult(true);

        public Task RequireAuthorizedPermission() => Task.CompletedTask;

        public Task RequirePermission(string? givenCanonical, WikiPermission[] permissions) => Task.CompletedTask;

        public Task RequirePermission(string? givenCanonical, WikiPermission permission) => Task.CompletedTask;

        public Task RequireAdminPermission() => Task.CompletedTask;

        public DateTime LocalizeDateTime(DateTime datetime)
            => TimeZoneInfo.ConvertTimeFromUtc(datetime, GetPreferredTimeZone());

        public TimeZoneInfo GetPreferredTimeZone()
            => TimeZoneInfo.Local;
    }
}
