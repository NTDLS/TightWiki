namespace TightWiki.Library
{
    public class TwLanguageItem
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public static List<TwLanguageItem> GetAll()
        {
            var list = new List<TwLanguageItem>();

            var supportedCultures = new TwSupportedCultures();

            foreach (var culture in supportedCultures.Collection)
            {
                list.Add(new TwLanguageItem
                {
                    Text = culture.Culture.NativeName,
                    Value = culture.Code
                });
            }

            return list.OrderBy(o => o.Text).ToList();
        }
    }
}
