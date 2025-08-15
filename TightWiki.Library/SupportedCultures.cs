using NTDLS.Helpers;
using System.Globalization;

namespace TightWiki.Library
{
    public class SupportedCultures
    {
        private readonly IList<CultureInfoSettings> Cultures;

        public IList<CultureInfo> AllCultures
            => Cultures.Select(x => x.Culture.EnsureNotNull()).ToList();

        public IList<CultureInfo> UICompleteCultures
            => Cultures.Where(x => x.IsUIComplete).Select(x => x.Culture.EnsureNotNull()).ToList();

        public static CultureInfo DefaultCulture
            => new CultureInfo("en");

        public SupportedCultures()
        {
            Cultures = new List<CultureInfoSettings>
            {
                new CultureInfoSettings { Culture = new CultureInfo("en"), IsUIComplete = true }, // English 
                new CultureInfoSettings { Culture = new CultureInfo("ar"), IsUIComplete = true }, // Arabic 
                new CultureInfoSettings { Culture = new CultureInfo("az"), IsUIComplete = false }, // Azerbaijani 
                new CultureInfoSettings { Culture = new CultureInfo("be"), IsUIComplete = false }, // Belarusian 
                new CultureInfoSettings { Culture = new CultureInfo("bg"), IsUIComplete = false }, // Bulgarian 
                new CultureInfoSettings { Culture = new CultureInfo("cs"), IsUIComplete = true }, // Czech 
                new CultureInfoSettings { Culture = new CultureInfo("da"), IsUIComplete = false }, // Danish 
                new CultureInfoSettings { Culture = new CultureInfo("de"), IsUIComplete = true }, // German 
                new CultureInfoSettings { Culture = new CultureInfo("el"), IsUIComplete = false }, // Greek 
                new CultureInfoSettings { Culture = new CultureInfo("es"), IsUIComplete = true }, // Spanish   
                new CultureInfoSettings { Culture = new CultureInfo("et"), IsUIComplete = false }, // Estonian 
                new CultureInfoSettings { Culture = new CultureInfo("fa"), IsUIComplete = true }, // Persian 
                new CultureInfoSettings { Culture = new CultureInfo("fi"), IsUIComplete = false }, // Finnish 
                new CultureInfoSettings { Culture = new CultureInfo("fr"), IsUIComplete = true }, // French 
                new CultureInfoSettings { Culture = new CultureInfo("he"), IsUIComplete = false }, // Hebrew 
                new CultureInfoSettings { Culture = new CultureInfo("hr"), IsUIComplete = false }, // Croatian 
                new CultureInfoSettings { Culture = new CultureInfo("hu"), IsUIComplete = false }, // Hungarian 
                new CultureInfoSettings { Culture = new CultureInfo("id"), IsUIComplete = true }, // Indonesian 
                new CultureInfoSettings { Culture = new CultureInfo("is"), IsUIComplete = false }, // Icelnadic 
                new CultureInfoSettings { Culture = new CultureInfo("it"), IsUIComplete = true }, // Italian 
                new CultureInfoSettings { Culture = new CultureInfo("ja"), IsUIComplete = true }, // Japanese 
                new CultureInfoSettings { Culture = new CultureInfo("ka"), IsUIComplete = false }, // Georgian 
                new CultureInfoSettings { Culture = new CultureInfo("kk"), IsUIComplete = false }, // Kazakh 
                new CultureInfoSettings { Culture = new CultureInfo("ko"), IsUIComplete = true }, // Korean
                new CultureInfoSettings { Culture = new CultureInfo("lt"), IsUIComplete = false }, // Lithuianian 
                new CultureInfoSettings { Culture = new CultureInfo("lv"), IsUIComplete = false }, // Latvian 
                new CultureInfoSettings { Culture = new CultureInfo("ms"), IsUIComplete = false }, // Malay 
                new CultureInfoSettings { Culture = new CultureInfo("nl"), IsUIComplete = true }, // Dutch 
                new CultureInfoSettings { Culture = new CultureInfo("nn"), IsUIComplete = false }, // Norwegian 
                new CultureInfoSettings { Culture = new CultureInfo("pl"), IsUIComplete = true }, // Polish 
                new CultureInfoSettings { Culture = new CultureInfo("pt"), IsUIComplete = true }, // Portuguese 
                new CultureInfoSettings { Culture = new CultureInfo("ro"), IsUIComplete = false }, // Romanian 
                new CultureInfoSettings { Culture = new CultureInfo("ru"), IsUIComplete = true }, // Russian 
                new CultureInfoSettings { Culture = new CultureInfo("sk"), IsUIComplete = true }, // Slovak 
                new CultureInfoSettings { Culture = new CultureInfo("sl"), IsUIComplete = false }, // Slovenian 
                new CultureInfoSettings { Culture = new CultureInfo("sr"), IsUIComplete = false }, // Serbian 
                new CultureInfoSettings { Culture = new CultureInfo("sv"), IsUIComplete = false }, // Swedish 
                new CultureInfoSettings { Culture = new CultureInfo("th"), IsUIComplete = false }, // Thai 
                new CultureInfoSettings { Culture = new CultureInfo("tr"), IsUIComplete = true }, // Turkish 
                new CultureInfoSettings { Culture = new CultureInfo("uk"), IsUIComplete = true }, // Ukrainian 
                new CultureInfoSettings { Culture = new CultureInfo("vi"), IsUIComplete = true }, // Vietnamese 
                new CultureInfoSettings { Culture = new CultureInfo("zh-Hans"), IsUIComplete = true }, // Chinese simplified 
                new CultureInfoSettings { Culture = new CultureInfo("zh-Hant"), IsUIComplete = true }, // Chinese traditional 
                new CultureInfoSettings { Culture = new CultureInfo("bn"), IsUIComplete = true }, // Bengali
                new CultureInfoSettings { Culture = new CultureInfo("hi"), IsUIComplete = true }, // Hindi
                new CultureInfoSettings { Culture = new CultureInfo("ur"), IsUIComplete = true }, // Urdu
            };
        }
    }
}
