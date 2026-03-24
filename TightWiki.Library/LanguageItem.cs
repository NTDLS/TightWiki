namespace TightWiki.Library
{
    public class LanguageItem
    {
        public string Text { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public static List<LanguageItem> GetAll()
        {
            var list = new List<LanguageItem>();

            var supportedCultures = new SupportedCultures();

            foreach (var culture in supportedCultures.Collection)
            {
                list.Add(new LanguageItem
                {
                    Text = culture.Culture.NativeName,
                    Value = culture.Code
                });
            }

            return list.OrderBy(o => o.Text).ToList();
        }
    }
}
