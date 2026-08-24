namespace TightWiki.Data.EfCore.Entities.Config
{
    /// <summary>
    /// A data type descriptor referenced by <see cref="ConfigurationEntry.DataTypeId"/> (Config.DataType).
    /// </summary>
    public class DataType
    {
        /// <summary>
        /// The unique identifier for this data type.
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// The name of this data type. Nullable and unique in the real schema (no NOT NULL constraint is
        /// declared, only the unique index).
        /// </summary>
        public string? Name { get; set; }
    }
}
