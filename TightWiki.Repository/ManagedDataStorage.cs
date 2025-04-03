using NTDLS.SqliteDapperWrapper;

namespace TightWiki.Repository
{
    /// <summary>
    /// Stores instances of ManagedDataStorageFactories that are used to store various parts of the data for the site.
    /// </summary>
    public static class ManagedDataStorage
    {
        private static (string Name, SqliteManagedFactory Factory)[]? _collection = null;

        public static (string Name, SqliteManagedFactory Factory)[] Collection
        {
            get
            {
                _collection ??=
                    [
                        ("DeletedPageRevisions", DeletedPageRevisions),
                        ("DeletedPages", DeletedPages),
                        ("Pages", Pages),
                        ("Statistics", Statistics),
                        ("Emoji", Emoji),
                        ("Exceptions", Exceptions),
                        ("Users", Users),
                        ("Config", Config)
                    ];
                return _collection;
            }
        }

        public static SqliteManagedFactory DeletedPageRevisions { get; private set; } = new();
        public static SqliteManagedFactory DeletedPages { get; private set; } = new();
        public static SqliteManagedFactory Pages { get; private set; } = new();
        public static SqliteManagedFactory Statistics { get; private set; } = new();
        public static SqliteManagedFactory Emoji { get; private set; } = new();
        public static SqliteManagedFactory Exceptions { get; private set; } = new();
        public static SqliteManagedFactory Users { get; private set; } = new();
        public static SqliteManagedFactory Config { get; private set; } = new();
    }
}
