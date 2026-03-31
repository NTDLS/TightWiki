using NTDLS.SqliteDapperWrapper;

namespace TightWiki.Plugin.Interfaces
{
    public interface ITwManagedDataStorage
    {
        SqliteManagedFactory DeletedPageRevisions { get; }
        SqliteManagedFactory DeletedPages { get; }
        SqliteManagedFactory Pages { get; }
        SqliteManagedFactory Statistics { get; }
        SqliteManagedFactory Emoji { get; }
        SqliteManagedFactory Logging { get; }
        SqliteManagedFactory Users { get; }
        SqliteManagedFactory Config { get; }
        SqliteManagedFactory Defaults { get; }
    }
}
