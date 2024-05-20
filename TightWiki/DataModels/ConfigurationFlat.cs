namespace TightWiki.DataModels
{
    public class ConfigurationFlat
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; } = string.Empty;
        public string GroupDescription { get; set; } = string.Empty;
        public int EntryId { get; set; }
        public string EntryName { get; set; } = string.Empty;
        public string EntryValue { get; set; } = string.Empty;
        public string EntryDescription { get; set; } = string.Empty;
        public bool IsEncrypted { get; set; }
        public string DataType { get; set; } = string.Empty;
    }
}
