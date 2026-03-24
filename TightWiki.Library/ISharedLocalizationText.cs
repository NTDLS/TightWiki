namespace TightWiki.Library
{
    using Microsoft.Extensions.Localization;

    public interface ISharedLocalizationText
    {
        LocalizedString this[string key] { get; }
        LocalizedString this[string key, params object[] arguments] { get; }

        string Get(string key);
        string Format(string key, params object?[] arguments);
    }
}
