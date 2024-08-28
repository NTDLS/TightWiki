using NTDLS.Helpers;

namespace TightWiki.Engine.Function
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

        private readonly FunctionCall _owner;

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
                    var prototype = _owner.Prototype.Parameters.Where(o => o.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase)).First();
                    return Converters.ConvertTo<T>(prototype.DefaultValue) ?? throw new Exception("Value cannot be null");
                }

                return Converters.ConvertTo<T>(value) ?? throw new Exception("Value cannot be null");
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

                return Converters.ConvertTo<T>(value) ?? throw new Exception("Value cannot be null");
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
                    .Select(o => Converters.ConvertTo<T>(o.Value) ?? throw new Exception("Value cannot be null"))?.ToList();

                return values ?? new List<T>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Function [{_owner.Name}], {ex.Message}");
            }
        }
    }
}
