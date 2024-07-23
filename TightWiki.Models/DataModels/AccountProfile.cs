using TightWiki.Library.Interfaces;

namespace TightWiki.Models.DataModels
{
    /// <summary>
    /// This model combines the elements of an account and a profile into one class.
    /// </summary>
    public partial class AccountProfile : IAccountProfile
    {
        public string? Theme { get; set; }
        public Guid UserId { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string AccountName { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;
        public string? FirstName { get; set; }
        public string? LastName { get; set; } = string.Empty;
        public string TimeZone { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public string? Biography { get; set; } = string.Empty;
        public bool EmailConfirmed { get; set; }
        public byte[]? Avatar { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }
        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }
        public string Role { get; set; } = string.Empty;
    }
}
