namespace TightWiki.Library.Interfaces
{
    public interface IAccountProfile
    {
        public string Role { get; set; }
        public Guid UserId { get; set; }
        public string EmailAddress { get; set; }
        public string AccountName { get; set; }
        public string Navigation { get; set; }
        public string? Theme { get; set; }
        public string TimeZone { get; set; }
    }
}
