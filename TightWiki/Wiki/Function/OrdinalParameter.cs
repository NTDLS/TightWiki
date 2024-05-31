namespace TightWiki.Wiki.Function
{
    public class OrdinalParameter
    {
        public string Value { get; set; }

        /// <summary>
        /// Has been matched to a prototype parameter?
        /// </summary>
        public bool IsMatched { get; set; } = false;

        /// <summary>
        /// If matched to a prototype parameter, this is the name of the parameter.
        /// </summary>
        public string ParameterName { get; set; } = string.Empty;

        public OrdinalParameter(string value)
        {
            Value = value;
        }

        public void AssociateWithPrototypeParam(string paramName)
        {
            IsMatched = true;
            ParameterName = paramName;
        }
    }
}
