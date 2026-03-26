namespace TightWiki.Repository
{
    public static class SpannedRepository
    {
        public static async Task<string> VacuumDatabase(string databaseName)
        {
            var results = await ManagedDataStorage.Collection.Single(o => o.Name == databaseName)
                .Factory.QueryAsync<string>("VacuumDatabase.sql");

            return string.Join("\r\n", results);
        }

        public static async Task<string> OptimizeDatabase(string databaseName)
        {
            var results = ManagedDataStorage.Collection.Single(o => o.Name == databaseName)
                .Factory.Query<string>("OptimizeDatabase.sql");

            return string.Join("\r\n", results);
        }

        public static async Task<string> IntegrityCheckDatabase(string databaseName)
        {
            var results = await ManagedDataStorage.Collection.Single(o => o.Name == databaseName)
                .Factory.QueryAsync<string>("IntegrityCheckDatabase.sql");

            return string.Join("\r\n", results) + ForeignKeyCheck(databaseName);
        }

        public static async Task<string> ForeignKeyCheck(string databaseName)
        {
            var results = await ManagedDataStorage.Collection.Single(o => o.Name == databaseName)
                .Factory.QueryAsync<string>("ForeignKeyCheck.sql");

            return string.Join("\r\n", results);
        }

        public static async Task<List<(string Name, string Version)>> GetDatabaseVersions()
        {
            var results = new List<(string, string)>();

            foreach (var db in ManagedDataStorage.Collection)
            {
                results.Add((db.Name, await db.Factory.ExecuteScalarAsync<string>("GetDatabaseVersion.sql") ?? string.Empty));
            }

            return results;
        }

        public static async Task<List<(string Name, int PageCount)>> GetDatabasePageCounts()
        {
            var results = new List<(string, int)>();

            foreach (var db in ManagedDataStorage.Collection)
            {
                results.Add((db.Name, await db.Factory.ExecuteScalarAsync<int>("GetDatabasePageCount.sql")));
            }

            return results;
        }

        public static async Task<List<(string Name, int PageSize)>> GetDatabasePageSizes()
        {
            var results = new List<(string, int)>();

            foreach (var db in ManagedDataStorage.Collection)
            {
                results.Add((db.Name, await db.Factory.ExecuteScalarAsync<int>("GetDatabasePageSize.sql")));
            }

            return results;
        }
    }
}
