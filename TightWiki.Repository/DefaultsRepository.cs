using NTDLS.SqliteDapperWrapper;
using TightWiki.Plugin.Interfaces.Repository;

namespace TightWiki.Repository
{
    public partial class DefaultsRepository
        : ITwDefaultsRepository
    {
        public SqliteManagedFactory DefaultsFactory { get; private set; }

        public DefaultsRepository(string connectionString)
        {
            DefaultsFactory = new SqliteManagedFactory(connectionString);
        }
    }
}
