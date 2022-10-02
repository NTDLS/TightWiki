using AsapWiki.Shared.Library;

namespace AsapWiki.Shared.Models
{
    public partial class ConfigurationEntry
    {
        public T Get<T>()
        {
            return Utility.ConvertTo<T>(Value);
        }

        public T Get<T>(T defaultValue)
        {
            if (Value == null)
            {
                return defaultValue;
            }

            return Utility.ConvertTo<T>(Value);
        }
    }
}
