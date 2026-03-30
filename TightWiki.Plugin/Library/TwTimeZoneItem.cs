namespace TightWiki.Plugin.Library
{
    public class TwTimeZoneItem
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public static List<TwTimeZoneItem> GetAll()
        {
            var list = new List<TwTimeZoneItem>();

            foreach (var item in TimeZoneInfo.GetSystemTimeZones())
            {
                list.Add(new TwTimeZoneItem { Value = item.Id, Text = item.DisplayName });
            }

            return list.OrderBy(o => o.Text).ToList();
        }
    }
}
