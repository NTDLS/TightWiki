using Microsoft.AspNetCore.Http;
using TightWiki.Library.Interfaces;
using TightWiki.Models;
using static TightWiki.Library.Constants;

namespace TightWiki.Repository
{
    public class DummySessionState
        : ISessionState
    {
        public IQueryCollection? QueryString { get; set; }

        public IAccountProfile? Profile { get; set; }

        public bool HoldsPermission(WikiPermission[] permissions) => true;

        public bool HoldsPermission(WikiPermission permission) => true;

        public bool HoldsPermission(string? givenCanonical, WikiPermission permission) => true;

        public bool HoldsPermission(string? givenCanonical, WikiPermission[] permissions) => true;

        public void RequireAuthorizedPermission() { }

        public void RequirePermission(string? givenCanonical, WikiPermission[] permissions) { }

        public void RequirePermission(string? givenCanonical, WikiPermission permission) { }

        public void RequireAdminPermission() { }

        public DateTime LocalizeDateTime(DateTime datetime)
            => TimeZoneInfo.ConvertTimeFromUtc(datetime, GetPreferredTimeZone());

        public TimeZoneInfo GetPreferredTimeZone()
            => TimeZoneInfo.FindSystemTimeZoneById(GlobalConfiguration.DefaultTimeZone);
    }
}
