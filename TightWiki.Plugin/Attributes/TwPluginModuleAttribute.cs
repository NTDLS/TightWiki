namespace TightWiki.Plugin.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TwPluginModuleAttribute
    : Attribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }

        public TwPluginModuleAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
