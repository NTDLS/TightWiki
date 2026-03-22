namespace GenerateSeedData.Models
{
    public partial class DefaultConfiguration
    {
        public string ConfigurationGroupName { get; set; } = string.Empty;
        public string ConfigurationEntryName { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int DataTypeId { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public bool IsRequired { get; set; }
    }
}
