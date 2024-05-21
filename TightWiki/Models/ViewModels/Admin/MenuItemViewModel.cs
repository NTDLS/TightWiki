using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Admin
{
    public class MenuItemViewModel : ViewModelBase
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public int Ordinal { get; set; }

        public MenuItem ToDataModel()
        {
            return new MenuItem
            {
                Name = Name,
                Id = Id,
                Link = Link,
                Ordinal = Ordinal
            };
        }
    }
}
