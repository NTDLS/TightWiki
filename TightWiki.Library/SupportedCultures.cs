using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace TightWiki.Library
{
    public class SupportedCultures
    {
        public IList<CultureInfoSettings> Collection { get; private set; }

        public IList<CultureInfo> AllCultures
            => Collection.Select(x => x.Culture).ToList();

        public IList<CultureInfo> UICompleteCultures
            => Collection.Select(x => x.Culture).ToList();

        public static CultureInfo DefaultCulture
            => new CultureInfo("en");

        public bool TryGetByCode(string code, [NotNullWhen(true)] out CultureInfoSettings? result)
        {
            result = Collection.FirstOrDefault(o => o.Code == code);
            return result != null;
        }

        public bool TryGetByName(string name, [NotNullWhen(true)] out CultureInfoSettings? result)
        {
            result = Collection.FirstOrDefault(o => o.Name == name);
            return result != null;
        }

        public SupportedCultures()
        {
            Collection = new List<CultureInfoSettings>
            {
                new CultureInfoSettings("en", "English"),
                new CultureInfoSettings("ar", "Arabic"),
                new CultureInfoSettings("az", "Azerbaijani") ,
                new CultureInfoSettings("be", "Belarusian") ,
                new CultureInfoSettings("bg", "Bulgarian") ,
                new CultureInfoSettings("cs", "Czech"),
                new CultureInfoSettings("da", "Danish") ,
                new CultureInfoSettings("de", "German"),
                new CultureInfoSettings("el", "Greek") ,
                new CultureInfoSettings("es", "Spanish"),
                new CultureInfoSettings("et", "Estonian") ,
                new CultureInfoSettings("fa", "Persian"),
                new CultureInfoSettings("fi", "Finnish") ,
                new CultureInfoSettings("fr", "French"),
                new CultureInfoSettings("he", "Hebrew") ,
                new CultureInfoSettings("hr", "Croatian") ,
                new CultureInfoSettings("hu", "Hungarian") ,
                new CultureInfoSettings("id", "Indonesian"),
                new CultureInfoSettings("is", "Icelandic") ,
                new CultureInfoSettings("it", "Italian"),
                new CultureInfoSettings("ja", "Japanese"),
                new CultureInfoSettings("ka", "Georgian") ,
                new CultureInfoSettings("kk", "Kazakh") ,
                new CultureInfoSettings("ko", "Korean"),
                new CultureInfoSettings("lt", "Lithuanian") ,
                new CultureInfoSettings("lv", "Latvian") ,
                new CultureInfoSettings("ms", "Malay") ,
                new CultureInfoSettings("nl", "Dutch"),
                new CultureInfoSettings("nn", "Norwegian"),
                new CultureInfoSettings("nb", "Bokmål"),
                new CultureInfoSettings("pl", "Polish"),
                new CultureInfoSettings("pt", "Portuguese"),
                new CultureInfoSettings("ro", "Romanian") ,
                new CultureInfoSettings("ru", "Russian"),
                new CultureInfoSettings("sk", "Slovak"),
                new CultureInfoSettings("sl", "Slovenian") ,
                new CultureInfoSettings("sr", "Serbian") ,
                new CultureInfoSettings("sv", "Swedish") ,
                new CultureInfoSettings("th", "Thai") ,
                new CultureInfoSettings("tr", "Turkish"),
                new CultureInfoSettings("uk", "Ukrainian"),
                new CultureInfoSettings("vi", "Vietnamese"),
                new CultureInfoSettings("zh-Hans", "Chinese simplified"),
                new CultureInfoSettings("zh-Hant", "Chinese traditional"),
                new CultureInfoSettings("bn", "Bengali"),
                new CultureInfoSettings("hi", "Hindi"),
                new CultureInfoSettings("ur", "Urdu")
            };
        }
    }
}
