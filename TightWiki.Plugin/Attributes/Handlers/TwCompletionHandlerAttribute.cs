namespace TightWiki.Plugin.Attributes.Functions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwCompletionHandlerAttribute
            : Attribute, ITwHandlerDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }

        public TwCompletionHandlerAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
