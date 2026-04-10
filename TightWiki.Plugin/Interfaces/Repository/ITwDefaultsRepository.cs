using NTDLS.SqliteDapperWrapper;

namespace TightWiki.Plugin.Interfaces.Repository
{
    /// <summary>
    ///  Data access for default values that are used when creating a new wiki, such as default configuration entries, default themes, etc.
    /// </summary>
    public interface ITwDefaultsRepository
    {
        /// <summary>
        /// SQLite factory that is used to access the database.
        /// </summary>
        SqliteManagedFactory DefaultsFactory { get; }
    }
}
