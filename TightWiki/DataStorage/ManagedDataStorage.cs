namespace TightWiki.DataStorage
{
    /// <summary>
    /// Stores instances of ManagedDataStorageFactories that are used to store various parts of the data for the site.
    /// </summary>
    public static class ManagedDataStorage
    {
        public static ManagedDataStorageFactory Default { get; private set; } = new();
        public static ManagedDataStorageFactory Statistics { get; private set; } = new();
        public static ManagedDataStorageFactory Emoji { get; private set; } = new();
        public static ManagedDataStorageFactory Exceptions { get; private set; } = new();
    }
}
