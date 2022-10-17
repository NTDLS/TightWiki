namespace TightWiki.Shared.Models.Data
{
    public class ConfigurationFlat
    {
        public int GroupId { get; set; }
        public string GroupName { get; set; }
        public string GroupDescription { get; set; }
        public int EntryId { get; set; }
        public string EntryName { get; set; }
        public string EntryValue { get; set; }
        public string EntryDescription { get; set; }
        public bool IsEncrypted { get; set; }
        public string DataType { get; set; }
    }
}
