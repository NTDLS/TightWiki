using System;
using System.Collections.Generic;
using System.Linq;

namespace TightWiki.Shared.Library
{
    public class TimeZoneItem
    {
        public string Text { get; set; }
        public string Value { get; set; }

        public static List<TimeZoneItem> GetAll()
        {
            var list = new List<TimeZoneItem>();

            foreach (var item in TimeZoneInfo.GetSystemTimeZones())
            {
                list.Add(new TimeZoneItem { Value = item.Id, Text = item.DisplayName });
            }

            return list.OrderBy(o => o.Text).ToList();
        }
    }
}
