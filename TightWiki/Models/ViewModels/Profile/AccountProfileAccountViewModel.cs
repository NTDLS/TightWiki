using System.ComponentModel.DataAnnotations;
using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.Profile
{
    public partial class AccountProfileAccountViewModel
    {
        [Required(ErrorMessage = "UserId is required")]
        public Guid UserId { get; set; }

        [Display(Name = "Email Address")]
        public string EmailAddress { get; set; } = string.Empty;

        [Display(Name = "Account Name")]
        [Required(ErrorMessage = "Account Name is required")]
        public string AccountName { get; set; } = string.Empty;

        public string Navigation { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; } = string.Empty;

        [Display(Name = "TimeZone")]
        [Required(ErrorMessage = "TimeZone is required")]
        public string TimeZone { get; set; } = string.Empty;

        [Display(Name = "Country")]
        [Required(ErrorMessage = "Country is required")]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Language")]
        [Required(ErrorMessage = "Language is required")]
        public string Language { get; set; } = string.Empty;

        [Display(Name = "Biography")]
        public string? Biography { get; set; } = string.Empty;

        [Display(Name = "Email Confirmed?")]
        public bool EmailConfirmed { get; set; }

        public byte[]? Avatar { get; set; }

        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public int PaginationSize { get; set; }
        public int PaginationCount { get; set; }

        public string? Role { get; set; } = string.Empty;

        public static AccountProfileAccountViewModel FromDataModel(AccountProfile model)
        {
            return new AccountProfileAccountViewModel
            {
                UserId = model.UserId,
                EmailAddress = model.EmailAddress,
                AccountName = model.AccountName,
                Avatar = model.Avatar,
                Biography = model.Biography,
                Country = model.Country,
                Language = model.Language,
                CreatedDate = model.CreatedDate,
                EmailConfirmed = model.EmailConfirmed,
                FirstName = model.FirstName,
                LastName = model.LastName,
                ModifiedDate = model.ModifiedDate,
                Navigation = model.Navigation,
                PaginationCount = model.PaginationCount,
                Role = model.Role,
                PaginationSize = model.PaginationSize,
                TimeZone = model.TimeZone
            };
        }
    }
}
