namespace TightWiki.Library.Api
{
    /// <summary>
    /// One page of a list that is split into pages.
    /// </summary>
    public class ApiPagedList<T>
    {
        public int PageNumber { get; set; }
        public int PageCount { get; set; }
        public List<T> Items { get; set; } = new();
    }
}
