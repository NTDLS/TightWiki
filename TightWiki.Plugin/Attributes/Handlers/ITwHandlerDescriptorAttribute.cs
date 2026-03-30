namespace TightWiki.Plugin.Attributes.Functions
{
    public interface ITwHandlerDescriptorAttribute
    {
        string FriendlyName { get; }
        string? Description { get; }
    }
}
