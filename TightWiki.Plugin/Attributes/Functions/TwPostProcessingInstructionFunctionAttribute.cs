namespace TightWiki.Plugin.Attributes.Functions
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TwPostProcessingInstructionFunctionAttribute
            : Attribute, ITwFunctionDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }
        public bool IsFirstChance { get; } = false;
        public string Demarcation { get; } = "##";

        public TwPostProcessingInstructionFunctionAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
