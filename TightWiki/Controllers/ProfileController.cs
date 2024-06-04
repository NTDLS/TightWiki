using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System.Security.Claims;
using TightWiki.Library;
using TightWiki.Models.ViewModels.Profile;
using TightWiki.Repository;
using TightWiki.Wiki;

namespace TightWiki.Site.Controllers
{

    [AllowAnonymous]
    [Route("[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public ProfileController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager, IWebHostEnvironment environment)
            : base(signInManager, userManager)
        {
            _environment = environment;
        }

        #region User Profile.

        /// <summary>
        /// //Gets a users avatar.
        /// </summary>
        /// <param name="navigation"></param>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet]
        [HttpGet("{userAccountName}/Avatar")]
        public ActionResult Avatar(string userAccountName)
        {
            WikiContext.RequireViewPermission();
            WikiContext.Title = $"Avatar";

            userAccountName = NamespaceNavigation.CleanAndValidate(userAccountName);
            string givenScale = Request.Query["Scale"].ToString().ToString().DefaultWhenNullOrEmpty("100");
            string givenMax = Request.Query["max"].ToString().DefaultWhenNullOrEmpty("512");
            string? givenExact = Request.Query["exact"];

            var imageBytes = UsersRepository.GetProfileAvatarByNavigation(userAccountName);

            if (imageBytes == null || imageBytes.Length == 0)
            {
                //Load the default avatar.
                var filePath = Path.Combine(_environment.WebRootPath, "Avatar.png");
                var image = Image.Load(filePath);
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                imageBytes = ms.ToArray();
            }

            if (imageBytes != null && imageBytes.Length > 0)
            {
                var img = Image.Load(new MemoryStream(imageBytes));

                int parsedScale = int.Parse(givenScale);
                int parsedMax = int.Parse(givenMax);

                if (string.IsNullOrEmpty(givenExact) == false)
                {
                    int parsedExact = int.Parse(givenExact);
                    if (parsedExact > 1024)
                    {
                        parsedExact = 1024;
                    }
                    else if (parsedExact < 16)
                    {
                        parsedExact = 16;
                    }

                    int diff = img.Width - parsedExact;
                    int width = (int)(img.Width - diff);
                    int height = (int)(img.Height - diff);

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  dimension to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both dimensions.
                    if (height < 16)
                    {
                        int difference = 16 - height;
                        height += difference;
                        width += difference;
                    }
                    if (width < 16)
                    {
                        int difference = 16 - width;
                        height += difference;
                        width += difference;
                    }

                    using var image = Images.ResizeImage(img, width, height);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
                else if (parsedMax != 0 && (img.Width > parsedMax || img.Height > parsedMax))
                {
                    int diff = img.Width - parsedMax;
                    int width = (int)(img.Width - diff);
                    int height = (int)(img.Height - diff);

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  dimension to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both dimensions.
                    if (height < 16)
                    {
                        int difference = 16 - height;
                        height += difference;
                        width += difference;
                    }
                    if (width < 16)
                    {
                        int difference = 16 - width;
                        height += difference;
                        width += difference;
                    }

                    using var image = Images.ResizeImage(img, width, height);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");

                }
                else if (parsedScale != 100)
                {
                    int width = (int)(img.Width * (parsedScale / 100.0));
                    int height = (int)(img.Height * (parsedScale / 100.0));

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  dimension to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both dimensions.
                    if (height < 16)
                    {
                        int difference = 16 - height;
                        height += difference;
                        width += difference;
                    }
                    if (width < 16)
                    {
                        int difference = 16 - width;
                        height += difference;
                        width += difference;
                    }

                    using var image = Images.ResizeImage(img, width, height);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
                else
                {
                    using var ms = new MemoryStream();
                    img.SaveAsPng(ms);
                    return File(ms.ToArray(), "image/png");
                }
            }
            else
            {
                return NotFound();
            }
        }

        /// <summary>
        /// Get user profile.
        /// </summary>
        /// <returns></returns>
        [AllowAnonymous]
        [HttpGet("Public/{userAccountName}")]
        public ActionResult Public(string userAccountName)
        {
            WikiContext.Title = $"Public Profile";

            userAccountName = NamespaceNavigation.CleanAndValidate(userAccountName);
            var profile = UsersRepository.GetAccountProfileByNavigation(userAccountName);

            if (profile == null)
            {
                return View(new PublicViewModel
                {
                    ErrorMessage = "The specified user was not found."
                });
            }

            var model = new PublicViewModel()
            {
                AccountName = profile.AccountName,
                Navigation = profile.Navigation,
                Id = profile.UserId,
                TimeZone = profile.TimeZone,
                Language = profile.Language,
                Country = profile.Country,
                Biography = WikifierLite.Process(profile.Biography),
                Avatar = profile.Avatar
            };

            model.RecentlyModified = PageRepository.GetTopRecentlyModifiedPagesInfoByUserId(profile.UserId, GlobalSettings.DefaultProfileRecentlyModifiedCount)
                .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Name).ToList();

            foreach (var item in model.RecentlyModified)
            {
                var thisRev = PageRepository.GetPageRevisionByNavigation(item.Navigation, item.Revision);
                var prevRev = PageRepository.GetPageRevisionByNavigation(item.Navigation, item.Revision - 1);
                item.ChangeSummary = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            return View(model);
        }

        /// <summary>
        /// Get user profile.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet]
        [HttpGet("My")]
        public ActionResult My()
        {
            WikiContext.RequireAuthorizedPermission();
            WikiContext.Title = $"My Profile";

            var model = new AccountProfileViewModel()
            {
                AccountProfile = AccountProfileAccountViewModel.FromDataModel(
                    UsersRepository.GetAccountProfileByUserId(WikiContext.Profile.EnsureNotNull().UserId)),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll()
            };

            model.AccountProfile.CreatedDate = WikiContext.LocalizeDateTime(model.AccountProfile.CreatedDate);
            model.AccountProfile.ModifiedDate = WikiContext.LocalizeDateTime(model.AccountProfile.ModifiedDate);

            return View(model);
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("My")]
        public ActionResult My(AccountProfileViewModel model)
        {
            WikiContext.RequireAuthorizedPermission();

            WikiContext.Title = $"My Profile";

            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();

            //Get the UserId from the logged in context because we do not trust anything from the model.
            var userId = WikiContext.Profile.EnsureNotNull().UserId;

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            var user = UserManager.FindByIdAsync(userId.ToString()).Result.EnsureNotNull();

            var profile = UsersRepository.GetAccountProfileByUserId(userId);
            if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.CurrentCultureIgnoreCase))
            {
                if (UsersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("Account.AccountName", "Account name is already in use.");
                    return View(model);
                }
            }

            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLower());

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
            {
                try
                {
                    var imageBytes = Utility.ConvertHttpFileToBytes(file);
                    var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(imageBytes));
                    UsersRepository.UpdateProfileAvatar(profile.UserId, imageBytes);
                }
                catch
                {
                    ModelState.AddModelError("Account.Avatar", "Could not save the attached image.");
                }
            }

            profile.AccountName = model.AccountProfile.AccountName;
            profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
            profile.Biography = model.AccountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            UsersRepository.UpdateProfile(profile);

            var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                    };
            SecurityHelpers.UpsertUserClaims(UserManager, user, claims);

