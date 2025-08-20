namespace TightWiki.Models.DataModels
{
    public partial class AccountRoleMembership
    {
        /// <summary>
        /// Id of the AccountRole table.
        /// </summary>
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
    }
}
