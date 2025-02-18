namespace TightWiki.Engine.Function
{
    public class FunctionPrototype
    {
        /// <summary>
        /// Demarcation is simply the prefix of the function (e.g. ##, $$, @@, etc.);
        /// </summary>
        public string Demarcation { get; set; } = string.Empty;
        /// <summary>
        /// Name of the function to use when displaying the function name, verbatim from the prototype string.
        /// </summary>
        public string ProperName { get; set; } = string.Empty;

        /// <summary>
        /// Invariant culture lower-cased name of the function, stored simply for matching performance.
        /// </summary>
        public string Key { get; set; } = string.Empty;

        /// <summary>
        /// List of parameters associated with the function.
        /// </summary>
        public List<PrototypeParameter> Parameters { get; set; }

        /// <summary>
        /// Whether this function should be processed as early as possible when parsing a page.
        /// </summary>
        public bool IsFirstChance { get; set; }

        public FunctionPrototype(string properName)
        {
            Parameters = new List<PrototypeParameter>();
            ProperName = properName;
            Key = properName.ToLowerInvariant();
        }

        public FunctionPrototype()
        {
            Parameters = new List<PrototypeParameter>();
        }
    }
}
