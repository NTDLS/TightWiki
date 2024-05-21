using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TightWiki.Library
{
    public class LanguageItem
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public static List<LanguageItem> GetAll()
        {
            var list = new List<LanguageItem>();

            var cultureInfo = CultureInfo.GetCultures(CultureTypes.SpecificCultures);

            foreach (var culture in cultureInfo)
            {
                var name = culture.NativeName;
                if (name.Contains('('))
                {
                    name = name.Substring(0, name.IndexOf('(')).Trim();
                }

                if (list.Where(o => o.Value == name).Any() == false)
                {
                    list.Add(new LanguageItem
                    {
                        Text = name,
                        Value = name
                    });
                }
            }

            return list.OrderBy(o => o.Text).ToList();
        }
    }
}
