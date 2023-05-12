using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class StatsModel : ModelBase
    {
        public WikiDatabaseStats DatabaseStats { get; set; }
        public int CachedItemCount { get; set; }
        public long CacheMemoryLimit { get; set; }
    }
}
