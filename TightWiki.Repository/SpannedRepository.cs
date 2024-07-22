namespace TightWiki.Repository
{
    public static class SpannedRepository
    {
        public static string VacuumDatabase(string databaseName)
        {
            var results = ManagedDataStorage.Collection.Single(o => o.Name == databaseName)
                .Factory.Query<string>("VacuumDatabase.sql");

            return string.Join("\r\n", results);
        }

        public static string OptimizeDatabase(string databaseName)
        {
            var results = ManagedDataStorage.Collection.Single(o => o.Name == databaseName)
                .Factory.Query<string>("OptimizeDatabase.sql");

            return string.Join("\r\n", results);
        }

        public static string IntegrityCheckDatabase(string databaseName)
        {
            var results = ManagedDataStorage.Collection.Single(o => o.Name == databaseName)
                .Factory.Query<string>("IntegrityCheckDatabase.sql");

            return string.Join("\r\n", results) + ForeignKeyCheck(databaseName);
        }

        public static string ForeignKeyCheck(string databaseName)
        {
            var results = ManagedDataStorage.Collection.Single(o => o.Name == databaseName)
                .Factory.Query<string>("ForeignKeyCheck.sql");

            return string.Join("\r\n", results);
        }

        public static List<(string Name, string Version)> GetDatabaseVersions()
        {
            var results = new List<(string, string)>();

            foreach (var db in ManagedDataStorage.Collection)
            {
                results.Add((db.Name, db.Factory.ExecuteScalar<string>("GetDatabaseVersion.sql") ?? string.Empty));
            }

            return results;
        }

        public static List<(string Name, int PageCount)> GetDatabasePageCounts()
        {
            var results = new List<(string, int)>();

            foreach (var db in ManagedDataStorage.Collection)
            {
                results.Add((db.Name, db.Factory.ExecuteScalar<int>("GetDatabasePageCount.sql")));
            }

            return results;
        }

        public static List<(string Name, int PageSize)> GetDatabasePageSizes()
        {
            var results = new List<(string, int)>();

            foreach (var db in ManagedDataStorage.Collection)
            {
                results.Add((db.Name, db.Factory.ExecuteScalar<int>("GetDatabasePageSize.sql")));
            }

            return results;
        }
    }
}
