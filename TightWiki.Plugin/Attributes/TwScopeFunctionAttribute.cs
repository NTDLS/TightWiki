namespace TightWiki.Plugin.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwScopeFunctionAttribute
            : Attribute, ITwFunctionDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }
        public bool IsFirstChance { get; }
        public string Demarcation { get; } = "{{";

        public TwScopeFunctionAttribute(string friendlyName, string? description = null, bool isFirstChance = false)
        {
            FriendlyName = friendlyName;
            Description = description;
            IsFirstChance = isFirstChance;
        }
    }
}
