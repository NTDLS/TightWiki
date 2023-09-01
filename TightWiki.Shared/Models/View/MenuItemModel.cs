using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class MenuItemModel : ModelBase
    {
        public int? Id { get; set; }
        public string Name { get; set; }
        public string Link { get; set; }
        public int Ordinal { get; set; }

        public MenuItem ToDataModel()
        {
            return new MenuItem
            {
                Name = Name,
                Id = Id ?? 0,
                Link = Link,
                Ordinal = Ordinal
            };
        }
    }
}
