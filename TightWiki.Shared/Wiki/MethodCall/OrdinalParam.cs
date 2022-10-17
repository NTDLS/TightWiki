namespace TightWiki.Shared.Wiki.MethodCall
{
    public class OrdinalParam
    {
        public string Value { get; set; }

        /// <summary>
        /// Has been matched to a prototype parameter?
        /// </summary>
        public bool IsMatched { get; set; } = false;

        /// <summary>
        /// If matched to a prototype parameter, this is the name of the parameter.
        /// </summary>
        public string ParameterName { get; set; }

        public OrdinalParam(string value)
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
