using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace TightWiki.Pages.Admin
{
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    [IgnoreAntiforgeryToken]
    public class AccountsModel(SignInManager<IdentityUser> signInManager
        /*, ILogger<AccountsModel> logger*/)
        : PageModelBase(signInManager)
    {
        [BindProperty]
        public string SearchString { get; set; } = string.Empty;
    }
}
