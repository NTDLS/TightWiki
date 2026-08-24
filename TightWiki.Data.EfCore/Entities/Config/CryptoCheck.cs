namespace TightWiki.Data.EfCore.Entities.Config
{
    /// <summary>
    /// Keyless, single-row table storing an encrypted marker value used to detect whether the configured
    /// machine key can still decrypt previously-encrypted configuration values (Config.CryptoCheck). The real
    /// schema declares no primary key - the row is deleted and re-inserted wholesale rather than updated in place.
    /// </summary>
    public class CryptoCheck
    {
        /// <summary>
        /// The encrypted marker value.
        /// </summary>
        public string? Content { get; set; }
    }
}
