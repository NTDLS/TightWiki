using System.Globalization;

namespace TightWiki.Plugin.Library
{
    public class TwCountryItem
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public static List<TwCountryItem> GetAll()
        {
            var list = new List<TwCountryItem>();

            foreach (var ci in CultureInfo.GetCultures(CultureTypes.SpecificCultures))
            {
                var regionInfo = new RegionInfo(ci.Name);
                if (list.Where(o => o.Value == regionInfo.Name).Any() == false)
                {
                    list.Add(new TwCountryItem
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
