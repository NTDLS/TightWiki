using System;
using System.Collections.Generic;
using System.Linq;

namespace AsapWiki.Shared.Classes
{
    public class MethodParameters
    {
        /// <summary>
        /// Variables set by ordinal.
        /// </summary>
        public List<OrdinalParam> Ordinals { get; set; } = new List<OrdinalParam>();
        /// <summary>
        /// Variables set by name.
        /// </summary>
        public List<NamedParam> Named { get; private set; } = new List<NamedParam>();

        private MethodCallInstance _owner;

        public MethodParameters(MethodCallInstance owner)
        {
            _owner = owner;
        }

        public T Get<T>(string name)
        {
            string value = Named.Where(o => o.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
            if (value == null)
            {
                var prototype = _owner.Prototype.Parameters.Where(o => o.Name.ToLower() == name.ToLower()).First();
                return ConvertTo<T>(prototype.DefaultValue);
            }

            return ConvertTo<T>(value);
        }

        public T Get<T>(string name, T defaultValue)
        {
            string value = Named.Where(o => o.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
            if (value == null)
            {
                return defaultValue;
            }

            return ConvertTo<T>(value);
        }

        public List<T> GetList<T>(string name)
        {
            var values = Named.Where(o => o.Name.ToLower() == name.ToLower())?
                .Select(o => ConvertTo<T>(o.Value))?.ToList();
            return values;
        }

        public T ConvertTo<T>(string value)
        {
            if (typeof(T) == typeof(string))
            {
                return (T)Convert.ChangeType(value, typeof(T));
            }
            else if (typeof(T) == typeof(int))
            {
                if (int.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Method [{_owner.Name}], error converting value [{value}] to integer.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(float))
            {
                if (float.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Method [{_owner.Name}], error converting value [{value}] to float.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(double))
            {
                if (double.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Method [{_owner.Name}], error converting value [{value}] to double.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else if (typeof(T) == typeof(bool))
            {
                value = value.ToLower();
                if (bool.TryParse(value, out var parsedResult) == false)
                {
                    throw new Exception($"Method [{_owner.Name}], error converting value [{value}] to boolean.");
                }
                return (T)Convert.ChangeType(parsedResult, typeof(T));
            }
            else
            {
                throw new Exception($"Method [{_owner.Name}], unsupported parameter type.");
            }
        }
    }
}
