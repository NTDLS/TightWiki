namespace TightWiki.Plugin.Models
{
    public class TwAccountRoleMembership
    {
        /// <summary>
        /// Id of the AccountRole table.
        /// </summary>
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int RoleId { get; set; }

        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
