namespace TightWiki.Plugin.Interfaces
{
    public interface ITwPage
    {
        int Id { get; }
        int Revision { get; }
        int MostCurrentRevision { get; }
        string Name { get; set; }
        string Description { get; }
        string Namespace { get; }
        string Title { get; }
        string Body { get; }
        string Navigation { get; }
        DateTime CreatedDate { get; }
        DateTime ModifiedDate { get; }
        string ModifiedByUserName { get; }
        string CreatedByUserName { get; }
        bool IsHistoricalVersion { get; }
        bool Exists { get; }
        int TotalViewCount { get; set; }
    }
}
