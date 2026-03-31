namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    ///  Data access for spanned queries that operate across multiple databases,
    ///  such as vacuuming all databases, optimizing all databases, integrity checking all databases,
    ///  foreign key checking all databases, and getting database versions, page counts, and page sizes.
    ///  These are operations that are not specific to a single database, but rather operate across all
    ///  databases used by the plugin. This is intended to be used by the plugin's maintenance tools
    ///  to perform these operations on all databases at once.
    /// </summary>
    public interface ISpannedRepository
    {
        public Task<string> VacuumDatabase(string databaseName);
        public Task<string> OptimizeDatabase(string databaseName);
        public Task<string> IntegrityCheckDatabase(string databaseName);
        public Task<string> ForeignKeyCheck(string databaseName);
        public Task<List<(string Name, string Version)>> GetDatabaseVersions();
        public Task<List<(string Name, int PageCount)>> GetDatabasePageCounts();
        public Task<List<(string Name, int PageSize)>> GetDatabasePageSizes();
    }
}
