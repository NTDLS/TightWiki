using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace TightWiki.Shared.Library
{
    public class CountryItem
    {
        public string Text { get; set; }
        public string Value { get; set; }

        public static List<CountryItem> GetAll()
        {
            var list = new List<CountryItem>();

            foreach (var ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                var regionInfo = new RegionInfo(ci.Name);
                if (list.Where(o => o.Value == regionInfo.Name).Any() == false)
                {
                    list.Add(new CountryItem
                    {
                        Text = regionInfo.EnglishName,
                        Value = regionInfo.Name
                    });
                }
            }

            return list.OrderBy(o => o.Text).ToList();
        }
    }
}
