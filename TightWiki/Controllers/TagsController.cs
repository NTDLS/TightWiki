using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using System.Text;
using TightWiki.Engine.Implementation.Utility;
using TightWiki.Library;
using TightWiki.Models.ViewModels.Page;
using TightWiki.Repository;
using static TightWiki.Library.Constants;

namespace TightWiki.Controllers
{
    [Authorize]
    public class TagsController(
        SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager,
        IStringLocalizer<TagsController> localizer)
        : WikiControllerBase<TagsController>(signInManager, userManager, localizer)
    {
        [AllowAnonymous]
        public ActionResult Browse(string givenCanonical)
        {
            SessionState.RequirePermission(givenCanonical, Permission.Read);

            SessionState.Page.Name = Localize("Tags");

            givenCanonical = NamespaceNavigation.CleanAndValidate(givenCanonical);

            string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
            var pages = PageRepository.GetPageInfoByTag(givenCanonical).OrderBy(o => o.Name).ToList();
            var glossaryHtml = new StringBuilder();
            var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpperInvariant()).Distinct();

            if (pages.Count > 0)
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
                    foreach (var page in pages.Where(p => p.Name.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
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

            var model = new BrowseViewModel
            {
                AssociatedPages = glossaryHtml.ToString(),
                TagCloud = TagCloud.Build(givenCanonical, 100)
            };

            return View(model);
        }
    }
}
