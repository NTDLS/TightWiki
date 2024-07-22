using NTDLS.SqliteDapperWrapper;

namespace TightWiki
{
    /// <summary>
    /// Stores instances of ManagedDataStorageFactories that are used to store various parts of the data for the site.
    /// </summary>
    public static class ManagedDataStorage
    {
        private static (string Name, ManagedDataStorageFactory Factory)[]? _collection = null;

        public static (string Name, ManagedDataStorageFactory Factory)[] Collection
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
                        ("Words", Words),
                        ("Users", Users),
                        ("Config", Config)
                    ];
                return _collection;
            }
        }

        public static ManagedDataStorageFactory DeletedPageRevisions { get; private set; } = new();
        public static ManagedDataStorageFactory DeletedPages { get; private set; } = new();
        public static ManagedDataStorageFactory Pages { get; private set; } = new();
        public static ManagedDataStorageFactory Statistics { get; private set; } = new();
        public static ManagedDataStorageFactory Emoji { get; private set; } = new();
        public static ManagedDataStorageFactory Exceptions { get; private set; } = new();
        public static ManagedDataStorageFactory Words { get; private set; } = new();
        public static ManagedDataStorageFactory Users { get; private set; } = new();
        public static ManagedDataStorageFactory Config { get; private set; } = new();
    }
}
