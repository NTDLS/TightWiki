namespace TightWiki.Plugin.Interfaces.Repository
{
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
