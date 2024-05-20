﻿using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Reflection;
using System.Security.Claims;
using TightWiki.DataModels;
using TightWiki.Library;
using TightWiki.Repository;
using TightWiki.ViewModels.Admin;
using TightWiki.ViewModels.Profile;
using TightWiki.ViewModels.Shared;
using TightWiki.Wiki;
using TightWiki.Wiki.Function;
using static TightWiki.Library.Constants;
using static TightWiki.Wiki.Constants;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class AdminController : ControllerBase
    {
        public AdminController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }

        #region Stats.

        [Authorize]
        [HttpGet("Stats")]
        public ActionResult Stats()
        {
            WikiContext.RequireAdminPermission();

            Assembly assembly = Assembly.GetEntryAssembly().EnsureNotNull();
            Version version = assembly.GetName().Version.EnsureNotNull();

            var model = new StatsViewModel()
            {
                DatabaseStats = ConfigurationRepository.GetWikiDatabaseStats(),
                ApplicationVerson = version.ToString()
            };

            WikiContext.Title = $"Statistics";

            return View(model);
        }

        #endregion

        #region Moderate.

        [Authorize]
        [HttpGet("Moderate/{page=1}")]
        public ActionResult Moderate(int page)
        {
            WikiContext.RequireModeratePermission();

            WikiContext.Title = $"Page Moderation";

            var instruction = GetQueryString("Instruction");
            if (instruction != null)
            {
                var model = new PageModerateViewModel()
                {
                    Pages = PageRepository.GetAllPagesByInstructionPaged(page, null, instruction),
                    Instruction = instruction,
                    Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
                };

                if (model.Pages != null && model.Pages.Any())
                {
                    model.Pages.ForEach(o =>
                    {
                        o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                        o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                    });

                    WikiContext.PaginationCount = model.Pages.First().PaginationCount;
                    WikiContext.CurrentPage = page;

                    if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                    if (page > 1) WikiContext.PreviousPage = page - 1;
                }

                return View(model);
            }

            return View(new PageModerateViewModel()
            {
                Pages = new List<Page>(),
                Instruction = String.Empty,
                Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
            });
        }

        [Authorize]
        [HttpPost("Moderate/{page=1}")]
        public ActionResult Moderate(int page, PageModerateViewModel model)
        {
            WikiContext.RequireModeratePermission();

            WikiContext.Title = $"Page Moderation";

            model = new PageModerateViewModel()
            {
                Pages = PageRepository.GetAllPagesByInstructionPaged(page, null, model.Instruction),
                Instruction = model.Instruction,
                Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
            };

            if (model.Pages != null && model.Pages.Any())
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                });

                WikiContext.PaginationCount = model.Pages.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        #endregion

        #region Missing Pages.

        [Authorize]
        [HttpGet("MissingPages/{page=1}")]
        public ActionResult MissingPages(int page)
        {
            WikiContext.RequireModeratePermission();

            var model = new MissingPagesViewModel()
            {
                Pages = PageRepository.GetNonexistentPagesPaged(page, 0)
            };

            if (model.Pages != null && model.Pages.Any())
            {
                WikiContext.Title = $"Missing Pages";
                WikiContext.PaginationCount = model.Pages.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("MissingPages/{page=1}")]
        public ActionResult MissingPages(int page, MissingPagesViewModel model)
        {
            WikiContext.RequireModeratePermission();

            page = 1;

            model = new MissingPagesViewModel()
            {
                Pages = PageRepository.GetNonexistentPagesPaged(page, 0)
            };

            if (model.Pages != null && model.Pages.Any())
            {
                WikiContext.Title = $"Missing Pages";
                WikiContext.PaginationCount = model.Pages.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        #endregion

        #region Namespaces.

        [Authorize]
        [HttpGet("Namespaces/{page=1}")]
        public ActionResult Namespaces(int page)
        {
            WikiContext.RequireModeratePermission();

            var model = new NamespacesViewModel()
            {
                Namespaces = PageRepository.GetAllNamespacesPaged(page, null),
            };

            if (model.Namespaces != null && model.Namespaces.Any())
            {
                WikiContext.Title = $"Namespaces";
                WikiContext.PaginationCount = model.Namespaces.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        #endregion

        #region Pages.

        [Authorize]
        [HttpGet("Pages/{page=1}")]
        public ActionResult Pages(int page)
        {
            WikiContext.RequireModeratePermission();

            var searchTokens = Utility.SplitToTokens(GetQueryString("Tokens"));

            var model = new PagesViewModel()
            {
                Pages = PageRepository.GetAllPagesPaged(page, null, searchTokens),
                SearchTokens = GetQueryString("Tokens", string.Empty)
            };

            if (model.Pages != null && model.Pages.Any())
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                });

                WikiContext.Title = $"Pages";
                WikiContext.PaginationCount = model.Pages.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("Pages/{page=1}")]
        public ActionResult Pages(int page, PagesViewModel model)
        {
            WikiContext.RequireModeratePermission();

            var searchTokens = Utility.SplitToTokens(model.SearchTokens);

            model = new PagesViewModel()
            {
                Pages = PageRepository.GetAllPagesPaged(page, null, searchTokens),
                SearchTokens = model.SearchTokens ?? string.Empty
            };

            if (model.Pages != null && model.Pages.Any())
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                });

                WikiContext.Title = $"Pages";
                WikiContext.PaginationCount = model.Pages.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        #endregion

        #region Confirm Action.

        [Authorize]
        [HttpGet("ConfirmAction")]
        public ActionResult ConfirmAction()
        {
            WikiContext.RequireAdminPermission();

            var model = new UtilitiesViewModel()
            {
            };

            return View(model);
        }

        [Authorize]
        [HttpPost("ConfirmAction")]
        public ActionResult ConfirmAction(ConfirmActionViewModel model)
        {
            WikiContext.RequireAdminPermission();

            model.ActionToConfirm = GetFormString("ActionToConfirm").EnsureNotNull();
            model.PostBackURL = GetQueryString("PostBack").EnsureNotNull();
            model.Message = GetFormString("message").EnsureNotNull();

            return View(model);
        }

        #endregion

        #region Utilities.

        [Authorize]
        [HttpGet("Utilities")]
        public ActionResult Utilities()
        {
            WikiContext.RequireAdminPermission();

            WikiContext.IsDebug = Debugger.IsAttached;

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

            WikiContext.IsDebug = Debugger.IsAttached;

            string action = (GetFormString("ActionToConfirm")?.ToString()?.ToLower()).EnsureNotNull();
            if (bool.Parse(GetFormString("ConfirmAction").EnsureNotNull()) != true)
            {
                return View(model);
            }

            switch (action)
            {
                case "rebuildpagesearchindex":
                    {
                        var pages = PageRepository.GetAllPages();

                        foreach (var page in pages)
                        {
                            var wiki = new Wikifier(WikiContext, page, null, Request.Query, new WikiMatchType[] { WikiMatchType.Function });

                            PageTagRepository.UpdatePageTags(page.Id, wiki.Tags);
                            PageRepository.UpdatePageProcessingInstructions(page.Id, wiki.ProcessingInstructions);
                            var pageTokens = wiki.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
                            PageRepository.SavePageTokens(pageTokens);
                            WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
                        }
                    }
                    break;
                case "rebuildallpages":
                    {
                        var pages = PageRepository.GetAllPages();

                        foreach (var page in pages)
                        {
                            base.RefreshPageProperties(page);
                        }
                    }
                    break;
                case "truncatepagerevisionhistory":
                    {
                        PageRepository.TruncateAllPageHistory("YES");
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
                Items = MenuItemRepository.GetAllMenuItems()
            };

            return View(model);
        }

        [Authorize]
        [HttpGet("MenuItem/{id:int?}")]
        public ActionResult MenuItem(int? id)
        {
            WikiContext.RequireAdminPermission();

            if (id != null)
            {
                var menuItem = MenuItemRepository.GetMenuItemById((int)id);
                WikiContext.Title = $"Menu Item";
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

            if (MenuItemRepository.GetAllMenuItems().Where(o => o.Name.ToLower() == model.Name.ToLower() && o.Id != model.Id).Any())
            {
                ModelState.AddModelError("Name", $"The menu name '{model.Name}' is already in use.");
                return View(model);
            }

            if (id.DefaultWhenNull(0) == 0)
            {
                model.Id = MenuItemRepository.InsertMenuItem(model.ToDataModel());
                ModelState.Clear();

                return Redirect($"/Admin/MenuItem/{model.Id}");
            }
            else
            {
                MenuItemRepository.UpdateMenuItemById(model.ToDataModel());
            }

            model.SuccessMessage = "The menu item has been saved successfully!";
            return View(model);
        }

        [Authorize]
        [HttpGet("DeleteMenuItem/{id}")]
        public ActionResult DeleteMenuItem(int id)
        {
            WikiContext.RequireAdminPermission();

            var model = MenuItemRepository.GetMenuItemById(id);
            WikiContext.Title = $"{model.Name} Delete";

            return View(model.ToViewModel());
        }

        [Authorize]
        [HttpPost("DeleteMenuItem/{id}")]
        public ActionResult DeleteMenuItem(MenuItemViewModel model)
        {
            WikiContext.RequireAdminPermission();

            bool confirmAction = bool.Parse(GetFormString("Action").EnsureNotNull());
            if (confirmAction == true)
            {
                MenuItemRepository.DeleteMenuItemById(model.Id);
                return Redirect($"/Admin/MenuItems");
            }

            return Redirect($"/Admin/MenuItem/{model.Id}");
        }

        #endregion

        #region Roles.

        [Authorize]
        [HttpGet("Role/{navigation}/{page=1}")]
        public ActionResult Role(string navigation, int page)
        {
            WikiContext.RequireAdminPermission();

            navigation = Navigation.Clean(navigation);

            var role = ProfileRepository.GetRoleByName(navigation);

            var model = new RoleViewModel()
            {
                Id = role.Id,
                Name = role.Name,
                Users = ProfileRepository.GetProfilesByRoleIdPaged(role.Id, page)
            };

            WikiContext.Title = $"Roles";
            WikiContext.PaginationCount = model.Users.FirstOrDefault()?.PaginationCount ?? 0;
            WikiContext.CurrentPage = page;

            if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
            if (page > 1) WikiContext.PreviousPage = page - 1;

            return View(model);
        }

        [Authorize]
        [HttpGet("Roles")]
        public ActionResult Roles()
        {
            WikiContext.RequireAdminPermission();

            var model = new RolesViewModel()
            {
                Roles = ProfileRepository.GetAllRoles()
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

            var model = new ViewModels.Admin.AccountProfileViewModel()
            {
                AccountProfile = ViewModels.Admin.AccountProfileAccountViewModel.FromDataModel(
                    ProfileRepository.GetAccountProfileByNavigation(Navigation.Clean(navigation))),
                Credential = new CredentialViewModel(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = ProfileRepository.GetAllRoles()
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
        public ActionResult Account(string navigation, ViewModels.Admin.AccountProfileViewModel model)
        {
            WikiContext.RequireAdminPermission();

            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = ProfileRepository.GetAllRoles();
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

                    if (model.AccountProfile.AccountName.Equals("admin", StringComparison.CurrentCultureIgnoreCase))
                    {
                        ConfigurationRepository.SetAdminPasswordIsChanged();
                    }
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("Credential.Password", ex.Message);
                    return View(model);
                }
            }

            var profile = ProfileRepository.GetAccountProfileByUserId(model.AccountProfile.UserId);
            if (!profile.Navigation.Equals(model.AccountProfile.Navigation, StringComparison.CurrentCultureIgnoreCase))
            {
                if (ProfileRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
                {
                    ModelState.AddModelError("AccountProfile.AccountName", "Account name is already in use.");
                    return View(model);
                }
            }

            if (!profile.EmailAddress.Equals(model.AccountProfile.EmailAddress, StringComparison.CurrentCultureIgnoreCase))
            {
                if (ProfileRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
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
                    ProfileRepository.UpdateProfileAvatar(profile.UserId, imageBytes);
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
            ProfileRepository.UpdateProfile(profile);

            var claims = new List<Claim>
                    {
                        new (ClaimTypes.Role, model.AccountProfile.Role),
                        new ("timezone", model.AccountProfile.TimeZone),
                        new (ClaimTypes.Country, model.AccountProfile.Country),
                        new ("language", model.AccountProfile.Language),
                        new ("firstname", model.AccountProfile.FirstName ?? ""),
                        new ("lastname", model.AccountProfile.LastName ?? ""),
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
            var defaultSignupRole = membershipConfig.As<string>("Default Signup Role").EnsureNotNull();
            var customizationConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Customization");

            var model = new ViewModels.Admin.AccountProfileViewModel()
            {
                AccountProfile = new ViewModels.Admin.AccountProfileAccountViewModel
                {
                    AccountName = ProfileRepository.GetRandomUnusedAccountName(),
                    Country = customizationConfig.As<string>("Default Country", string.Empty),
                    TimeZone = customizationConfig.As<string>("Default TimeZone", string.Empty),
                    Language = customizationConfig.As<string>("Default Language", string.Empty),
                    Role = defaultSignupRole
                },
                Credential = new CredentialViewModel(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = ProfileRepository.GetAllRoles()
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
        public ActionResult AddAccount(ViewModels.Admin.AccountProfileViewModel model)
        {
            WikiContext.RequireAdminPermission();

            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = ProfileRepository.GetAllRoles();
            model.AccountProfile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName.ToLower());

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            if (ProfileRepository.DoesProfileAccountExist(model.AccountProfile.AccountName))
            {
                ModelState.AddModelError("AccountProfile.AccountName", "Account name is already in use.");
                return View(model);
            }

            if (ProfileRepository.DoesEmailAddressExist(model.AccountProfile.EmailAddress))
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
                    };
                SecurityHelpers.UpsertUserClaims(UserManager, identityUser, claims);
            }
            catch (Exception ex)
            {
                model.ErrorMessage = ex.Message;
                return View(model);
            }

            ProfileRepository.CreateProfile((Guid)userId, model.AccountProfile.AccountName);
            var profile = ProfileRepository.GetAccountProfileByUserId((Guid)userId);

            profile.AccountName = model.AccountProfile.AccountName;
            profile.Navigation = NamespaceNavigation.CleanAndValidate(model.AccountProfile.AccountName);
            profile.Biography = model.AccountProfile.Biography;
            profile.ModifiedDate = DateTime.UtcNow;
            ProfileRepository.UpdateProfile(profile);

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
                    model.ErrorMessage += "Could not save the attached image.";
                }
            }

            model.SuccessMessage = "The account has been created successfully!";

            return Redirect($"/Admin/Account/{profile.Navigation}");
        }


        [Authorize]
        [HttpGet("Accounts/{page=1}")]
        public ActionResult Accounts(int page)
        {
            WikiContext.RequireAdminPermission();

            var searchToken = GetQueryString("Token");

            var model = new AccountsViewModel()
            {
                Users = ProfileRepository.GetAllUsersPaged(page, null, searchToken)
            };

            if (model.Users != null && model.Users.Any())
            {
                model.Users.ForEach(o =>
                {
                    o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                });

                WikiContext.PaginationCount = model.Users.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("Accounts/{page=1}")]
        public ActionResult Accounts(int page, AccountsViewModel model)
        {
            WikiContext.RequireAdminPermission();

            string searchToken = model.SearchToken;

            model = new AccountsViewModel()
            {
                Users = ProfileRepository.GetAllUsersPaged(page, null, searchToken)
            };

            if (model.Users != null && model.Users.Any())
            {
                model.Users.ForEach(o =>
                {
                    o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
                });

                WikiContext.PaginationCount = model.Users.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("DeleteAccount/{navigation}")]
        public ActionResult DeleteAccount(string navigation, AccountViewModel model)
        {
            WikiContext.RequireAdminPermission();

            var profile = ProfileRepository.GetAccountProfileByNavigation(navigation);

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

                ProfileRepository.AnonymizeProfile(profile.UserId);
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.User, [profile.Navigation]));

                if (profile.UserId == WikiContext.Profile?.UserId)
                {
                    //We're deleting our own account. Oh boy...
                    SignInManager.SignOutAsync();
                    return Redirect($"/Profile/Deleted");
                }

                return Redirect($"/Admin/Accounts");
            }

            return Redirect($"/Admin/Accounts/{navigation}");
        }

        [Authorize]
        [HttpGet("DeleteAccount/{navigation}")]
        public ActionResult DeleteAccount(string navigation)
        {
            WikiContext.RequireAdminPermission();

            var profile = ProfileRepository.GetAccountProfileByNavigation(navigation);

            WikiContext.AccountName = profile.AccountName;

            var model = new AccountViewModel()
            {
            };

            if (profile != null)
            {
                WikiContext.Title = $"{profile.AccountName} Delete";
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
                Roles = ProfileRepository.GetAllRoles(),
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
                Roles = ProfileRepository.GetAllRoles(),
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
        [HttpGet("Emojis/{page=1}")]
        public ActionResult Emojis(int page)
        {
            WikiContext.RequireModeratePermission();

            var searchTokens = Utility.SplitToTokens(GetQueryString("Categories"));

            var model = new EmojisViewModel()
            {
                Emojis = EmojiRepository.GetAllEmojisPaged(page, null, searchTokens),
                Categories = GetQueryString("Categories", string.Empty)
            };

            if (model.Emojis != null && model.Emojis.Any())
            {
                WikiContext.Title = $"Emojis";
                WikiContext.PaginationCount = model.Emojis.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("Emojis/{page=1}")]
        public ActionResult Emojis(int page, EmojisViewModel model)
        {
            WikiContext.RequireModeratePermission();

            var searchTokens = Utility.SplitToTokens(model.Categories);

            model = new EmojisViewModel()
            {
                Emojis = EmojiRepository.GetAllEmojisPaged(page, null, searchTokens),
                Categories = model.Categories ?? string.Empty
            };

            if (model.Emojis != null && model.Emojis.Any())
            {
                WikiContext.Title = $"Emojis";
                WikiContext.PaginationCount = model.Emojis.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

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
                Categories = String.Join(",", EmojiRepository.GetEmojiCategoriesByName(name).Select(o => o.Category).ToList())
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
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Emoji.Name} {WikiUtility.SplitCamelCase(model.Emoji.Name)}")
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

            if (nameChanged)
            {
                return Redirect($"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
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
                Categories = Utility.SplitToTokens($"{model.Categories} {model.Name} {WikiUtility.SplitCamelCase(model.Name)}")
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

            return Redirect($"/Admin/Emoji/{Navigation.Clean(emoji.Name)}");
        }

        [Authorize]
        [HttpPost("DeleteEmoji/{name}")]
        public ActionResult DeleteEmoji(string name, EmojiViewModel model)
        {
            WikiContext.RequireAdminPermission();

            var emoji = EmojiRepository.GetEmojiByName(name);

            bool confirmAction = bool.Parse(GetFormString("Action").EnsureNotNull());
            if (confirmAction == true && emoji != null)
            {
                EmojiRepository.DeleteById(emoji.Id);
                return Redirect($"/Admin/Emojis");
            }

            return Redirect($"/Admin/Emoji/{name}");
        }

        [Authorize]
        [HttpGet("DeleteEmoji/{name}")]
        public ActionResult DeleteEmoji(string name)
        {
            WikiContext.RequireAdminPermission();

            var emoji = EmojiRepository.GetEmojiByName(name);

            WikiContext.EmojiName = emoji?.Name ?? string.Empty;

            var model = new EmojiViewModel()
            {
            };

            if (emoji != null)
            {
                WikiContext.Title = $"{emoji.Name} Delete";
            }

            return View(model);
        }

        #endregion
    }
}
