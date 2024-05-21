using TightWiki.Library;

namespace TightWiki.Models.DataModels
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

        public T? As<T>()
        {
            return Utility.ConvertTo<T>(Value);
        }

        public T? As<T>(T defaultValue)
        {
            if (Value == null)
            {
                return defaultValue;
            }

            return Utility.ConvertTo<T>(Value);
        }

        public string DataType { get; set; } = string.Empty;
    }
}