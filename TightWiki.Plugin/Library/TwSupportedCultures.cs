using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace TightWiki.Plugin.Library
{
    public class TwSupportedCultures
    {
        public IList<TwCultureInfoSettings> Collection { get; private set; }

        public IList<CultureInfo> AllCultures
            => Collection.Select(x => x.Culture).ToList();

        public IList<CultureInfo> UICompleteCultures
            => Collection.Select(x => x.Culture).ToList();

        public static CultureInfo DefaultCulture
            => new CultureInfo("en");

        public bool TryGetByCode(string code, [NotNullWhen(true)] out TwCultureInfoSettings? result)
        {
            result = Collection.FirstOrDefault(o => o.Code == code);
            return result != null;
        }

        public bool TryGetByName(string name, [NotNullWhen(true)] out TwCultureInfoSettings? result)
        {
            result = Collection.FirstOrDefault(o => o.Name == name);
            return result != null;
        }

        public TwSupportedCultures()
        {
            Collection = new List<TwCultureInfoSettings>
            {
                new TwCultureInfoSettings("en", "English"),
                new TwCultureInfoSettings("ar", "Arabic"),
                new TwCultureInfoSettings("az", "Azerbaijani") ,
                new TwCultureInfoSettings("be", "Belarusian") ,
                new TwCultureInfoSettings("bg", "Bulgarian") ,
                new TwCultureInfoSettings("cs", "Czech"),
                new TwCultureInfoSettings("da", "Danish") ,
                new TwCultureInfoSettings("de", "German"),
                new TwCultureInfoSettings("el", "Greek") ,
                new TwCultureInfoSettings("es", "Spanish"),
                new TwCultureInfoSettings("et", "Estonian") ,
                new TwCultureInfoSettings("fa", "Persian"),
                new TwCultureInfoSettings("fi", "Finnish") ,
                new TwCultureInfoSettings("fr", "French"),
                new TwCultureInfoSettings("he", "Hebrew") ,
                new TwCultureInfoSettings("hr", "Croatian") ,
                new TwCultureInfoSettings("hu", "Hungarian") ,
                new TwCultureInfoSettings("id", "Indonesian"),
                new TwCultureInfoSettings("is", "Icelandic") ,
                new TwCultureInfoSettings("it", "Italian"),
                new TwCultureInfoSettings("ja", "Japanese"),
                new TwCultureInfoSettings("ka", "Georgian") ,
                new TwCultureInfoSettings("kk", "Kazakh") ,
                new TwCultureInfoSettings("ko", "Korean"),
                new TwCultureInfoSettings("lt", "Lithuanian") ,
                new TwCultureInfoSettings("lv", "Latvian") ,
                new TwCultureInfoSettings("ms", "Malay") ,
                new TwCultureInfoSettings("nl", "Dutch"),
                new TwCultureInfoSettings("nn", "Norwegian"),
                new TwCultureInfoSettings("nb", "Bokmål"),
                new TwCultureInfoSettings("pl", "Polish"),
                new TwCultureInfoSettings("pt", "Portuguese"),
                new TwCultureInfoSettings("ro", "Romanian") ,
                new TwCultureInfoSettings("ru", "Russian"),
                new TwCultureInfoSettings("sk", "Slovak"),
                new TwCultureInfoSettings("sl", "Slovenian") ,
                new TwCultureInfoSettings("sr", "Serbian") ,
                new TwCultureInfoSettings("sv", "Swedish") ,
                new TwCultureInfoSettings("th", "Thai") ,
                new TwCultureInfoSettings("tr", "Turkish"),
                new TwCultureInfoSettings("uk", "Ukrainian"),
                new TwCultureInfoSettings("vi", "Vietnamese"),
                new TwCultureInfoSettings("zh-Hans", "Chinese simplified"),
                new TwCultureInfoSettings("zh-Hant", "Chinese traditional"),
                new TwCultureInfoSettings("bn", "Bengali"),
                new TwCultureInfoSettings("hi", "Hindi"),
                new TwCultureInfoSettings("ur", "Urdu")
            };
        }
    }
}
