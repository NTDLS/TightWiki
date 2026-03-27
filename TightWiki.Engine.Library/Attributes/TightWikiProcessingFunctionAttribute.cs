namespace TightWiki.Engine.Library
{
    namespace Ae.Engine.Metadata
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class TightWikiProcessingFunctionAttribute
            : Attribute
        {
            public string FriendlyName { get; }
            public string? Description { get; }

            public TightWikiProcessingFunctionAttribute(string friendlyName, string? description = null)
            {
                FriendlyName = friendlyName;
                Description = description;
            }
        }
    }
}
