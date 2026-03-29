namespace TightWiki.Plugin.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwProcessingInstructionFunctionAttribute
            : Attribute, ITwFunctionDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }
        public bool IsFirstChance { get; } = false;
        public string Demarcation { get; } = "@@";

        public TwProcessingInstructionFunctionAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
