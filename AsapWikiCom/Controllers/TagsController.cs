using AsapWiki.Shared.Classes;
using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;
using AsapWiki.Shared.Wiki;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace AsapWikiCom.Controllers
{
    [Authorize]
    public class TagsController : ControllerHelperBase
    {
        [AllowAnonymous]
        public ActionResult Browse()
        {
            Configure();
            string navigation = Utility.CleanPartialURI(RouteValue("navigation"));

            ViewBag.TagCloud = Utility.BuildTagCloud(navigation);

            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
            var pages = PageTagRepository.GetPageInfoByTag(navigation).OrderBy(o => o.Name).ToList();
            var glossaryHtml = new StringBuilder();
            var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpper()).Distinct();

            if (pages.Count() > 0)
            {
                glossaryHtml.Append("<center>");
                foreach (var alpha in alphabet)
                {
                    glossaryHtml.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                }
                glossaryHtml.Append("</center>");

                glossaryHtml.Append("<ul>");
                foreach (var alpha in alphabet)
                {
                    glossaryHtml.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                    glossaryHtml.Append("<ul>");
                    foreach (var page in pages.Where(p => p.Name.ToLower().StartsWith(alpha.ToLower())))
                    {
                        glossaryHtml.Append("<li><a href=\"/Wiki/Content/" + page.Navigation + "\">" + page.Name + "</a>");

                        if (page.Description.Length > 0)
                        {
                            glossaryHtml.Append(" - " + page.Description);
                        }
                        glossaryHtml.Append("</li>");
                    }
                    glossaryHtml.Append("</ul>");
                }

                glossaryHtml.Append("</ul>");
            }

            ViewBag.Pages = glossaryHtml.ToString();


            return View();
        }
    }
}
