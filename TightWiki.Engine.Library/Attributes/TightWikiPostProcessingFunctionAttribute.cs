namespace TightWiki.Engine.Library
{
    namespace Ae.Engine.Metadata
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class TightWikiPostProcessingFunctionAttribute
            : Attribute
        {
            public string FriendlyName { get; }
            public string? Description { get; }

            public TightWikiPostProcessingFunctionAttribute(string friendlyName, string? description = null)
            {
                FriendlyName = friendlyName;
                Description = description;
            }
        }
    }
}
