using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TightWiki.Models.ViewModels.Page;
using TightWiki.Plugin;
using TightWiki.Plugin.Interfaces;
using TightWiki.Repository;

namespace TightWiki.Controllers
{
    [Authorize]
    [Route("[controller]")]
    public class TagsController(ILogger<ITwEngine> logger, SignInManager<IdentityUser> signInManager,
        UserManager<IdentityUser> userManager, ISharedLocalizationText localizer, TwConfiguration wikiConfiguration)
        : WikiControllerBase<TagsController>(logger, signInManager, userManager, localizer, wikiConfiguration)
    {
        [AllowAnonymous]
        [HttpGet("Browse/{givenCanonical}")]
        public async Task<ActionResult> Browse(string givenCanonical)
        {
            try
            {
                try
                {
                    await SessionState.RequirePermission(givenCanonical, WikiPermission.Read);
                }
                catch (Exception ex)
                {
                    return NotifyOfError(ex.GetBaseException().Message, "/");
                }
                SessionState.Page.Name = Localize("Tags");

                givenCanonical = NamespaceNavigation.CleanAndValidate(givenCanonical);

                string glossaryName = "glossary_" + (new Random()).Next(0, 1000000).ToString();
                var pages = (await PageRepository.GetPageInfoByTag(givenCanonical)).OrderBy(o => o.Name).ToList();
                var glossaryHtml = new StringBuilder();
                var alphabet = pages.Select(p => p.Name.Substring(0, 1).ToUpperInvariant()).Distinct();

                if (pages.Count > 0)
                {
                    // Alphabet jump bar.
                    glossaryHtml.Append("<div class=\"text-center mb-2\">");
                    foreach (var alpha in alphabet)
                    {
                        glossaryHtml.Append($"<a href=\"#{glossaryName}_{alpha}\" class=\"mx-1 text-decoration-none\">{alpha}</a>");
                    }
                    glossaryHtml.Append("</div>");

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
                    TagCloud = await TagCloudBuilder.Build(WikiConfiguration.BasePath, givenCanonical, 100)
                };

                return View(model);
            }
            catch (Exception ex)
            {
                Logger.LogError(ex, "Error in TagsController.Browse for tag {Tag}", givenCanonical);
                throw;
            }
        }
    }
}
