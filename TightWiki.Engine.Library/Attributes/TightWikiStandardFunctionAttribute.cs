namespace TightWiki.Engine.Library.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TightWikiStandardFunctionAttribute
            : Attribute, ITightWikiFunctionDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }
        public bool IsFirstChance { get; }
        public string Demarcation { get; } = "##";

        public TightWikiStandardFunctionAttribute(string friendlyName, string? description = null, bool isFirstChance = false)
        {
            FriendlyName = friendlyName;
            Description = description;
            IsFirstChance = isFirstChance;
        }
    }
}
