﻿using System.Globalization;

namespace TightWiki.Library
{
    public class CountryItem
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

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
