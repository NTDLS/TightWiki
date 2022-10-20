using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;
using static TightWiki.Shared.Library.Constants;
using static TightWiki.Shared.Wiki.MethodCall.Singletons;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class AdminController : ControllerHelperBase
    {
        [Authorize]
        [HttpGet]
        public ActionResult Moderate(int page)
        {
            if (context.CanAdmin == false && context.CanModerate == false)
            {
                return new HttpUnauthorizedResult();
            }

            if (page <= 0) page = 1;

            ViewBag.Config.Title = $"Page Moderation";

            string instruction = Request.QueryString["Instruction"];
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
                return new HttpUnauthorizedResult();
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
                ViewBag.PaginationCount = model.Pages.First().PaginationCount;
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
                return new HttpUnauthorizedResult();
            }

            if (page <= 0) page = 1;

            string searchTokens = Request.QueryString["Tokens"];
            if (searchTokens != null)
            {
                var tokens = searchTokens.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries).Select(o => o.ToLower()).Distinct();
                searchTokens = string.Join(",", tokens);
            }

            var model = new PagesModel()
            {
                Pages = PageRepository.GetAllPages(page, 0, searchTokens),
                SearchTokens = Request.QueryString["Tokens"]
            };

            if (model.Pages != null && model.Pages.Any())
            {
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
                return new HttpUnauthorizedResult();
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
                Pages = PageRepository.GetAllPages(page, 0, searchTokens),
                SearchTokens = model.SearchTokens
            };

            if (model.Pages != null && model.Pages.Any())
            {
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
                return new HttpUnauthorizedResult();
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
                return new HttpUnauthorizedResult();
            }

            model.ActionToConfirm = Request.Form["ActionToConfirm"];
            model.PostBackURL = Request.QueryString["PostBack"];
            model.Message = Request.Form["message"];

            return View(model);
        }


        [Authorize]
        [HttpGet]
        public ActionResult Utilities()
        {
            if (context.CanAdmin == false)
            {
                return new HttpUnauthorizedResult();
            }

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
                return new HttpUnauthorizedResult();
            }

            string action = Request.Form["ActionToConfirm"]?.ToLower();
            if (bool.Parse(Request.Form["ConfirmAction"]) != true)
            {
                return View(model);
            }

            if (action == "rebuildsearchcache")
            {
                var pages = PageRepository.GetAllPages();

                foreach (var page in pages)
                {
                    var wikifier = new Wikifier(context, page, null, Request.QueryString, new WikiMatchType[] { WikiMatchType.Function });
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
                    page.Id = PageRepository.SavePage(page);
                    var wikifier = new Wikifier(context, page, null, Request.QueryString, new WikiMatchType[] { WikiMatchType.Function });
                    PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
                    PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
                    var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
                    PageRepository.SavePageTokens(pageTokens);

                    Cache.ClearClass($"Page:{page.Navigation}");
                }
            }
            else if (action == "truncatepagerevisionhistory")
            {
                PageRepository.TruncateAllPageHistory("YES");
                Cache.Clear();
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Roles()
        {
            if (context.CanAdmin == false)
            {
                return new HttpUnauthorizedResult();
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
                return new HttpUnauthorizedResult();
            }

            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

            navigation = WikiUtility.CleanPartialURI(navigation);

            var model = new AccountModel()
            {
                Account = UserRepository.GetUserByNavigation(navigation)
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
        public ActionResult Account([Bind(Exclude = "Avatar")] AccountModel model)
        {
            if (context.CanAdmin == false)
            {
                return new HttpUnauthorizedResult();
            }

            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

            model.Account.Navigation = WikiUtility.CleanPartialURI(model.Account.AccountName.ToLower());
            var user = UserRepository.GetUserByNavigation(model.Account.Navigation);

            HttpPostedFileBase file = Request.Files["Avatar"];
            if (file != null && file.ContentLength > 0)
            {
                try
                {
                    var imageBytes = Utility.ConvertHttpFileToBytes(file);
                    //This is just to ensure this is a valid image:
                    var image = System.Drawing.Image.FromStream(new MemoryStream(imageBytes));
                    UserRepository.UpdateUserAvatar(user.Id, imageBytes);
                }
                catch
                {
                    ModelState.AddModelError("Avatar", "Could not save the attached image.");
                }
            }

            if (ModelState.IsValid)
            {
                if (user.Navigation.ToLower() != model.Account.Navigation.ToLower())
                {
                    var checkName = UserRepository.GetUserByNavigation(WikiUtility.CleanPartialURI(model.Account.AccountName.ToLower()));
                    if (checkName != null)
                    {
                        ModelState.AddModelError("AccountName", "Account name is already in use.");
                        return View(model);
                    }
                }

                if (user.EmailAddress.ToLower() != model.Account.EmailAddress.ToLower())
                {
                    var checkName = UserRepository.GetUserByEmail(model.Account.EmailAddress.ToLower());
                    if (checkName != null)
                    {
                        ModelState.AddModelError("EmailAddress", "Email address is already in use.");
                        return View(model);
                    }
                }

                user.AboutMe = model.Account.AboutMe;
                user.FirstName = model.Account.FirstName;
                user.LastName = model.Account.LastName;
                user.TimeZone = model.Account.TimeZone;
                user.Country = model.Account.Country;
                user.AccountName = model.Account.AccountName;
                user.Navigation = WikiUtility.CleanPartialURI(model.Account.AccountName);
                user.EmailAddress = model.Account.EmailAddress;
                user.ModifiedDate = DateTime.UtcNow;
                UserRepository.UpdateUser(user);

                ViewBag.Success = "Your profile has been saved successfully!.";
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Accounts(int page)
        {
            if (context.CanAdmin == false)
            {
                return new HttpUnauthorizedResult();
            }

            if (page <= 0) page = 1;

            var model = new AccountsModel()
            {
                Users = UserRepository.GetAllUsers(page)
            };

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Config()
        {
            if (context.CanAdmin == false)
            {
                return new HttpUnauthorizedResult();
            }

            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

            var model = new ConfigurationModel();
            model.Nest = ConfigurationRepository.GetConfigurationNest();
            return View(model);
        }

        [Authorize]
        [HttpPost, ValidateInput(false)]
        public ActionResult Config([Bind(Exclude = "Avatar")] ConfigurationModel model)
        {
            if (context.CanAdmin == false)
            {
                return new HttpUnauthorizedResult();
            }

            ViewBag.TimeZones = TimeZoneItem.GetAll();
            ViewBag.Countries = CountryItem.GetAll();

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
                ViewBag.Success = "The configuration has been saved successfully!";
            }

            var newModel = new ConfigurationModel();
            newModel.Nest = ConfigurationRepository.GetConfigurationNest();
            return View(newModel);
        }
    }
}
