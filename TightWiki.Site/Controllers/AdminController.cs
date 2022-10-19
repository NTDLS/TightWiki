using TightWiki.Shared.Library;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using System.Web.Mvc;
using TightWiki.Shared.Wiki;
using System.Linq;
using System.Web;
using System;
using System.IO;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class AdminController : ControllerHelperBase
    {
        [Authorize]
        [HttpGet]
        public ActionResult Pages(int page)
        {
            if (context.Roles?.Contains(Constants.Roles.Administrator) != true
                && context.CanModerate == false)
            {
                return new HttpUnauthorizedResult();
            }

            if (page <= 0) page = 1;

            string searchTokens = Request.QueryString["Tokens"];
            if (searchTokens != null)
            {
                searchTokens = string.Join(",", searchTokens.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries));
            }

            var model = new PagesModel()
            {
                Pages = PageRepository.GetAllPages(page, 0, searchTokens),
                SearchTokens = Request.QueryString["Tokens"]
            };

            if (model.Pages != null && model.Pages.Any())
            {
                ViewBag.Config.Title = $"{model.Pages.First().Name} History";
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
            if (context.Roles?.Contains(Constants.Roles.Administrator) != true
                && context.CanModerate == false)
            {
                return new HttpUnauthorizedResult();
            }

            page = 1;

            string searchTokens = null;
            if (model.SearchTokens != null)
            {
                searchTokens = string.Join(",", model.SearchTokens.Split(new char[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries));
            }

            model = new PagesModel()
            {
                Pages = PageRepository.GetAllPages(page, 0, searchTokens),
                SearchTokens = model.SearchTokens
            };

            if (model.Pages != null && model.Pages.Any())
            {
                ViewBag.Config.Title = $"{model.Pages.First().Name} History";
                ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpGet]
        public ActionResult Roles()
        {
            if (context.Roles?.Contains(Constants.Roles.Administrator) != true)
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
            if (context.Roles?.Contains(Constants.Roles.Administrator) != true)
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
            if (context.Roles?.Contains(Constants.Roles.Administrator) != true)
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
            if (context.Roles?.Contains(Constants.Roles.Administrator) != true)
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
            if (context.Roles?.Contains(Constants.Roles.Administrator) != true)
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
            if (context.Roles?.Contains(Constants.Roles.Administrator) != true)
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

