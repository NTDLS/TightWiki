using TightWiki.Shared.Models.View;

namespace TightWiki.Shared.Models.Data
{
    public partial class MenuItem
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public int Ordinal { get; set; }

        public MenuItemModel ToViewModel()
        {
            return new MenuItemModel
            {
                Name = Name,
                Id = Id,
                Link = Link,
                Ordinal = Ordinal
            };
        }
    }
}
