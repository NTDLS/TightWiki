namespace TightWiki.Plugin.Attributes.Functions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwExternalLinkHandlerAttribute
            : Attribute, ITwHandlerDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }

        public TwExternalLinkHandlerAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
