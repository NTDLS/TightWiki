using NTDLS.SqliteDapperWrapper;

namespace TightWiki.Plugin.Interfaces.Repository
{
    public interface ITwDefaultsRepository
    {
        SqliteManagedFactory DefaultsFactory { get; }
    }
}
