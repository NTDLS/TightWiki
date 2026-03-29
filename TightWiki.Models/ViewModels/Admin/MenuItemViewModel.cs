using TightWiki.Plugin.Models;

namespace TightWiki.Models.ViewModels.Admin
{
    public class MenuItemViewModel
        : ViewModelBase
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
