namespace TightWiki.Engine.Library
{
    namespace Ae.Engine.Metadata
    {
        [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
        public class TightWikiStandardFunctionAttribute
            : Attribute
        {
            public string FriendlyName { get; }
            public string? Description { get; set; }
            public bool IsFirstChance { get; }

            public TightWikiStandardFunctionAttribute(string friendlyName, string? description = null, bool isFirstChance = false)
            {
                FriendlyName = friendlyName;
                Description = description;
                IsFirstChance = isFirstChance;
            }
        }
    }
}
