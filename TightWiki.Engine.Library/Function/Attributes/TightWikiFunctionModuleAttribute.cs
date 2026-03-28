namespace TightWiki.Engine.Library.Function.Attributes
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class TightWikiFunctionModuleAttribute
    : Attribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }

        public TightWikiFunctionModuleAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
