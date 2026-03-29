namespace TightWiki.Plugin.Attributes
{
    public interface ITwFunctionDescriptorAttribute
    {
        string FriendlyName { get; }
        string? Description { get; }
        bool IsFirstChance { get; }
        string Demarcation { get; }
    }
}
