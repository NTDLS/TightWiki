using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using NTDLS.Helpers;
using SixLabors.ImageSharp;
using System.Security.Claims;
using TightWiki.Caching;
using TightWiki.Engine;
using TightWiki.Engine.Implementation.Utility;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Models.ViewModels.Profile;
using TightWiki.Models.ViewModels.Utility;
using TightWiki.Repository;
using static TightWiki.Library.Images;

namespace TightWiki.Controllers
{
    [Route("[controller]")]
    public class ProfileController(ILogger<ITightEngine> logger, SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager, IWebHostEnvironment environment, ISharedLocalizationText localizer,
        TightWikiConfiguration wikiConfiguration)
        : WikiControllerBase<ProfileController>(logger, signInManager, userManager, localizer, wikiConfiguration)
    {
        private readonly IWebHostEnvironment _environment = environment;

        #region User Profile.

        /// <summary>
        /// //Gets a users avatar.
        /// </summary>
        [AllowAnonymous]
        [HttpGet]
        [HttpGet("{userAccountName}/Avatar")]
        public async Task<ActionResult> Avatar(string userAccountName)
        {
            try
            {
                //TODO: Do we need to check permissions here?
                //SessionState.RequireViewPermission();
                SessionState.Page.Name = Localize("Avatar");

                string givenScale = Request.Query["Scale"].ToString().ToString().DefaultWhenNullOrEmpty("100");
                string givenMax = Request.Query["max"].ToString().DefaultWhenNullOrEmpty("512");
                string? givenExact = Request.Query["exact"];

                ProfileAvatar? avatar;
                if (WikiConfiguration.EnablePublicProfiles)
                {
                    avatar = await UsersRepository.GetProfileAvatarByNavigation(NamespaceNavigation.CleanAndValidate(userAccountName)) ?? new ProfileAvatar();
                }
                else
                {
                    avatar = new ProfileAvatar();
                }

                if (avatar.Bytes == null || avatar.Bytes.Length == 0)
                {
                    //Load the default avatar.
                    var filePath = Path.Combine(_environment.WebRootPath, "Avatar.png");
                    var image = Image.Load(filePath);
                    using var ms = new MemoryStream();
                    image.SaveAsPng(ms);
                    avatar.ContentType = "image/png";
                    avatar.Bytes = ms.ToArray();
                }

                if (avatar.Bytes != null && avatar.Bytes.Length > 0)
                {
                    if (avatar.ContentType == "image/x-icon")
                    {
                        //We do not handle the resizing of icon file. Maybe later....
                        return File(avatar.Bytes, avatar.ContentType);
                    }

                    var img = Image.Load(new MemoryStream(avatar.Bytes));

                    int width = img.Width;
                    int height = img.Height;

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
                        width = (int)(img.Width - diff);
                        height = (int)(img.Height - diff);

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
                    }
                    else if (parsedMax != 0 && (img.Width > parsedMax || img.Height > parsedMax))
                    {
                        int diff = img.Width - parsedMax;
                        width = (int)(img.Width - diff);
                        height = (int)(img.Height - diff);

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
                    }
                    else if (parsedScale != 100)
                    {
                        width = (int)(img.Width * (parsedScale / 100.0));
                        height = (int)(img.Height * (parsedScale / 100.0));

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
                    }
                    else
                    {
                        return File(avatar.Bytes, avatar.ContentType);
                    }

                    if (avatar.ContentType.Equals("image/gif", StringComparison.InvariantCultureIgnoreCase))
                    {
                        var resized = ResizeGifImage(avatar.Bytes, width, height);
                        return File(resized, "image/gif");
                    }
                    else
                    {
                        using var image = ResizeImage(img, width, height);
                        using var ms = new MemoryStream();
                        string contentType = BestEffortConvertImage(image, ms, avatar.ContentType);
                        return File(ms.ToArray(), contentType);
                    }
                }
                else
                {
                    return NotFound();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving avatar for user {UserAccountName}", userAccountName);
                throw;
            }
        }

        /// <summary>
        /// Get user profile.
        /// </summary>
        [AllowAnonymous]
        [HttpGet("{userAccountName}/Public")]
        public async Task<ActionResult> Public(string userAccountName)
        {
            try
            {
                SessionState.Page.Name = Localize("Public Profile");

                userAccountName = NamespaceNavigation.CleanAndValidate(userAccountName);

                if (!WikiConfiguration.EnablePublicProfiles)
                {
                    return View(new PublicViewModel
                    {
                        ErrorMessage = Localize("Public profiles are disabled.")
                    });
                }

                var accountProfile = await UsersRepository.GetAccountProfileByNavigation(userAccountName);
                if (accountProfile == null)
                {
                    return View(new PublicViewModel
                    {
                        ErrorMessage = Localize("The specified user was not found.")
                    });
                }

                var model = new PublicViewModel()
                {
                    AccountName = accountProfile.AccountName,
                    Navigation = accountProfile.Navigation,
                    Id = accountProfile.UserId,
                    TimeZone = accountProfile.TimeZone,
                    Language = accountProfile.Language,
                    Country = accountProfile.Country,
                    Biography = WikifierLite.Process(WikiConfiguration, accountProfile.Biography),
                    Avatar = accountProfile.Avatar
                };

                model.RecentlyModified = (await PageRepository.GetTopRecentlyModifiedPagesInfoByUserId(accountProfile.UserId, WikiConfiguration.DefaultProfileRecentlyModifiedCount))
                    .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Name).ToList();

                foreach (var item in model.RecentlyModified)
                {
                    var thisRev = await PageRepository.GetPageRevisionByNavigation(item.Navigation, item.Revision);
                    var prevRev = await PageRepository.GetPageRevisionByNavigation(item.Navigation, item.Revision - 1);
                    item.ChangeAnalysis = Differentiator.GetComparisonSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
                }

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving public profile for user {UserAccountName}", userAccountName);
                throw;
            }
        }

