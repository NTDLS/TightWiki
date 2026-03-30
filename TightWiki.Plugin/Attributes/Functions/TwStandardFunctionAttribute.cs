namespace TightWiki.Plugin.Attributes.Functions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwStandardFunctionAttribute
            : Attribute, ITwFunctionDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }
        public bool IsFirstChance { get; }
        public string Demarcation { get; } = "##";

        public TwStandardFunctionAttribute(string friendlyName, string? description = null, bool isFirstChance = false)
        {
            FriendlyName = friendlyName;
            Description = description;
            IsFirstChance = isFirstChance;
        }
    }
}
