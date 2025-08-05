namespace GenerateSeedData
{
    public partial class ConfigurationEntry
    {
        public int Id { get; set; }
        public int ConfigurationGroupId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
        public int DataTypeId { get; set; }
        public string Description { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public bool IsRequired { get; set; }

        public string ConfigurationGroupName { get; set; } = string.Empty;
    }
}
