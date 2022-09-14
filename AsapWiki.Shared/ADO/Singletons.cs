using System.Configuration;

namespace AsapWiki.Shared.ADO
{
    public static class Singletons
    {
        public static int CommandTimeout { get; private set; } = 60;

        private static string connectionString = null;
        public static string ConnectionString
        {
            get
            {
                if (connectionString == null)
                {
                    connectionString = ConfigurationManager.ConnectionStrings["AsapWikiADO"].ConnectionString;
                }
                return connectionString;
            }
        }
    }
}
