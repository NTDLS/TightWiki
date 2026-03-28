namespace TightWiki.Engine.Library.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TightWikiPostProcessingInstructionFunctionAttribute
            : Attribute, ITightWikiFunctionDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }
        public bool IsFirstChance { get; } = false;
        public string Demarcation { get; } = "##";

        public TightWikiPostProcessingInstructionFunctionAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
