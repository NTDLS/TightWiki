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
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public bool IsHistoricalVersion { get; }
    }
}
