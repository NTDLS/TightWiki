namespace TightWiki.Plugin.Models
{
    public class TwConfigurationEntries
    {
        public List<TwConfigurationEntry> Collection { get; set; }

        public TwConfigurationEntries()
        {
            Collection = new();
        }

        public TwConfigurationEntries(List<TwConfigurationEntry> entries)
        {
            Collection = new List<TwConfigurationEntry>(entries);
        }

        public T? Value<T>(string name)
        {
            var value = Collection.Where(o => o.Name == name).FirstOrDefault();
            if (value == null)
            {
                return default;
            }
            return value.As<T>();
        }

        public T Value<T>(string name, T defaultValue)
        {
            var value = Collection.Where(o => o.Name == name).FirstOrDefault();
            if (value == null)
            {
                return defaultValue;
            }
            return value.As<T>() ?? defaultValue;
        }
    }
}
