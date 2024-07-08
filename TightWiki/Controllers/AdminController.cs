using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Reflection;
using System.Security.Claims;
using TightWiki.Controllers;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Models.ViewModels.Admin;
using TightWiki.Models.ViewModels.Page;
using TightWiki.Models.ViewModels.Profile;
using TightWiki.Models.ViewModels.Shared;
using TightWiki.Models.ViewModels.Utility;
using TightWiki.Repository;
using TightWiki.Wiki;
using TightWiki.Wiki.Function;
using static TightWiki.Library.Constants;
using Constants = TightWiki.Library.Constants;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminController : WikiControllerBase
    {
        public AdminController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }

        #region Statistics.

        [Authorize]
        [HttpGet("Statistics")]
        public ActionResult Statistics()
        {
            WikiContext.RequireAdminPermission();
            WikiContext.Title = $"Statistics";

            Assembly assembly = Assembly.GetEntryAssembly().EnsureNotNull();
            Version version = assembly.GetName().Version.EnsureNotNull();

            var model = new StatisticsViewModel()
            {
                Statistics = ConfigurationRepository.GetWikiDatabaseStatistics(),
                ApplicationVersion = version.ToString()
            };

            return View(model);
        }

        #endregion

        #region Moderate.

        [Authorize]
        [HttpGet("Moderate")]
        public ActionResult Moderate()
        {
            WikiContext.RequireModeratePermission();
            WikiContext.Title = $"Page Moderation";

            var instruction = GetQueryString("Instruction");
            if (instruction != null)
            {
                var model = new PageModerateViewModel()
                {
                    Pages = PageRepository.GetAllPagesByInstructionPaged(GetQueryString("page", 1), null, instruction),
                    Instruction = instruction,
                    Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
                };

                model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

                if (model.Pages != null && model.Pages.Count > 0)
                {
                    model.Pages.ForEach(o =>
                    {
                        o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                        o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                    });
                }

                return View(model);
            }

            return View(new PageModerateViewModel()
            {
                Pages = new List<Page>(),
                Instruction = string.Empty,
                Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
            });
        }

        #endregion

        #region Missing Pages.

        [Authorize]
        [HttpGet("MissingPages")]
        public ActionResult MissingPages()
        {
            WikiContext.RequireModeratePermission();
            WikiContext.Title = $"Missing Pages";

            var model = new MissingPagesViewModel()
            {
                Pages = PageRepository.GetMissingPagesPaged(GetQueryString("page", 1))
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        #endregion

        #region Namespaces.

        [Authorize]
        [HttpGet("Namespaces")]
        public ActionResult Namespaces()
        {
            WikiContext.RequireModeratePermission();
            WikiContext.Title = $"Namespaces";

            var model = new NamespacesViewModel()
            {
                Namespaces = PageRepository.GetAllNamespacesPaged(GetQueryString("page", 1)),
            };

            model.PaginationPageCount = (model.Namespaces.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        #endregion

        #region Pages.

        [Authorize]
        [HttpGet("Pages")]
        public ActionResult Pages()
        {
            WikiContext.RequireModeratePermission();
            WikiContext.Title = $"Pages";

            var searchString = GetQueryString("Tokens");

            var model = new PagesViewModel()
            {
                Pages = PageRepository.GetAllPagesPaged(GetQueryString("page", 1), null, Utility.SplitToTokens(searchString)),
                SearchString = searchString ?? string.Empty
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            if (model.Pages != null && model.Pages.Count > 0)
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                });
            }

            return View(model);
        }

        #endregion

        #region Deleted Pages.

        [Authorize]
        [HttpGet("DeletedPage/{pageId}")]
        public ActionResult DeletedPage(int pageId)
        {
            WikiContext.RequireModeratePermission();

            var model = new DeletedPageViewModel();

            var page = PageRepository.GetDeletedPageById(pageId);

            if (page != null)
            {
                var wiki = new Wikifier(WikiContext, page, null, Request.Query);
                model.PageId = pageId;
                model.Body = wiki.ProcessedBody;
                model.DeletedDate = WikiContext.LocalizeDateTime(page.ModifiedDate);
                model.DeletedByUserName = page.DeletedByUserName;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet("DeletedPages")]
        public ActionResult DeletedPages()
        {
            WikiContext.RequireModeratePermission();

            string searchString = GetQueryString("SearchString") ?? string.Empty;
            var model = new DeletedPagesViewModel()
            {
                Pages = PageRepository.GetAllDeletedPagesPaged(GetQueryString("page", 1), null, Utility.SplitToTokens(searchString)),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Pages.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpPost("PurgeDeletedPages")]
        public ActionResult PurgeDeletedPages(ConfirmActionViewModel model)
        {
            WikiContext.RequireModeratePermission();

            if (model.UserSelection == true)
            {
                PageRepository.PurgeDeletedPages();
                return Redirect(model.YesRedirectURL);
            }

            return Redirect(model.NoRedirectURL);
        }

        [Authorize]
        [HttpPost("RestoreDeletedPage")]
        public ActionResult RestoreDeletedPage(ConfirmActionViewModel model)
        {
            WikiContext.RequireModeratePermission();

            if (model.UserSelection == true)
            {
                var pageId = int.Parse(model.Parameter.EnsureNotNull());
                PageRepository.RestoreDeletedPageByPageId(pageId);
                var page = PageRepository.GetPageById(pageId);
                if (page != null)
                {
                    PageController.RefreshPageMetadata(this, page);
                }
                return Redirect(model.YesRedirectURL);
            }

            return Redirect(model.NoRedirectURL);
        }

        #endregion

        #region Utilities.

        [Authorize]
        [HttpGet("Utilities")]
        public ActionResult Utilities()
        {
            WikiContext.RequireAdminPermission();

            var model = new UtilitiesViewModel()
            {
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("Utilities")]
        public ActionResult Utilities(UtilitiesViewModel model)
        {
            WikiContext.RequireAdminPermission();

            if (model.UserSelection == false)
            {
                return View(model);
            }

            switch (model.Parameter?.ToLower())
            {
                case "rebuildallpages":
                    {
                        foreach (var page in PageRepository.GetAllPages())
                        {
                            PageController.RefreshPageMetadata(this, page);
                        }
                    }
                    break;
                case "truncatepagerevisions":
                    {
                        PageRepository.TruncateAllPageRevisions("YES");
                        WikiCache.Clear();
                    }
                    break;
                case "flushmemorycache":
                    {
                        WikiCache.Clear();
                    }
                    break;
                case "createselfdocumentation":
                    {
                        SelfDocument.CreateNotExisting();
                    }
                    break;
            }

            return View(model);
        }

        #endregion

        #region Menu Items.

        [Authorize]
        [HttpGet("MenuItems")]
        public ActionResult MenuItems()
        {
            WikiContext.RequireAdminPermission();

            var model = new MenuItemsViewModel()
            {
                Items = ConfigurationRepository.GetAllMenuItems()
            };

            return View(model);
        }

        [Authorize]
        [HttpGet("MenuItem/{id:int?}")]
        public ActionResult MenuItem(int? id)
        {
            WikiContext.RequireAdminPermission();
            WikiContext.Title = $"Menu Item";

            if (id != null)
            {
                var menuItem = ConfigurationRepository.GetMenuItemById((int)id);
                return View(menuItem.ToViewModel());
            }
            else
            {
                var model = new MenuItemViewModel
                {
                    Link = "/"
                };
                return View(model);
            }

        }

        /// <summary>
        /// Save site menu item.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("MenuItem/{id:int?}")]
        public ActionResult MenuItem(int? id, MenuItemViewModel model)
        {
            WikiContext.RequireAdminPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            if (ConfigurationRepository.GetAllMenuItems().Where(o => o.Name.ToLower() == model.Name.ToLower() && o.Id != model.Id).Any())
            {
                ModelState.AddModelError("Name", $"The menu name '{model.Name}' is already in use.");
                return View(model);
            }

            if (id.DefaultWhenNull(0) == 0)
            {
                model.Id = ConfigurationRepository.InsertMenuItem(model.ToDataModel());
                ModelState.Clear();

                return NotifyOfSuccessAction("The menu item has been created successfully!", $"/Admin/MenuItem/{model.Id}");
            }
            else
            {
                ConfigurationRepository.UpdateMenuItemById(model.ToDataModel());
            }

            model.SuccessMessage = "The menu item has been saved successfully!";
            return View(model);
        }

        [Authorize]
        [HttpGet("DeleteMenuItem/{id}")]
        public ActionResult DeleteMenuItem(int id)
        {
            WikiContext.RequireAdminPermission();

            var model = ConfigurationRepository.GetMenuItemById(id);
            WikiContext.Title = $"{model.Name} Delete";

            return View(model.ToViewModel());
        }

        [Authorize]
        [HttpPost("DeleteMenuItem/{id}")]
        public ActionResult DeleteMenuItem(MenuItemViewModel model)
        {
            WikiContext.RequireAdminPermission();

            bool confirmAction = bool.Parse(GetFormString("IsActionConfirmed").EnsureNotNull());
            if (confirmAction == true)
            {
                ConfigurationRepository.DeleteMenuItemById(model.Id);

                return NotifyOfSuccessAction("The menu item has been deleted successfully!", $"/Admin/MenuItems");
            }

            return Redirect($"/Admin/MenuItem/{model.Id}");
        }

        #endregion

        #region Roles.

        [Authorize]
        [HttpGet("Role/{navigation}")]
        public ActionResult Role(string navigation)
        {
            WikiContext.RequireAdminPermission();
            WikiContext.Title = $"Roles";

            navigation = Navigation.Clean(navigation);

            var role = UsersRepository.GetRoleByName(navigation);

            var model = new RoleViewModel()
            {
                Id = role.Id,
                Name = role.Name,
                Users = UsersRepository.GetProfilesByRoleIdPaged(role.Id, GetQueryString("page", 1))
            };

            model.PaginationPageCount = (model.Users.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Roles")]
        public ActionResult Roles()
        {
            WikiContext.RequireAdminPermission();

            var model = new RolesViewModel()
            {
                Roles = UsersRepository.GetAllRoles()
            };

            return View(model);
        }

        #endregion

        #region Accounts

        [Authorize]
        [HttpGet("Account/{navigation}")]
        public ActionResult Account(string navigation)
        {
            WikiContext.RequireAdminPermission();

            var model = new Models.ViewModels.Admin.AccountProfileViewModel()
            {
                AccountProfile = Models.ViewModels.Admin.AccountProfileAccountViewModel.FromDataModel(
                    UsersRepository.GetAccountProfileByNavigation(Navigation.Clean(navigation))),
                Credential = new CredentialViewModel(),
                Themes = ConfigurationRepository.GetAllThemes(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = UsersRepository.GetAllRoles()
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
        [HttpPost("Account/{navigation}")]
        public ActionResult Account(string navigation, Models.ViewModels.Admin.AccountProfileViewModel model)
        {
            WikiContext.RequireAdminPermission();

            model.Themes = ConfigurationRepository.GetAllThemes();
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = UsersRepository.GetAllRoles();
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLower());

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            var user = UserManager.FindByIdAsync(model.AccountProfile.UserId.ToString()).Result.EnsureNotNull();

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

                    if (model.AccountProfile.AccountName.Equals(Constants.DEFAULTACCOUNT, StringComparison.CurrentCultureIgnoreCase))
                    {
                        UsersRepository.SetAdminPasswordIsChanged();
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Credential.Password", ex.Message);
                    return View(model);
                }
            }

            var profile = UsersRepository.GetAccountProfileByUserId(model.AccountProfile.UserId);
            if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.CurrentCultureIgnoreCase))
            {
                if (UsersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("AccountProfile.AccountName", "Account name is already in use.");
                    return View(model);
                }
            }

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                if (UsersRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
                {
                    ModelState.AddModelError("AccountProfile.EmailAddress", "Email address is already in use.");
                    return View(model);
                }
            }

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
                    model.ErrorMessage += "Could not save the attached image.";
                }
            }

            profile.AccountName = model.AccountProfile.AccountName;
            profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
            profile.Biography = model.AccountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            UsersRepository.UpdateProfile(profile);

            var claims = new List<Claim>
                    {
                        new (ClaimTypes.Role, model.AccountProfile.Role),
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
            SecurityHelpers.UpsertUserClaims(UserManager, user, claims);

            //Allow the administrator to confirm/unconfirm the email address.
            bool emailConfirmChanged = profile.EmailConfirmed != model.AccountProfile.EmailConfirmed;
            if (emailConfirmChanged)
            {
                user.EmailConfirmed = model.AccountProfile.EmailConfirmed;
                var updateResult = UserManager.UpdateAsync(user).Result;
                if (!updateResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", updateResult.Errors.Select(o => o.Description)));
                }
            }

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                bool wasEmailAlreadyConfirmed = user.EmailConfirmed;

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

                //If the email address was already confirmed, just keep the status. Afterall, this is an admin making the change.
                if (wasEmailAlreadyConfirmed && emailConfirmChanged == false)
                {
                    user.EmailConfirmed = true;
                    var updateResult = UserManager.UpdateAsync(user).Result;
                    if (!updateResult.Succeeded)
                    {
                        throw new Exception(string.Join("<br />\r\n", updateResult.Errors.Select(o => o.Description)));
                    }
                }
            }

            model.SuccessMessage = "Your profile has been saved successfully!";

            return View(model);
        }

        [Authorize]
        [HttpGet("AddAccount")]
        public ActionResult AddAccount()
        {
            WikiContext.RequireAdminPermission();

            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
            var defaultSignupRole = membershipConfig.Value<string>("Default Signup Role").EnsureNotNull();
            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Customization");

            var model = new Models.ViewModels.Admin.AccountProfileViewModel()
            {
                AccountProfile = new Models.ViewModels.Admin.AccountProfileAccountViewModel
                {
                    AccountName = UsersRepository.GetRandomUnusedAccountName(),
                    Country = customizationConfig.Value<string>("Default Country", string.Empty),
                    TimeZone = customizationConfig.Value<string>("Default TimeZone", string.Empty),
                    Language = customizationConfig.Value<string>("Default Language", string.Empty),
                    Role = defaultSignupRole
                },
                Themes = ConfigurationRepository.GetAllThemes(),
                Credential = new CredentialViewModel(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = UsersRepository.GetAllRoles()
            };

            return View(model);
        }

        /// <summary>
        /// Create a new user profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddAccount")]
        public ActionResult AddAccount(Models.ViewModels.Admin.AccountProfileViewModel model)
        {
            WikiContext.RequireAdminPermission();

            model.Themes = ConfigurationRepository.GetAllThemes();
            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = UsersRepository.GetAllRoles();
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLower());

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            if (UsersRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
            {
                ModelState.AddModelError("AccountProfile.AccountName", "Account name is already in use.");
                return View(model);
            }

            if (UsersRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
            {
                ModelState.AddModelError("AccountProfile.EmailAddress", "Email address is already in use.");
                return View(model);
            }

            Guid? userId;

            try
            {
                //Define the new user:
                var identityUser = new IdentityUser(model.AccountProfile.EmailAddress)
                {
                    Email = model.AccountProfile.EmailAddress,
                    EmailConfirmed = true
                };

                //Create the new user:
                var creationResult = UserManager.CreateAsync(identityUser, model.Credential.Password).Result;
                if (!creationResult.Succeeded)
                {
                    throw new Exception(string.Join("<br />\r\n", creationResult.Errors.Select(o => o.Description)));
                }
                identityUser = UserManager.FindByEmailAsync(model.AccountProfile.EmailAddress).Result.EnsureNotNull();

                userId = Guid.Parse(identityUser.Id);

                //Insert the claims.
                var claims = new List<Claim>
                    {
                        new (ClaimTypes.Role, model.AccountProfile.Role),
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
                        new ("theme", model.AccountProfile.Theme ?? ""),
                    };
                SecurityHelpers.UpsertUserClaims(UserManager, identityUser, claims);
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
                return View(model);
            }

            UsersRepository.CreateProfile((Guid)userId, model.AccountProfile.AccountName);
            var profile = UsersRepository.GetAccountProfileByUserId((Guid)userId);

            profile.AccountName = model.AccountProfile.AccountName;
            profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
            profile.Biography = model.AccountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            UsersRepository.UpdateProfile(profile);

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
                    model.ErrorMessage += "Could not save the attached image.";
                }
            }

            return NotifyOfAction("The account has been created successfully!", model.ErrorMessage, $"/Admin/Account/{profile.Navigation}");
        }

        [Authorize]
        [HttpGet("Accounts")]
        public ActionResult Accounts()
        {
            WikiContext.RequireAdminPermission();

            var searchString = GetQueryString("SearchString") ?? string.Empty;

            var model = new AccountsViewModel()
            {
                Users = UsersRepository.GetAllUsersPaged(GetQueryString("page", 1), null, searchString),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Users.FirstOrDefault()?.PaginationPageCount ?? 0);

            if (model.Users != null && model.Users.Count > 0)
            {
                model.Users.ForEach(o =>
                {
                    o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                });
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("DeleteAccount/{navigation}")]
        public ActionResult DeleteAccount(string navigation, DeleteAccountViewModel model)
        {
            WikiContext.RequireAdminPermission();

            var profile = UsersRepository.GetAccountProfileByNavigation(navigation);

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

                UsersRepository.AnonymizeProfile(profile.UserId);
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));

                if (profile.UserId == WikiContext.Profile?.UserId)
                {
                    //We're deleting our own account. Oh boy...
                    SignInManager.SignOutAsync();

                    return NotifyOfSuccessAction("Your account has been deleted successfully!", $"/Profile/Deleted");
                }

                return NotifyOfSuccessAction("The account has been deleted successfully!", $"/Admin/Accounts");
            }

            return Redirect($"/Admin/Account/{navigation}");
        }

        [Authorize]
        [HttpGet("DeleteAccount/{navigation}")]
        public ActionResult DeleteAccount(string navigation)
        {
            WikiContext.RequireAdminPermission();
            WikiContext.Title = $"Delete Profile";


            var profile = UsersRepository.GetAccountProfileByNavigation(navigation);

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

        #endregion

        #region Config.

        [Authorize]
        [HttpGet("Config")]
        public ActionResult Config()
        {
            WikiContext.RequireAdminPermission();

            var model = new ConfigurationViewModel()
            {
                Themes = ConfigurationRepository.GetAllThemes(),
                Roles = UsersRepository.GetAllRoles(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Nest = ConfigurationRepository.GetConfigurationNest()
            };
            return View(model);
        }

        [Authorize]
        [HttpPost("Config")]
        public ActionResult Config(ConfigurationViewModel model)
        {
            WikiContext.RequireAdminPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            var flatConfig = ConfigurationRepository.GetFlatConfiguration();

            foreach (var fc in flatConfig)
            {
                string key = $"{fc.GroupId}:{fc.EntryId}";

                var value = GetFormString(key, string.Empty);

                if (fc.IsEncrypted)
                {
                    value = Security.EncryptString(Security.MachineKey, value);
                }

                ConfigurationRepository.SaveConfigurationEntryValueByGroupAndEntry(fc.GroupName, fc.EntryName, value);
            }

            WikiCache.ClearCategory(WikiCache.Category.Configuration);

            model.SuccessMessage = "The configuration has been saved successfully!";

            var newModel = new ConfigurationViewModel()
            {
                Themes = ConfigurationRepository.GetAllThemes(),
                Roles = UsersRepository.GetAllRoles(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Nest = ConfigurationRepository.GetConfigurationNest()
            };

            return View(newModel);
        }

        #endregion

        #region Emojis.

        [Authorize]
        [HttpGet("Emojis")]
        public ActionResult Emojis()
        {
            WikiContext.RequireModeratePermission();
            WikiContext.Title = $"Emojis";

            var searchString = GetQueryString("SearchString") ?? string.Empty;

            var model = new EmojisViewModel()
            {
                Emojis = EmojiRepository.GetAllEmojisPaged(GetQueryString("page", 1), null, Utility.SplitToTokens(searchString)),
                SearchString = searchString
            };

            model.PaginationPageCount = (model.Emojis.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Emoji/{name}")]
        public ActionResult Emoji(string name)
        {
            WikiContext.RequireModeratePermission();

            var emoji = EmojiRepository.GetEmojiByName(name);

            var model = new EmojiViewModel()
            {
                Emoji = emoji ?? new Emoji(),
                Categories = string.Join(",", EmojiRepository.GetEmojiCategoriesByName(name).Select(o => o.Category).ToList())
            };

            model.OriginalName = emoji?.Name ?? string.Empty;

            return View(model);
        }

        /// <summary>
        /// Update an existing emoji.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("Emoji/{name}")]
        public ActionResult Emoji(EmojiViewModel model)
        {
            WikiContext.RequireAdminPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            bool nameChanged = false;

            if (model.OriginalName.ToLowerInvariant() != model.Emoji.Name.ToLowerInvariant())
            {
                nameChanged = true;
                var checkName = EmojiRepository.GetEmojiByName(model.Emoji.Name.ToLowerInvariant());
                if (checkName != null)
                {
                    ModelState.AddModelError("Emoji.Name", "Emoji name is already in use.");
                    return View(model);
                }
            }

            var emoji = new UpsertEmoji
            {
                Id = model.Emoji.Id,
                Name = model.Emoji.Name.ToLowerInvariant(),
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Emoji.Name} {WikiUtility.SeperateCamelCase(model.Emoji.Name)}")
            };

            var file = Request.Form.Files["ImageData"];
            if (file != null && file.Length > 0)
            {
                try
                {
                    emoji.ImageData = Utility.ConvertHttpFileToBytes(file);
                    var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(emoji.ImageData));
                    emoji.MimeType = file.ContentType;
                }
                catch
                {
                    model.ErrorMessage += "Could not save the attached image.";
                }
            }

            emoji.Id = EmojiRepository.UpsertEmoji(emoji);
            model.OriginalName = model.Emoji.Name;
            model.SuccessMessage = "The emoji has been saved successfully!";
            model.Emoji.Id = (int)emoji.Id;
            ModelState.Clear();

            GlobalSettings.ReloadEmojis();

            if (nameChanged)
            {
                return NotifyOfSuccessAction("The emoji has been saved successfully!", $"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
            }

            return View(model);
        }

        [Authorize]
        [HttpGet("AddEmoji")]
        public ActionResult AddEmoji()
        {
            WikiContext.RequireAdminPermission();

            var model = new AddEmojiViewModel()
            {
                Name = string.Empty,
                OriginalName = string.Empty,
                Categories = string.Empty
            };

            return View(model);
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost("AddEmoji")]
        public ActionResult AddEmoji(AddEmojiViewModel model)
        {
            WikiContext.RequireAdminPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            if (string.IsNullOrEmpty(model.OriginalName) == true || model.OriginalName.ToLowerInvariant() != model.Name.ToLowerInvariant())
            {
                var checkName = EmojiRepository.GetEmojiByName(model.Name.ToLower());
                if (checkName != null)
                {
                    ModelState.AddModelError("Name", "Emoji name is already in use.");
                    return View(model);
                }
            }

            var emoji = new UpsertEmoji
            {
                Id = model.Id,
                Name = model.Name.ToLowerInvariant(),
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Name} {WikiUtility.SeperateCamelCase(model.Name)}")
            };

            var file = Request.Form.Files["ImageData"];
            if (file != null && file.Length > 0)
            {
                try
                {
                    emoji.ImageData = Utility.ConvertHttpFileToBytes(file);
                    var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(emoji.ImageData));
                    emoji.MimeType = file.ContentType;
                }
                catch
                {
                    ModelState.AddModelError("Name", "Could not save the attached image.");
                }
            }

            EmojiRepository.UpsertEmoji(emoji);

            return NotifyOfSuccessAction("The emoji has been created successfully!", $"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
        }

        [Authorize]
        [HttpPost("DeleteEmoji/{name}")]
        public ActionResult DeleteEmoji(string name, EmojiViewModel model)
        {
            WikiContext.RequireAdminPermission();

            var emoji = EmojiRepository.GetEmojiByName(name);

            bool confirmAction = bool.Parse(GetFormString("IsActionConfirmed").EnsureNotNull());
            if (confirmAction == true && emoji != null)
            {
                EmojiRepository.DeleteById(emoji.Id);

                return NotifyOfSuccessAction("The emoji has been deleted successfully!", $"/Admin/Emojis");
            }

            return Redirect($"/Admin/Emoji/{name}");
        }

        [Authorize]
        [HttpGet("DeleteEmoji/{name}")]
        public ActionResult DeleteEmoji(string name)
        {
            WikiContext.RequireAdminPermission();
            WikiContext.Title = $"Delete Emoji";

            var emoji = EmojiRepository.GetEmojiByName(name);

            var model = new EmojiViewModel()
            {
                OriginalName = emoji?.Name ?? string.Empty
            };

            if (emoji != null)
            {
                WikiContext.Title = $"Delete {emoji.Name}";
            }

            return View(model);
        }

        #endregion

        #region Exceptions.

        [Authorize]
        [HttpGet("Exceptions")]
        public ActionResult Exceptions()
        {
            WikiContext.RequireAdminPermission();
            WikiContext.Title = $"Exceptions";

            var model = new ExceptionsViewModel()
            {
                Exceptions = ExceptionRepository.GetAllExceptionsPaged(GetQueryString("page", 1))
            };

            model.PaginationPageCount = (model.Exceptions.FirstOrDefault()?.PaginationPageCount ?? 0);

            return View(model);
        }

        [Authorize]
        [HttpGet("Exception/{id}")]
        public ActionResult Exception(int id)
        {
            WikiContext.RequireAdminPermission();
            WikiContext.Title = $"Exception";

            var model = new ExceptionViewModel()
            {
                Exception = ExceptionRepository.GetExceptionById(id)
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("PurgeExceptions")]
        public ActionResult PurgeExceptions(ConfirmActionViewModel model)
        {
            WikiContext.RequireAdminPermission();

            if (model.UserSelection == true)
            {
                ExceptionRepository.PurgeExceptions();
                return Redirect(model.YesRedirectURL);
            }

            return Redirect(model.NoRedirectURL);
        }

        #endregion
    }
}
