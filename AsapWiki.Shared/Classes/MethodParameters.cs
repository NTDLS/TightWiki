using System.Collections.Generic;
using System.Linq;

namespace AsapWiki.Shared.Classes
{
    public class MethodParameters
    {
        /// <summary>
        /// Variables set by ordinal.
        /// </summary>
        public List<string> Ordinals { get; set; } = new List<string>();
        /// <summary>
        /// Variables set by name.
        /// </summary>
        public List<NamedParam> Named { get; private set; } = new List<NamedParam>();

        private MethodCallInstance _owner;

        public MethodParameters(MethodCallInstance owner)
        {
            _owner = owner;
        }

        public List<string> GetStringList(string name)
        {
            name = name.ToLower();
            return Named.Where(o => o.Name.ToLower() == name)?.Select(o => o.Value)?.ToList();
        }

        public string GetString(string name, string defaultValue)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name.ToLower() == name).FirstOrDefault()?.Value;
            if (value == null)
            {
                value = defaultValue;
            }

            return value;
        }

        public string GetString(string name)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name.ToLower() == name).FirstOrDefault()?.Value;
            if (value == null)
            {
                var prototype = _owner.Prototype.Parameters.Where(o => o.Name.ToLower() == name).First();
                value = prototype.DefaultValue;
            }

            return value;
        }

        public bool GetBool(string name)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name.ToLower() == name).FirstOrDefault()?.Value;
            if (value != null)
            {
                if (bool.TryParse(value, out bool parsed))
                {
                    return parsed;
                }
            }

            var prototype = _owner.Prototype.Parameters.Where(o => o.Name.ToLower() == name).First();
            return bool.Parse(prototype.DefaultValue);
        }

        public int GetInt(string name)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name.ToLower() == name).FirstOrDefault()?.Value;
            if (value != null)
            {
                if (int.TryParse(value, out int parsed))
                {
                    return parsed;
                }
            }

            var prototype = _owner.Prototype.Parameters.Where(o => o.Name.ToLower() == name).First();
            return int.Parse(prototype.DefaultValue);
        }

        public List<int> GetIntList(string name)
        {
            var intList = new List<int>();
            var stringList = GetStringList(name);
            foreach (var s in stringList)
            {
                if (int.TryParse(s, out int parsed))
                {
                    intList.Add(parsed);
                }
            }
            return intList;
        }

    }
}
