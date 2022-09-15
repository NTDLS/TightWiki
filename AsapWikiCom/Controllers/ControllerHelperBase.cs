using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Repository;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;

namespace AsapWikiCom.Controllers
{
    public class ControllerHelperBase: Controller
    {
        public StateContext context = new StateContext();

        public string RouteValue(string key, string defaultValue = "")
        {
            if (RouteData.Values.ContainsKey(key))
            {
                return RouteData.GetRequiredString(key);
            }
            return defaultValue;
        }

        public void Configure()
        {
            var config = ConfigurationEntryRepository.GetConfigurationEntryValuesByGroupName("Basic");
            ViewBag.Name = config.Where(o => o.Name == "Name").FirstOrDefault()?.Value;
            ViewBag.Title = ViewBag.Name; //Default the title to the name. This will be replaced when the page is found and loaded.
            ViewBag.FooterBlurb = config.Where(o => o.Name == "FooterBlurb").FirstOrDefault()?.Value;
            ViewBag.Copyright = config.Where(o => o.Name == "Copyright").FirstOrDefault()?.Value;

        }
    }
}