            model.SuccessMessage = "Your profile has been saved successfully!";

            return View(model);
        }

        #endregion

        #region Delete.

        /// <summary>
        /// User is deleting their own profile.
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Delete")]
        public ActionResult Delete(DeleteAccountViewModel model)
        {
            WikiContext.RequireAuthorizedPermission();

            var profile = UsersRepository.GetBasicProfileByUserId(WikiContext.Profile.EnsureNotNull().UserId);

            bool confirmAction = bool.Parse(GetFormString("IsActionConfirmed").EnsureNotNull());
            if (confirmAction == true && profile != null)
            {
                var user = UserManager.FindByIdAsync(profile.UserId.ToString()).Result;
                if (user == null)
                {
                    return NotFound("User not found.");
                }

                var result = UserManager.DeleteAsync(user).Result;
                if (!result.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
                }

                SignInManager.SignOutAsync();

                UsersRepository.AnonymizeProfile(profile.UserId);
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));

                HttpContext.SignOutAsync(); //Do we still need this??
                return Redirect($"/Profile/Deleted");
            }

            return Redirect($"/Profile/My");
        }

        /// <summary>
        /// User is deleting their own profile.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("Delete")]
        public ActionResult Delete()
        {
            WikiContext.RequireAuthorizedPermission();
            WikiContext.Title = $"Delete Account";

            var profile = UsersRepository.GetBasicProfileByUserId(WikiContext.Profile.EnsureNotNull().UserId);

            var model = new DeleteAccountViewModel()
            {
                AccountName = profile.AccountName
            };

            if (profile != null)
            {
                WikiContext.Title = $"Delete {profile.AccountName}";
            }

            return View(model);
        }

        /// <summary>
        /// User is deleting their own profile.
        /// </summary>
        /// <returns></returns>
        [Authorize]
        [HttpGet("Deleted")]
        public ActionResult Deleted()
        {
            var model = new DeleteAccountViewModel()
            {
            };
            return View(model);
        }

        #endregion
    }
}
