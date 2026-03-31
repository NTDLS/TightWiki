namespace TightWiki.Plugin.Interfaces
{
    public interface ITwPluginModule
    {
        /// <summary>
        /// Gets the current assembly version in Major.Minor.Patch format.
        /// </summary>
        public string GetVersion();
    }
}
