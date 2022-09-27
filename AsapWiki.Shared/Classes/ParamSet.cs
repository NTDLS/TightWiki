using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public class ParamSet
    {
        /// <summary>
        /// Variables set by name.
        /// </summary>
        public List<NamedParam> Named { get; set; } = new List<NamedParam>();

        public string NamedString(string name)
        {
            name = name.ToLower();
            return Named.Where(o => o.Name == name).FirstOrDefault()?.Value;
        }

        public string NamedString(string name, string defaultValue)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name == name).FirstOrDefault()?.Value;
            if (value == null)
            {
                return defaultValue;
            }
            return value;
        }

        public int? NamedInt(string name)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name == name).FirstOrDefault()?.Value;
            if (value != null)
            {
                if (int.TryParse(value, out int parsed))
                {
                    return parsed;
                }
            }

            return null;
        }

        public int NamedInt(string name, int defaultValue)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name == name).FirstOrDefault()?.Value;
            if (value != null)
            {
                if (int.TryParse(value, out int parsed))
                {
                    return parsed;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Variables set by ordinal.
        /// </summary>
        public List<string> Ordinals { get; set; } = new List<string>();
    }
}
