namespace TightWiki.Engine.Library.Function.Attributes
{
    public interface ITightWikiFunctionDescriptorAttribute
    {
        string FriendlyName { get; }
        string? Description { get; }
        bool IsFirstChance { get; }
        string Demarcation { get; }
    }
}
