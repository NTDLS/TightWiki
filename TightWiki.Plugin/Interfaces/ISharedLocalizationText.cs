using Microsoft.Extensions.Localization;

namespace TightWiki.Plugin.Interfaces
{
    public interface ISharedLocalizationText
    {
        LocalizedString this[string key] { get; }
        LocalizedString this[string key, params object[] arguments] { get; }

        string Get(string key);
        string Format(string key, params object?[] arguments);
    }
}
