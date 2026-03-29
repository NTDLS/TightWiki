namespace TightWiki.Engine.Library.Function.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
    public class TightWikiScopeFunctionAttribute
            : Attribute, ITightWikiFunctionDescriptorAttribute
    {
        public string FriendlyName { get; }
        public string? Description { get; }
        public bool IsFirstChance { get; }
        public string Demarcation { get; } = "{{";

        public TightWikiScopeFunctionAttribute(string friendlyName, string? description = null, bool isFirstChance = false)
        {
            FriendlyName = friendlyName;
            Description = description;
            IsFirstChance = isFirstChance;
        }
    }
}
