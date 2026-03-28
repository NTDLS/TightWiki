namespace TightWiki.Engine.Library.Attributes
{
    public interface ITightWikiFunctionPrototypeAttribute
    {
        string FriendlyName { get; }
        string? Description { get; }
        bool IsFirstChance { get; }
        string Demarcation { get; }
    }
}
