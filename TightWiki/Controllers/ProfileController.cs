using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using System.Security.Claims;
using TightWiki.Controllers;
using TightWiki.Library;
using TightWiki.Library.Library;
using TightWiki.Library.Repository;
using TightWiki.Library.ViewModels.Profile;
using TightWiki.Library.ViewModels.Shared;
using TightWiki.Library.Wiki;

namespace TightWiki.Site.Controllers
{

    [AllowAnonymous]
    [Route("[controller]")]
    public class ProfileController : ControllerHelperBase
    {
        public ProfileController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
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
            context.RequireViewPermission();

            ViewBag.Context.Title = $"Avatar";

            userAccountName = NamespaceNavigation.CleanAndValidate(userAccountName);
            string scale = Request.Query["Scale"].ToString().ToString().DefaultWhenNullOrEmpty("100");
            string max = Request.Query["max"].ToString().DefaultWhenNullOrEmpty("512");
            string? exact = Request.Query["exact"];

            var imageBytes = ProfileRepository.GetProfileAvatarByNavigation(userAccountName);

            if (imageBytes == null || imageBytes.Count() == 0)
            {
                //Load the default avatar.
                var image = Image.Load("Avatar.png");
                using var ms = new MemoryStream();
                image.SaveAsPng(ms);
                imageBytes = ms.ToArray();
            }

            if (imageBytes != null && imageBytes.Count() > 0)
            {
                var img = Image.Load(new MemoryStream(imageBytes));

                int iScale = int.Parse(scale);
                int iMax = int.Parse(max);

                if (string.IsNullOrEmpty(exact) == false)
                {
                    int iexact = int.Parse(exact);
                    if (iexact > 1024)
                    {
                        iexact = 1024;
                    }
                    else if (iexact < 16)
                    {
                        iexact = 16;
                    }

                    int diff = img.Width - iexact;
                    int width = (int)(img.Width - diff);
                    int height = (int)(img.Height - diff);

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  deminsion to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both demensions.
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
                else if (iMax != 0 && (img.Width > iMax || img.Height > iMax))
                {
                    int diff = img.Width - iMax;
                    int width = (int)(img.Width - diff);
                    int height = (int)(img.Height - diff);

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  deminsion to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both demensions.
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
                else if (iScale != 100)
                {
                    int width = (int)(img.Width * (iScale / 100.0));
                    int height = (int)(img.Height * (iScale / 100.0));

                    //Adjusting by a ratio (and especially after applying additional scaling) may have caused one
                    //  deminsion to become very small (or even negative). So here we will check the height and width
                    //  to ensure they are both at least n pixels and adjust both demensions.
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
            ViewBag.Context.Title = $"Public Profile";

            userAccountName = NamespaceNavigation.CleanAndValidate(userAccountName);
            var profile = ProfileRepository.GetAccountProfileByNavigation(userAccountName);

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
                item.ChangeSummary = Differentiator.GetComparisionSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
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
            context.RequireAuthorizedPermission();
            ViewBag.Context.Title = $"Profile";

            var model = new AccountProfileViewModel()
            {
                AccountProfile = AccountProfileAccountViewModel.FromDataModel(
                    ProfileRepository.GetAccountProfileByUserId(context.User.EnsureNotNull().UserId)),
                Credential = new CredentialViewModel(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll()
            };

            model.AccountProfile.CreatedDate = context.LocalizeDateTime(model.AccountProfile.CreatedDate);
            model.AccountProfile.ModifiedDate = context.LocalizeDateTime(model.AccountProfile.ModifiedDate);

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
            context.RequireAuthorizedPermission();

            ViewBag.Context.Title = $"Profile";

            //Get the UserId from the logged in context because we do not trust anyhting from the model.
            var userId = context.User.EnsureNotNull().UserId;

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            var user = UserManager.FindByIdAsync(userId.ToString()).Result.EnsureNotNull();

            if (model.Credential.Password != CredentialViewModel.NOTSET && model.Credential.Password == model.Credential.ComparePassword)
            {
                try
                {
                    var token = UserManager.GeneratePasswordResetTokenAsync(user).Result.EnsureNotNull();
                    var result = UserManager.ResetPasswordAsync(user, token, model.Credential.Password).Result.EnsureNotNull();
                    if (!result.Succeeded)
                    {
                        throw new Exception(string.Join("<br />\r\n", result.Errors.Select(o => o.Description)));
                    }
                }
                catch (Exception ex)
                {
                    model.ErrorMessage = ex.Message;
                    return View(model);
                }
            }

            var profile = ProfileRepository.GetAccountProfileByUserId(userId);
            if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.CurrentCultureIgnoreCase))
            {
                if (ProfileRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("Account.AccountName", "Account name is already in use.");
                    return View(model);
                }
            }

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                if (ProfileRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
                {
                    ModelState.AddModelError("Account.EmailAddress", "Email address is already in use.");
                    return View(model);
                }
            }

            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();

            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLower());

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
            {
                try
                {
                    var imageBytes = Utility.ConvertHttpFileToBytes(file);
                    var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(imageBytes));
                    ProfileRepository.UpdateProfileAvatar(profile.UserId, imageBytes);
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
            ProfileRepository.UpdateProfile(profile);

            var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                    };
            SecurityHelpers.UpsertUserClaims(UserManager, user, claims);

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                var setEmailResult = UserManager.SetEmailAsync(user, model.AccountProfile.EmailAddress).Result;
                if (!setEmailResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", setEmailResult.Errors.Select(o => o.Description)));
                }

                var setUserNameResult = UserManager.SetUserNameAsync(user, model.AccountProfile.EmailAddress).Result;
                if (!setUserNameResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", setUserNameResult.Errors.Select(o => o.Description)));
                }
            }

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
        public ActionResult Delete(AccountViewModel model)
        {
            context.RequireAuthorizedPermission();

            var profile = ProfileRepository.GetBasicProfileByUserId(context.User.EnsureNotNull().UserId);

            bool confirmAction = bool.Parse(GetFormString("Action").EnsureNotNull());
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

                ProfileRepository.AnonymizeProfile(profile.UserId);
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
            context.RequireAuthorizedPermission();

            var profile = ProfileRepository.GetBasicProfileByUserId(context.User.EnsureNotNull().UserId);

            ViewBag.AccountName = profile.AccountName;

            var model = new AccountViewModel()
            {
            };

            if (profile != null)
            {
                ViewBag.Context.Title = $"{profile.AccountName} Delete";
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
            var model = new AccountViewModel()
            {
            };
            return View(model);
        }

        #endregion
    }
}
