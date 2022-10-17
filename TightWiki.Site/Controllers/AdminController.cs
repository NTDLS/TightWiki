using TightWiki.Shared.Library;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using System.Web.Mvc;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class AdminController : ControllerHelperBase
    {
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
        public ActionResult Config([Bind(Exclude = "Avatar")] ConfigurationModel config)
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

            var model = new ConfigurationModel();
            model.Nest = ConfigurationRepository.GetConfigurationNest();
            return View(model);
        }
    }
}