        /// <summary>
        /// Get user profile.
        /// </summary>
        [Authorize]
        [HttpGet]
        [HttpGet("My")]
        public async Task<ActionResult> My()
        {
            try
            {
                try
                {
                    await SessionState.RequireAuthorizedPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("My Profile");

                var model = new AccountProfileViewModel()
                {
                    AccountProfile = AccountProfileAccountViewModel.FromDataModel(
                         await UsersRepository.GetAccountProfileByUserId(SessionState.Profile.EnsureNotNull().UserId)),

                    Themes = await ConfigurationRepository.GetAllThemes(),
                    TimeZones = TimeZoneItem.GetAll(),
                    Countries = CountryItem.GetAll(),
                    Languages = LanguageItem.GetAll()
                };

                model.AccountProfile.CreatedDate = SessionState.LocalizeDateTime(model.AccountProfile.CreatedDate);
                model.AccountProfile.ModifiedDate = SessionState.LocalizeDateTime(model.AccountProfile.ModifiedDate);

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error retrieving profile for user {UserId}", SessionState.Profile?.UserId);
                throw;
            }
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        [Authorize]
        [HttpPost("My")]
        public async Task<ActionResult> My(AccountProfileViewModel model)
        {
            try
            {
                try
                {
                    await SessionState.RequireAuthorizedPermission();
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("My Profile");

                model.TimeZones = TimeZoneItem.GetAll();
                model.Countries = CountryItem.GetAll();
                model.Languages = LanguageItem.GetAll();
                model.Themes = await ConfigurationRepository.GetAllThemes();

                //Get the UserId from the logged in context because we do not trust anything from the model.
                var userId = SessionState.Profile.EnsureNotNull().UserId;

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                var user = UserManager.FindByIdAsync(userId.ToString()).Result.EnsureNotNull();

                var profile = await UsersRepository.GetAccountProfileByUserId(userId);
                if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.InvariantCultureIgnoreCase))
                {
                    if (await UsersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                    {
                        ModelState.AddModelError("Account.AccountName", Localize("Account name is already in use."));
                        return View(model);
                    }
                }

                model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLowerInvariant());

                var file = Request.Form.Files["Avatar"];
                if (file != null && file.Length > 0)
                {
                    if (WikiConfiguration.AllowableImageTypes.Contains(file.ContentType.ToLowerInvariant()) == false)
                    {
                        model.ErrorMessage += Localize("Could not save the attached image, type not allowed.") + "\r\n";
                    }
                    else if (file.Length > WikiConfiguration.MaxAvatarFileSize)
                    {
                        model.ErrorMessage += Localize("Could not save the attached image, too large.") + "\r\n";
                    }
                    else
                    {
                        try
                        {
                            var imageBytes = Utility.ConvertHttpFileToBytes(file);
                            var image = Utility.CropImageToCenteredSquare(new MemoryStream(imageBytes));
                            await UsersRepository.UpdateProfileAvatar(profile.UserId, image, "image/webp");
                        }
                        catch
                        {
                            ModelState.AddModelError("Account.Avatar", Localize("Could not save the attached image."));
                        }
                    }
                }

                profile.AccountName = model.AccountProfile.AccountName;
                profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
                profile.Biography = model.AccountProfile.Biography;
                profile.ModifiedDate = DateTime.UtcNow;
                await UsersRepository.UpdateProfile(profile);

                var claims = new List<Claim>
                    {
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
                await SecurityRepository.UpsertUserClaims(UserManager, user, claims);

                await SignInManager.RefreshSignInAsync(user);
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.UserId]));

                await UpdateUserCultureCookie(userId);

                //Redirect so that the language and theme are applied immediately.
                return LocalRedirect($"{WikiConfiguration.BasePath}/Profile/My?SuccessMessage={Localize("Your profile has been saved.")}");
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error saving profile for user {UserId}", SessionState.Profile?.UserId);
                throw;
            }
        }

        #endregion

        #region Delete.

        /// <summary>
        /// User is deleting their own profile.
        /// </summary>
        [Authorize]
        [HttpPost("Delete")]
        public async Task<ActionResult> DeleteAccount(ConfirmActionViewModel model)
        {
            try
            {
                await SessionState.RequireAuthorizedPermission();
            }
            catch (Exception ex)
            {
                return NotifyOfError(ex.GetBaseException().Message, "/");
            }
            var profile = await UsersRepository.GetBasicProfileByUserId(SessionState.Profile.EnsureNotNull().UserId);

            if (model.UserSelection == true && profile != null)
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

                await SignInManager.SignOutAsync();

                await UsersRepository.AnonymizeProfile(profile.UserId);
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));

                await HttpContext.SignOutAsync(); //Do we still need this??
                return NotifyOfSuccess(Localize("Your account has been deleted."), $"/Profile/Deleted");
            }

            return Redirect($"{WikiConfiguration.BasePath}{model.NoRedirectURL}");
        }

        /// <summary>
        /// User is deleting their own profile.
        /// </summary>
        [Authorize]
        [HttpGet("Deleted")]
        public async Task<ActionResult> Deleted()
        {
            var model = new DeletedAccountViewModel()
            {
            };
            return View(model);
        }

        #endregion

        [NonAction]
        private async Task UpdateUserCultureCookie(Guid userId)
        {

            try
            {
                var profile = await UsersRepository.GetBasicProfileByUserId(userId);
                if (profile != null)
                {
                    Response.Cookies.Append(CookieRequestCultureProvider.DefaultCookieName,
                            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(profile.Language)),
                        new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddYears(1),
                            IsEssential = true,
                            SameSite = SameSiteMode.Lax,
                            Secure = true,
                            HttpOnly = false
                        }
                    );
                }
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error updating user culture cookie");
            }
        }
    }
}
