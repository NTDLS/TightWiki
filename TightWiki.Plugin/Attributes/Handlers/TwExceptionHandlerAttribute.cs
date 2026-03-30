namespace TightWiki.Plugin.Attributes.Functions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwExceptionHandlerAttribute
            : Attribute, ITwHandlerDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }

        public TwExceptionHandlerAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
