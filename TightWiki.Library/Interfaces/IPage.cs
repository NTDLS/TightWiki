namespace TightWiki.Library.Interfaces
{
    public interface IPage
    {
        public int Id { get; set; }
        public int Revision { get; set; }
        public int MostCurrentRevision { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Namespace { get; }
        public string Title { get; }
        public string Body { get; }
        public string Navigation { get; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsHistoricalVersion { get; }
        public bool Exists { get; }
    }
}
