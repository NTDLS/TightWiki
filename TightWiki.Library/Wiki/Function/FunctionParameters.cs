using System;
using System.Collections.Generic;
using System.Linq;
using TightWiki.Library;

namespace TightWiki.Wiki.Function
{
    public class FunctionParameters
    {
        /// <summary>
        /// Variables set by ordinal.
        /// </summary>
        public List<OrdinalParam> Ordinals { get; set; } = new List<OrdinalParam>();
        /// <summary>
        /// Variables set by name.
        /// </summary>
        public List<NamedParam> Named { get; private set; } = new List<NamedParam>();

        private FunctionCallInstance _owner;

        public FunctionParameters(FunctionCallInstance owner)
        {
            _owner = owner;
        }

        public T Get<T>(string name)
        {
            try
            {
                var value = Named.Where(o => o.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
                if (value == null)
                {
                    var prototype = _owner.Prototype.Parameters.Where(o => o.Name.ToLower() == name.ToLower()).First();
                    return Utility.ConvertTo<T>(prototype.DefaultValue) ?? throw new Exception("Value cannot be null");
                }

                return Utility.ConvertTo<T>(value) ?? throw new Exception("Value cannot be null");
            }
            catch (Exception ex)
            {
                throw new Exception($"Function [{_owner.Name}], {ex.Message}");
            }
        }

        public T Get<T>(string name, T defaultValue)
        {
            try
            {
                var value = Named.Where(o => o.Name.ToLower() == name.ToLower()).FirstOrDefault()?.Value;
                if (value == null)
                {
                    return defaultValue;
                }

                return Utility.ConvertTo<T>(value) ?? throw new Exception("Value cannot be null");
            }
            catch (Exception ex)
            {
                throw new Exception($"Function [{_owner.Name}], {ex.Message}");
            }
        }

        public List<T> GetList<T>(string name)
        {
            try
            {
                var values = Named.Where(o => o.Name.ToLower() == name.ToLower())?
                    .Select(o => Utility.ConvertTo<T>(o.Value) ?? throw new Exception("Value cannot be null"))?.ToList();

                return values ?? new List<T>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Function [{_owner.Name}], {ex.Message}");
            }
        }
    }
}
