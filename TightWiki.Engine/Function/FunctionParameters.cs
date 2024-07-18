using TightWiki.Library;

namespace TightWiki.Wiki.Function
{
    public class FunctionParameters
    {
        /// <summary>
        /// Variables set by ordinal.
        /// </summary>
        public List<OrdinalParameter> Ordinals { get; set; } = new();
        /// <summary>
        /// Variables set by name.
        /// </summary>
        public List<NamedParameter> Named { get; private set; } = new();

        private FunctionCall _owner;

        public FunctionParameters(FunctionCall owner)
        {
            _owner = owner;
        }

        public T Get<T>(string name)
        {
            try
            {
                var value = Named.Where(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault()?.Value;
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
                var value = Named.Where(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault()?.Value;
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
                var values = Named.Where(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))?
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
