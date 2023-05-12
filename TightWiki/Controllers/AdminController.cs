using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using TightWiki.Controllers;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;
using TightWiki.Shared.Wiki.Function;
using static TightWiki.Shared.Library.Constants;
using static TightWiki.Shared.Wiki.Constants;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class AdminController : ControllerHelperBase
    {
        [Authorize]
        [HttpGet]
        public ActionResult Stats()
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            var model = new StatsModel()
            {
                DatabaseStats = ConfigurationRepository.GetWikiDatabaseStats(),
                CachedItemCount = Cache.Memcache.Count(),
                CacheMemoryLimit = Cache.Memcache.CacheMemoryLimit
            };

            ViewBag.Config.Title = $"Statistics";

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Moderate(int page)
        {
            if (context.CanAdmin == false && context.CanModerate == false)
            {
                return Unauthorized();
            }

            if (page <= 0) page = 1;

            ViewBag.Config.Title = $"Page Moderation";

            string instruction = Request.Query["Instruction"];
            if (instruction != null)
            {
                var model = new PageModerateModel()
                {
                    Pages = PageRepository.GetAllPagesByInstructionPaged(page, 0, instruction),
                    Instruction = instruction,
                    Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
                };

                if (model.Pages != null && model.Pages.Any())
                {
                    model.Pages.ForEach(o =>
                    {
                        o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
                        o.ModifiedDate = context.LocalizeDateTime(o.ModifiedDate);
                    });

                    ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                    ViewBag.CurrentPage = page;

                    if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                    if (page > 1) ViewBag.PreviousPage = page - 1;
                }

                return View(model);
            }

            return View(new PageModerateModel()
            {
                Pages = new List<Page>(),
                Instruction = String.Empty,
                Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
            });
        }

        [Authorize]
        [HttpPost]
        public ActionResult Moderate(int page, PageModerateModel model)
        {
            if (context.CanAdmin == false && context.CanModerate == false)
            {
                return Unauthorized();
            }

            page = 1;

            ViewBag.Config.Title = $"Page Moderation";

            model = new PageModerateModel()
            {
                Pages = PageRepository.GetAllPagesByInstructionPaged(page, 0, model.Instruction),
                Instruction = model.Instruction,
                Instructions = typeof(WikiInstruction).GetProperties().Select(o => o.Name).ToList()
            };

            if (model.Pages != null && model.Pages.Any())
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = context.LocalizeDateTime(o.ModifiedDate);
                });

                ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult MissingPages(int page)
        {
            if (context.CanAdmin == false && context.CanModerate == false)
            {
                return Unauthorized();
            }

            if (page <= 0) page = 1;

            var model = new MissingPagesModel()
            {
                Pages = PageRepository.GetNonexistentPagesPaged(page, 0)
            };

            if (model.Pages != null && model.Pages.Any())
            {
                ViewBag.Config.Title = $"Missing Pages";
                ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult MissingPages(int page, MissingPagesModel model)
        {
            if (context.CanAdmin == false && context.CanModerate == false)
            {
                return Unauthorized();
            }

            page = 1;

            model = new MissingPagesModel()
            {
                Pages = PageRepository.GetNonexistentPagesPaged(page, 0)
            };

            if (model.Pages != null && model.Pages.Any())
            {
                ViewBag.Config.Title = $"Missing Pages";
                ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Namespaces(int page)
        {
            if (context.CanAdmin == false && context.CanModerate == false)
            {
                return Unauthorized();
            }

            if (page <= 0) page = 1;


            var model = new NamespacesModel()
            {
                Namespaces = PageRepository.GetAllNamespacesPaged(page, 0),
            };

            if (model.Namespaces != null && model.Namespaces.Any())
            {
                ViewBag.Config.Title = $"Namespaces";
                ViewBag.PaginationCount = model.Namespaces.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Pages(int page)
        {
            if (context.CanAdmin == false && context.CanModerate == false)
            {
                return Unauthorized();
            }

            if (page <= 0) page = 1;

            string searchTokens = Request.Query["Tokens"];
            if (searchTokens != null)
            {
                var tokens = searchTokens.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Select(o => o.ToLower()).Distinct();
                searchTokens = string.Join(",", tokens);
            }

            var model = new PagesModel()
            {
                Pages = PageRepository.GetAllPagesPaged(page, 0, searchTokens),
                SearchTokens = Request.Query["Tokens"]
            };

            if (model.Pages != null && model.Pages.Any())
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = context.LocalizeDateTime(o.ModifiedDate);
                });

                ViewBag.Config.Title = $"Pages";
                ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Pages(int page, PagesModel model)
        {
            if (context.CanAdmin == false && context.CanModerate == false)
            {
                return Unauthorized();
            }

            page = 1;

            string searchTokens = null;
            if (model.SearchTokens != null)
            {
                var tokens = model.SearchTokens.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Select(o => o.ToLower()).Distinct();
                searchTokens = string.Join(",", tokens);
            }

            model = new PagesModel()
            {
                Pages = PageRepository.GetAllPagesPaged(page, 0, searchTokens),
                SearchTokens = model.SearchTokens
            };

            if (model.Pages != null && model.Pages.Any())
            {
                model.Pages.ForEach(o =>
                {
                    o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = context.LocalizeDateTime(o.ModifiedDate);
                });

                ViewBag.Config.Title = $"Pages";
                ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult ConfirmAction()
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            var model = new UtilitiesModel()
            {
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult ConfirmAction(ConfirmActionModel model)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            model.ActionToConfirm = Request.Form["ActionToConfirm"];
            model.PostBackURL = Request.Query["PostBack"];
            model.Message = Request.Form["message"];

            return View(model);
        }


        [Authorize]
        [HttpGet]
        public ActionResult Utilities()
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            ViewBag.IsDebug = Debugger.IsAttached;

            var model = new UtilitiesModel()
            {
            };

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Utilities(UtilitiesModel model)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            ViewBag.IsDebug = Debugger.IsAttached;

            string action = Request.Form["ActionToConfirm"].ToString()?.ToLower();
            if (bool.Parse(Request.Form["ConfirmAction"]) != true)
            {
                return View(model);
            }

            if (action == "rebuildsearchcache")
            {
                var pages = PageRepository.GetAllPages();

                foreach (var page in pages)
                {
                    var wikifier = new Wikifier(context, page, null, Request.Query, new WikiMatchType[] { WikiMatchType.Function });
                    PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
                    PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
                    var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
                    PageRepository.SavePageTokens(pageTokens);
                    Cache.ClearClass($"Page:{page.Navigation}");
                }
            }
            else if (action == "rebuildallpages")
            {
                var pages = PageRepository.GetAllPages();

                foreach (var page in pages)
                {
                    base.SavePage(page);
                }
            }
            else if (action == "truncatepagerevisionhistory")
            {
                PageRepository.TruncateAllPageHistory("YES");
                Cache.Clear();
            }
            else if (action == "createselfdocumentation")
            {
                SelfDocument.CreateNotExisting();
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Role(string navigation, int page)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            if (page <= 0) page = 1;

            var role = UserRepository.GetRoleByName(navigation);

            var model = new RoleModel()
            {
                Id = role.Id,
                Name = role.Name,
                Users = UserRepository.GetUsersByRoleId(role.Id, 1)
            };

            ViewBag.Config.Title = $"Roles";
            ViewBag.PaginationCount = model.Users.FirstOrDefault()?.PaginationCount ?? 0;
            ViewBag.CurrentPage = page;

            if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
            if (page > 1) ViewBag.PreviousPage = page - 1;

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Roles()
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            var model = new RolesModel()
            {
                Roles = UserRepository.GetAllRoles()
            };

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Account(string navigation)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            navigation = WikiUtility.CleanPartialURI(navigation);

            var model = new AccountAdminModel()
            {
                Account = UserRepository.GetUserByNavigation(navigation),
                Credential = new Credential(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = UserRepository.GetAllRoles()
            };


            model.Account.CreatedDate = context.LocalizeDateTime(model.Account.CreatedDate);
            model.Account.ModifiedDate = context.LocalizeDateTime(model.Account.ModifiedDate);
            model.Account.LastLoginDate = context.LocalizeDateTime(model.Account.LastLoginDate);

            return View(model);
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult Account(AccountAdminModel model)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = UserRepository.GetAllRoles();

            model.Account.Navigation = WikiUtility.CleanPartialURI(model.Account.AccountName.ToLower());
            var user = UserRepository.GetUserByNavigation(model.Account.Navigation);

            var file = Request.Form.Files["Avatar"];
            if (file != null && file.Length > 0)
            {
                try
                {
                    var imageBytes = Utility.ConvertHttpFileToBytes(file);
                    //This is just to ensure this is a valid image:
                    var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(imageBytes));
                    UserRepository.UpdateUserAvatar(user.Id, imageBytes);
                }
                catch
                {
                    ModelState.AddModelError("Account.Avatar", "Could not save the attached image.");
                }
            }

            if (ModelState.IsValid)
            {
                if (user.Navigation.ToLower() != model.Account.Navigation.ToLower())
                {
                    var checkName = UserRepository.GetUserByNavigation(WikiUtility.CleanPartialURI(model.Account.AccountName.ToLower()));
                    if (checkName != null)
                    {
                        ModelState.AddModelError("Account.AccountName", "Account name is already in use.");
                        return View(model);
                    }
                }

                if (user.EmailAddress.ToLower() != model.Account.EmailAddress.ToLower())
                {
                    var checkName = UserRepository.GetUserByEmail(model.Account.EmailAddress.ToLower());
                    if (checkName != null)
                    {
                        ModelState.AddModelError("Account.EmailAddress", "Email address is already in use.");
                        return View(model);
                    }
                }

                user.AboutMe = model.Account.AboutMe;
                user.FirstName = model.Account.FirstName;
                user.LastName = model.Account.LastName;
                user.TimeZone = model.Account.TimeZone;
                user.Country = model.Account.Country;
                user.Role = model.Account.Role;
                user.Language = model.Account.Language;
                user.AccountName = model.Account.AccountName;
                user.Navigation = WikiUtility.CleanPartialURI(model.Account.AccountName);
                user.EmailAddress = model.Account.EmailAddress;
                user.ModifiedDate = DateTime.UtcNow;
                UserRepository.UpdateUser(user);

                //Only set the password if it has changed.
                if (string.IsNullOrEmpty(model.Credential.Password) == false
                    && string.IsNullOrEmpty(model.Credential.ComparePassword) == false
                    && model.Credential.Password == model.Credential.ComparePassword
                    && model.Credential.Password != Credential.NOTSET)
                {
                    UserRepository.UpdateUserPassword(user.Id, model.Credential.Password);
                }

                model.SuccessMessage = "Your profile has been saved successfully!.";
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult AddAccount()
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
            var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
            var defaultSignupRole = membershipConfig.As<string>("Default Signup Role");

            var model = new AccountAdminModel()
            {
                Account = new User()
                {
                    Country = basicConfig.As<string>("Default Country"),
                    TimeZone = basicConfig.As<string>("Default TimeZone"),
                    Language = basicConfig.As<string>("Default Language"),
                    Role = defaultSignupRole,
                },
                Credential = new Credential(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Roles = UserRepository.GetAllRoles()
            };

            return View(model);
        }

        /// <summary>
        /// Save user profile.
        /// </summary>
        /// <param name="profile"></param>
        /// <returns></returns>
        [Authorize]
        [HttpPost]
        public ActionResult AddAccount(AccountAdminModel model)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            model.TimeZones = TimeZoneItem.GetAll();
            model.Countries = CountryItem.GetAll();
            model.Languages = LanguageItem.GetAll();
            model.Roles = UserRepository.GetAllRoles();

            model.Account.Navigation = WikiUtility.CleanPartialURI(model.Account.AccountName?.ToLower());

            if (ModelState.IsValid)
            {
                if (string.IsNullOrWhiteSpace(model.Account.Navigation))
                {
                    ModelState.AddModelError("Account.AccountName", "Account name is required.");
                    return View(model);
                }

                var checkAccount = UserRepository.GetUserByNavigation(WikiUtility.CleanPartialURI(model.Account.AccountName.ToLower()));
                if (checkAccount != null)
                {
                    ModelState.AddModelError("Account.AccountName", "Account name is already in use.");
                    return View(model);
                }

                if (string.IsNullOrWhiteSpace(model.Account.EmailAddress))
                {
                    ModelState.AddModelError("Account.EmailAddress", "Email address is required.");
                    return View(model);
                }

                var checkEmail = UserRepository.GetUserByEmail(model.Account.EmailAddress?.ToLower());
                if (checkEmail != null)
                {
                    ModelState.AddModelError("Account.EmailAddress", "Email address is already in use.");
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.Credential.Password) || model.Credential.Password == Credential.NOTSET)
                {
                    ModelState.AddModelError("Credential.Password", "You must enter a password.");
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.Credential.ComparePassword) || model.Credential.ComparePassword == Credential.NOTSET)
                {
                    ModelState.AddModelError("Credential.ComparePassword", "You must enter a password.");
                    return View(model);
                }

                if (string.IsNullOrEmpty(model.Credential.ComparePassword) || model.Credential.ComparePassword == Credential.NOTSET)
                {
                    ModelState.AddModelError("Credential.Password", "Passwords do not match.");
                    ModelState.AddModelError("Credential.ComparePassword", "Passwords do not match.");
                    return View(model);
                }

                var basicConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Basic");
                var siteName = basicConfig.As<string>("Name");
                var address = basicConfig.As<string>("Address");

                var membershipConfig = ConfigurationRepository.GetConfigurationEntryValuesByGroupName("Membership");
                //var defaultSignupRole = membershipConfig.As<string>("Default Signup Role");
                var requestEmailVerification = membershipConfig.As<bool>("Request Email Verification");
                var requireEmailVerification = membershipConfig.As<bool>("Require Email Verification");
                var accountVerificationEmailTemplate = new StringBuilder(membershipConfig.As<string>("Account Verification Email Template"));

                var user = new User()
                {
                    AboutMe = model.Account.AboutMe,
                    FirstName = model.Account.FirstName,
                    LastName = model.Account.LastName,
                    TimeZone = model.Account.TimeZone,
                    Country = model.Account.Country,
                    Language = model.Account.Language,
                    AccountName = model.Account.AccountName,
                    Navigation = WikiUtility.CleanPartialURI(model.Account.AccountName),
                    EmailAddress = model.Account.EmailAddress,
                    Role = model.Account.Role,
                    ModifiedDate = DateTime.UtcNow
                };
                user.Id = UserRepository.CreateUser(user);

                var file = Request.Form.Files["Avatar"];
                if (file != null && file.Length > 0)
                {
                    try
                    {
                        var imageBytes = Utility.ConvertHttpFileToBytes(file);
                        //This is just to ensure this is a valid image:
                        var image = SixLabors.ImageSharp.Image.Load(new MemoryStream(imageBytes));
                        UserRepository.UpdateUserAvatar(user.Id, imageBytes);
                    }
                    catch
                    {
                        ModelState.AddModelError("Account.Avatar", "Could not save the attached image.");
                    }
                }

                if (requestEmailVerification || requireEmailVerification)
                {
                    var emailSubject = "Account Verification";
                    accountVerificationEmailTemplate.Replace("##SUBJECT##", emailSubject);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTCOUNTRY##", user.Country);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTTIMEZONE##", user.TimeZone);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTLANGUAGE##", user.Language);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTEMAIL##", user.EmailAddress);
                    accountVerificationEmailTemplate.Replace("##ACCOUNTNAME##", user.AccountName);
                    accountVerificationEmailTemplate.Replace("##PERSONNAME##", $"{user.FirstName} {user.LastName}");
                    accountVerificationEmailTemplate.Replace("##CODE##", user.VerificationCode);
                    accountVerificationEmailTemplate.Replace("##SITENAME##", siteName);
                    accountVerificationEmailTemplate.Replace("##SITEADDRESS##", address);

                    Email.Send(user.EmailAddress, emailSubject, accountVerificationEmailTemplate.ToString());
                }

                UserRepository.UpdateUserPassword(user.Id, model.Credential.Password);

                model.SuccessMessage = "The account was created successfully!.";
            }

            return View(model);
        }


        [Authorize]
        [HttpGet]
        public ActionResult Accounts(int page)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            if (page <= 0) page = 1;

            string searchToken = Request.Query["Token"];

            var model = new AccountsModel()
            {
                Users = UserRepository.GetAllUsersPaged(page, 0, searchToken)
            };

            if (model.Users != null && model.Users.Any())
            {
                model.Users.ForEach(o =>
                {
                    o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = context.LocalizeDateTime(o.ModifiedDate);
                    o.LastLoginDate = context.LocalizeDateTime(o.LastLoginDate);
                });

                ViewBag.PaginationCount = model.Users.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Accounts(int page, AccountsModel model)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            page = 1;

            string searchToken = model.SearchToken;

            model = new AccountsModel()
            {
                Users = UserRepository.GetAllUsersPaged(page, 0, searchToken)
            };

            if (model.Users != null && model.Users.Any())
            {
                model.Users.ForEach(o =>
                {
                    o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
                    o.ModifiedDate = context.LocalizeDateTime(o.ModifiedDate);
                    o.LastLoginDate = context.LocalizeDateTime(o.LastLoginDate);
                });

                ViewBag.PaginationCount = model.Users.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Config()
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            var model = new ConfigurationModel()
            {
                Roles = UserRepository.GetAllRoles(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Nest = ConfigurationRepository.GetConfigurationNest()
            };
            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Config(ConfigurationModel model)
        {
            if (context.CanAdmin == false)
            {
                return Unauthorized();
            }

            var flatConfig = ConfigurationRepository.GetFlatConfiguration();

            foreach (var fc in flatConfig)
            {
                string key = $"{fc.GroupId}:{fc.EntryId}";

                var value = Request.Form[key];

                if (fc.IsEncrypted)
                {
                    value = Security.EncryptString(Security.MachineKey, value);
                }

                ConfigurationRepository.SaveConfigurationEntryValueByGroupAndEntry(fc.GroupName, fc.EntryName, value);
            }

            Cache.ClearClass("Config:");

            if (ModelState.IsValid)
            {
                model.SuccessMessage = "The configuration has been saved successfully!";
            }

            var newModel = new ConfigurationModel()
            {
                Roles = UserRepository.GetAllRoles(),
                TimeZones = TimeZoneItem.GetAll(),
                Countries = CountryItem.GetAll(),
                Languages = LanguageItem.GetAll(),
                Nest = ConfigurationRepository.GetConfigurationNest()
            };

            return View(newModel);
        }

        [Authorize]
        [HttpPost]
        public ActionResult DeleteAccount(string navigation, AccountModel model)
        {
            if (context.CanDelete == false)
            {
                return Unauthorized();
            }

            var user = UserRepository.GetUserByNavigation(navigation);

            bool confirmAction = bool.Parse(Request.Form["Action"]);
            if (confirmAction == true && user != null)
            {
                UserRepository.DeleteById(user.Id);
                Cache.ClearClass($"User:{user.Navigation}");
                return RedirectToAction("Accounts", "Admin");
            }

            return RedirectToAction("Account", "Admin", new { navigation = navigation });
        }

        [Authorize]
        [HttpGet]
        public ActionResult DeleteAccount(string navigation)
        {
            if (context.CanDelete == false)
            {
                return Unauthorized();
            }

            var user = UserRepository.GetUserByNavigation(navigation);

            ViewBag.AccountName = user.AccountName;

            var model = new AccountModel()
            {
            };

            if (user != null)
            {
                ViewBag.Config.Title = $"{user.AccountName} Delete";
            }

            return View(model);
        }
    }
}
