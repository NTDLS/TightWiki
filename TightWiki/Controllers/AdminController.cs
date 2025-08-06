using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using TightWiki.Repository;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminController(
        //ITightEngine tightEngine,
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<AdminController> localizer)
    : WikiControllerBase<AdminController>(signInManager, userManager, localizer)
    {
        [Authorize]
        [HttpGet("AccountsPaged")]
        public ActionResult AccountsPaged(
            [FromQuery] int page = 1,
            [FromQuery] int? size = null,
            [FromQuery(Name = "sort[0][field]")] string? orderBy = null,
            [FromQuery(Name = "sort[0][dir]")] string? orderByDirection = null,
            [FromQuery] string searchString = "")
        {
            SessionState.RequireAdminPermission();

            var users = UsersRepository.GetAllUsersPaged(page, orderBy, orderByDirection, searchString, size);

            users.ForEach(o =>
            {
                o.CreatedDate = SessionState.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = SessionState.LocalizeDateTime(o.ModifiedDate);
            });

            var result = new
            {
                data = users,
                last_page = users.FirstOrDefault()?.PaginationPageCount ?? 0
            };

            return Json(result);
        }

    }
}
