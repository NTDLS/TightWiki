namespace TightWiki.Plugin.Attributes.Functions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwInternalLinkHandlerAttribute
            : Attribute, ITwHandlerDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }

        public TwInternalLinkHandlerAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
