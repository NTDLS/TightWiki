using Microsoft.Extensions.Localization;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Translations
{
    public class SharedLocalizationText
        : ISharedLocalizationText
    {
        private readonly IStringLocalizer<SharedLocalizer> _localizer;

        public SharedLocalizationText(IStringLocalizer<SharedLocalizer> localizer)
        {
            _localizer = localizer;
        }

        public LocalizedString this[string key] => _localizer[key];

        public LocalizedString this[string key, params object[] arguments]
            => _localizer[key, arguments];

        public string Get(string key)
            => _localizer[key].Value;

        public string Format(string key, params object?[] arguments)
            => _localizer[key, arguments.Select(o => o ?? string.Empty).ToArray()].Value;
    }
}
