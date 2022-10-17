using TightWiki.Shared.Library;

namespace TightWiki.Shared.Models.Data
{
    public partial class ConfigurationEntry
    {
        public T As<T>()
        {
            return Utility.ConvertTo<T>(Value);
        }

        public T As<T>(T defaultValue)
        {
            if (Value == null)
            {
                return defaultValue;
            }

            return Utility.ConvertTo<T>(Value);
        }

        public string DataType { get; set; }
    }
}
