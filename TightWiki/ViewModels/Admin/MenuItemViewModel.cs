using TightWiki.Plugin.Models;

namespace TightWiki.ViewModels.Admin
{
    public class MenuItemViewModel
        : TwViewModel
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public int Ordinal { get; set; }

        public TwMenuItem ToDataModel()
        {
            return new TwMenuItem
            {
                Name = Name,
                Id = Id,
                Link = Link,
                Ordinal = Ordinal
            };
        }
    }
}
