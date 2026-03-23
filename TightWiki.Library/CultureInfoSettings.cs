using System.Globalization;

namespace TightWiki.Library
{
    public class CultureInfoSettings
    {
        public string Code { get; set; }
        public string Name { get; set; }
        public CultureInfo Culture { get; set; }

        public CultureInfoSettings(string code, string name)
        {
            Code = code;
            Name = name;
            Culture = new CultureInfo(code);
        }
    }
}
