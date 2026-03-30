namespace TightWiki.Plugin.Attributes.Functions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwMarkupHandlerAttribute
            : Attribute, ITwHandlerDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }

        public TwMarkupHandlerAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
