namespace TightWiki.Plugin.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TwPluginModuleAttribute
    : Attribute
    {
        public string FriendlyName { get; }
        public string Description { get; }
        public int Order { get; }

        public TwPluginModuleAttribute(string friendlyName, string description, int order)
        {
            FriendlyName = friendlyName;
            Description = description;
            Order = order;
        }
    }
}
