using TightWiki.Shared.Library;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using System.Web.Mvc;
using TightWiki.Shared.Wiki;
using System.Linq;

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

