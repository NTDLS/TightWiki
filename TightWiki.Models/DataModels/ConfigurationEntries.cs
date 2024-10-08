﻿namespace TightWiki.Models.DataModels
{
    public class ConfigurationEntries
    {
        public List<ConfigurationEntry> Collection { get; set; }

        public ConfigurationEntries()
        {
            Collection = new();
        }

        public ConfigurationEntries(List<ConfigurationEntry> entries)
        {
            Collection = new List<ConfigurationEntry>(entries);
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
