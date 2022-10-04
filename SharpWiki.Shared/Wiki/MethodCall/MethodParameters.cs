using SharpWiki.Shared.Library;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SharpWiki.Shared.Wiki.MethodCall
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
            try
            {
                string value = Named.Where(o => o.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
                if (value == null)
                {
                    var prototype = _owner.Prototype.Parameters.Where(o => o.Name.ToLower() == name.ToLower()).First();
                    return Utility.ConvertTo<T>(prototype.DefaultValue);
                }

                return Utility.ConvertTo<T>(value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Method [{_owner.Name}], {ex.Message}");
            }
        }

        public T Get<T>(string name, T defaultValue)
        {
            try
            {
                string value = Named.Where(o => o.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
                if (value == null)
                {
                    return defaultValue;
                }

                return Utility.ConvertTo<T>(value);
            }
            catch (Exception ex)
            {
                throw new Exception($"Method [{_owner.Name}], {ex.Message}");
            }
        }

        public List<T> GetList<T>(string name)
        {
            try
            {
                var values = Named.Where(o => o.Name.ToLower() == name.ToLower())?
                    .Select(o => Utility.ConvertTo<T>(o.Value))?.ToList();
                return values;
            }
            catch (Exception ex)
            {
                throw new Exception($"Method [{_owner.Name}], {ex.Message}");
            }
        }
    }
}
