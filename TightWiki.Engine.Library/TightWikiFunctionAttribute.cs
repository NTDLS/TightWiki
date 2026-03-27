namespace TightWiki.Engine.Library
{
    namespace Ae.Engine.Metadata
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class TwStandardFunction
            : Attribute
        {
            public string FriendlyName { get; }
            public string? Description { get; set; }

            public TwStandardFunction(string friendlyName, string? description = null)
            {
                FriendlyName = friendlyName;
                Description = description;
            }
        }
    }
}
