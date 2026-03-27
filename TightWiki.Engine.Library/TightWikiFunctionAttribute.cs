namespace TightWiki.Engine.Library
{
    namespace Ae.Engine.Metadata
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class TightWikiStandardFunction
            : Attribute
        {
            public string FriendlyName { get; }
            public string? Description { get; set; }

            public TightWikiStandardFunction(string friendlyName, string? description = null)
            {
                FriendlyName = friendlyName;
                Description = description;
            }
        }
    }
}
