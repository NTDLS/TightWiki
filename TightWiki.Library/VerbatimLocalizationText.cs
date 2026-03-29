using Microsoft.Extensions.Localization;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Library
{
    public class VerbatimLocalizationText
        : ISharedLocalizationText
    {
        public LocalizedString this[string key]
            => new LocalizedString(key, key);

        public LocalizedString this[string key, params object[] arguments]
            => new LocalizedString(key, string.Format(key, arguments));

        public string Get(string key)
            => key;

        public string Format(string key, params object?[] arguments)
            => string.Format(key, arguments.Select(o => o ?? string.Empty).ToArray());
    }
}
