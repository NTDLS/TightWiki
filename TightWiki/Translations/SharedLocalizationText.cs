using Microsoft.Extensions.Localization;
using NTDLS.Localization;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Translations
{
    /// <summary>
    /// Exposes the NTDLS.Localization translations (Translations.db) to plugins and the engine.
    /// The language is that of the current UI culture, which the request localization middleware sets for each request.
    /// </summary>
    public class SharedLocalizationText
        : ITwSharedLocalizationText
    {
        public LocalizedString this[string key]
            => new(key, Localizer.Default.Localize(key));

        public LocalizedString this[string key, params object[] arguments]
            => new(key, Localizer.Default.Localize(key, arguments));

        public string Get(string key)
            => Localizer.Default.Localize(key);

        public string Format(string key, params object?[] arguments)
            => Localizer.Default.Localize(key, arguments.Select(o => o ?? string.Empty).ToArray());
    }
}
