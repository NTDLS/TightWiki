namespace TightWiki.Engine.Library.Function.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TightWikiProcessingInstructionFunctionAttribute
            : Attribute, ITightWikiFunctionDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }
        public bool IsFirstChance { get; } = false;
        public string Demarcation { get; } = "@@";

        public TightWikiProcessingInstructionFunctionAttribute(string friendlyName, string? description = null)
        {
            FriendlyName = friendlyName;
            Description = description;
        }
    }
}
