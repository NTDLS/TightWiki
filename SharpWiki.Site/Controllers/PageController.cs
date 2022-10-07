using SharpWiki.Shared.Models.Data;
using SharpWiki.Shared.Models.View;
using SharpWiki.Shared.Repository;
using SharpWiki.Shared.Wiki;
using System;
using System.Linq;
using System.Web.Mvc;

namespace SharpWiki.Site.Controllers
{
    [Authorize]
    public class PageController : ControllerHelperBase
    {
        #region Content.
        [AllowAnonymous]
        public ActionResult History(string pageNavigation, int page)
        {
            if (page <= 0) page = 1;

            if (context.CanView == false)
            {
                return new HttpUnauthorizedResult();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var result = new PageHistoryModel()
            {
                History = PageRepository.GetPageRevisionHistoryInfoByNavigation(pageNavigation, page)
            };

            if (result.History != null && result.History.Any())
            {
                context.SetPageId(result.History.First().PageId);
                ViewBag.Title = $"{result.History.First().Name} History";
                ViewBag.PaginationCount = result.History.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if(page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(result);
        }


        [AllowAnonymous]
        public ActionResult Display(string pageNavigation, int? pageRevision)
        {
            if (context.CanView == false)
            {
                return new HttpUnauthorizedResult();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision);
            if (page != null)
            {
                if (page.Revision == page.LatestRevision)
                {
                    pageRevision = null;
                }

                context.SetPageId(page.Id, pageRevision);

                var wiki = new Wikifier(context, page, pageRevision);
                ViewBag.Title = page.Name;
                ViewBag.Body = wiki.ProcessedBody;
            }
            else if (pageRevision != null)
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Basic", "Revision Does Not Exists Page");
                string notExistPageNavigation = WikiUtility.CleanPartialURI(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation);

                context.SetPageId(null, pageRevision);

                var wiki = new Wikifier(context, notExistsPage);
                ViewBag.Title = notExistsPage.Name;
                ViewBag.Body = wiki.ProcessedBody;

                if (context.IsAuthenticated && context.CanCreate)
                {
                    ViewBag.CreatePage = false;
                }
            }
            else
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Basic", "Page Not Exists Page");
                string notExistPageNavigation = WikiUtility.CleanPartialURI(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation);

                context.SetPageId(null, pageRevision);

                var wiki = new Wikifier(context, notExistsPage);
                ViewBag.Title = notExistsPage.Name;
                ViewBag.Body = wiki.ProcessedBody;

                if (context.IsAuthenticated && context.CanCreate)
                {
                    ViewBag.CreatePage = true;
                }
            }

            return View();
        }

        #endregion

        #region Edit.

        [Authorize]
        [HttpGet]
        public ActionResult Edit(string pageNavigation)
        {
            if (context.CanEdit == false)
            {
                return new HttpUnauthorizedResult();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                context.SetPageId(page.Id);

                //Editing an existing page.
                ViewBag.Title = page.Name;

                return View(new EditPageModel()
                {
                    Id = page.Id,
                    Body = page.Body,
                    Name = page.Name,
                    Navigation = WikiUtility.CleanPartialURI(page.Navigation),
                    Description = page.Description
                });
            }
            else
            {
                var pageName = Request.QueryString["Name"] ?? pageNavigation;

                string templateName = ConfigurationRepository.Get<string>("Basic", "New Page Template");
                string templateNavigation = WikiUtility.CleanPartialURI(templateName);
                var templatePage = PageRepository.GetPageRevisionByNavigation(templateNavigation);

                if (templatePage == null)
                {
                    templatePage = new Page();
                }

                return View(new EditPageModel()
                {
                    Body = templatePage.Body,
                    Name = pageName?.Replace('_', ' '),
                    Navigation = WikiUtility.CleanPartialURI(pageNavigation)
                });
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(EditPageModel editPage)
        {
            if (context.CanEdit == false)
            {
                return new HttpUnauthorizedResult();
            }

            if (string.IsNullOrWhiteSpace(editPage.Name))
            {
                ViewBag.Warninig = "The page name cannot be empty.";

                return View(new EditPageModel()
                {
                    Id = editPage.Id,
                    Body = editPage.Body,
                    Name = editPage.Name,
                    Navigation = editPage.Navigation,
                    Description = editPage.Description
                });
            }

            if (ModelState.IsValid)
            {
                Page page;

                if (editPage.Id == 0) //Saving a new page.
                {
                    page = new Page()
                    {
                        CreatedDate = DateTime.UtcNow,
                        CreatedByUserId = context.User.Id,
                        ModifiedDate = DateTime.UtcNow,
                        ModifiedByUserId = context.User.Id,
                        Body = editPage.Body ?? "",
                        Name = editPage.Name,
                        Navigation = WikiUtility.CleanPartialURI(editPage.Name),
                        Description = editPage.Description ?? ""
                    };

                    page.Id = PageRepository.SavePage(page);

                    var wikifier = new Wikifier(context, page);
                    PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
                    PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
                    var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
                    PageRepository.SavePageTokens(pageTokens);

                    context.SetPageId(page.Id);

                    if (ModelState.IsValid)
                    {
                        ViewBag.Success = "The page was successfully created!";
                    }

                    return RedirectToAction("Edit", "Page", new { pageNavigation = page.Navigation });
                }
                else
                {
                    page = PageRepository.GetPageRevisionById(editPage.Id);
                    page.ModifiedDate = DateTime.UtcNow;
                    page.ModifiedByUserId = context.User.Id;
                    page.Body = editPage.Body ?? "";
                    page.Name = editPage.Name;
                    page.Navigation = WikiUtility.CleanPartialURI(editPage.Name);
                    page.Description = editPage.Description ?? "";

                    PageRepository.SavePage(page);

                    var wikifier = new Wikifier(context, page);
                    PageTagRepository.UpdatePageTags(page.Id, wikifier.Tags);
                    PageRepository.UpdatePageProcessingInstructions(page.Id, wikifier.ProcessingInstructions);
                    var pageTokens = wikifier.ParsePageTokens().Select(o => o.ToPageToken(page.Id)).ToList();
                    PageRepository.SavePageTokens(pageTokens);

                    context.SetPageId(page.Id);

                    if (ModelState.IsValid)
                    {
                        ViewBag.Success = "The page was saved successfully!";
                    }

                    return View(new EditPageModel()
                    {
                        Id = page.Id,
                        Body = page.Body,
                        Name = page.Name,
                        Navigation = page.Navigation,
                        Description = page.Description
                    });
                }
            }
            return View();
        }

        #endregion
    }
}
