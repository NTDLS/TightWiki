using System.Globalization;

namespace TightWiki.Library
{
    public class TwCultureInfoSettings
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public CultureInfo Culture { get; set; }

        public TwCultureInfoSettings(string code, string name)
        {
            Code = code;
            Name = name;
            Culture = new CultureInfo(code);
        }
    }
}
