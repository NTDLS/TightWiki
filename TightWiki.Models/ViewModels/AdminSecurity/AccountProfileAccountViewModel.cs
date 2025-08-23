using System.ComponentModel.DataAnnotations;
using TightWiki.Models.DataModels;

namespace TightWiki.Models.ViewModels.AdminSecurity
{
    public partial class AccountProfileAccountViewModel
    {
        [Display(Name = "Theme")]
        public string? Theme { get; set; } = string.Empty;
        public Guid UserId { get; set; }

        [Display(Name = "Email Address")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Resources.ValTexts))]
        public string EmailAddress { get; set; } = string.Empty;

        [Display(Name = "Display Name")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Resources.ValTexts))]
        public string AccountName { get; set; } = string.Empty;

        public string? Navigation { get; set; } = string.Empty;

        [Display(Name = "First Name")]
        public string? FirstName { get; set; }

        [Display(Name = "Last Name")]
        public string? LastName { get; set; } = string.Empty;

        [Display(Name = "Time-Zone")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Resources.ValTexts))]
        public string TimeZone { get; set; } = string.Empty;

        [Display(Name = "Country")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Resources.ValTexts))]
        public string Country { get; set; } = string.Empty;

        [Display(Name = "Language")]
        [Required(ErrorMessageResourceName = "RequiredAttribute_ValidationError", ErrorMessageResourceType = typeof(Resources.ValTexts))]
        public string Language { get; set; } = string.Empty;

        [Display(Name = "Biography")]
        public string? Biography { get; set; } = string.Empty;

        [Display(Name = "Email Confirmed?")]
        public bool EmailConfirmed { get; set; }
        public byte[]? Avatar { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime ModifiedDate { get; set; }

        public int PaginationPageSize { get; set; }
        public int PaginationPageCount { get; set; }

        public static AccountProfileAccountViewModel FromDataModel(AccountProfile model)
        {
            return new AccountProfileAccountViewModel
            {
                Theme = model.Theme,
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
                PaginationPageCount = model.PaginationPageCount,
                PaginationPageSize = model.PaginationPageSize,
                TimeZone = model.TimeZone
            };
        }
    }
}
