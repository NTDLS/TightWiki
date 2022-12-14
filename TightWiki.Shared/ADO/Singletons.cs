namespace TightWiki.Shared.ADO
{
    public static class Singletons
    {
        public static int CommandTimeout { get; private set; } = 60;
        public static string ConnectionString { get; set; }
    }
}
