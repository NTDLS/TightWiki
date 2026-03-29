namespace TightWiki.Plugin.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TwFunctionModuleAttribute
    : Attribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }

        public TwFunctionModuleAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
