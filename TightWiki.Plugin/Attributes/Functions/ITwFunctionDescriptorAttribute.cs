namespace TightWiki.Plugin.Attributes.Functions
{
    public interface ITwFunctionDescriptorAttribute
    {
        string FriendlyName { get; }
        string? Description { get; }
        bool IsFirstChance { get; }
        string Demarcation { get; }
    }
}
