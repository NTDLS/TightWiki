using TightWiki.Models.ViewModels.Admin;

namespace TightWiki.Models.DataModels
{
    public partial class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Link { get; set; } = string.Empty;
        public int Ordinal { get; set; }

        public MenuItemViewModel ToViewModel()
        {
            return new MenuItemViewModel
            {
                Name = Name,
                Id = Id,
                Link = Link,
                Ordinal = Ordinal
            };
        }
    }
}
