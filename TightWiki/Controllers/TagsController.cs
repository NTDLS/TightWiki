using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TightWiki.Controllers;
using TightWiki.Library;
using TightWiki.Library.Repository;
using TightWiki.Library.Wiki;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class TagsController : ControllerHelperBase
    {
        public TagsController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }

        [AllowAnonymous]
        public ActionResult Browse(string navigation)
        {
            context.RequireViewPermission();

            ViewBag.Context.Title = "Tags";

            navigation = NamespaceNavigation.CleanAndValidate(navigation);

            ViewBag.TagCloud = WikiUtility.BuildTagCloud(navigation);

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
                        glossaryHtml.Append("<li><a href=\"/" + page.Navigation + "\">" + page.Name + "</a>");

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
