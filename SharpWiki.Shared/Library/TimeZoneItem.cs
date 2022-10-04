using System;
using System.Collections.Generic;

namespace SharpWiki.Shared.Library
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

            return list;
        }
    }
}
