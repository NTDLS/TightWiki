using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;
using System;
using System.Linq;
using System.Web.Mvc;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class PageController : ControllerHelperBase
    {
        #region Content.
        [Authorize]
        [HttpGet]
        public ActionResult History(string pageNavigation, int page)
        {
            if (context.Roles?.Contains(Constants.Roles.Guest) == true && context.Config.AllowGuestsToViewHistory == false)
            {
                return new HttpUnauthorizedResult();
            }

            if (context.CanView == false)
            {
                return new HttpUnauthorizedResult();
            }

            if (page <= 0) page = 1;

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var result = new PageHistoryModel()
            {
                History = PageRepository.GetPageRevisionHistoryInfoByNavigation(pageNavigation, page)
            };

            foreach (var p in result.History)
            {
                var thisRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeSummary = Differentiator.GetComparisionSummary(thisRev.Body, prevRev?.Body ?? "");
            }

            if (result.History != null && result.History.Any())
            {
                context.SetPageId(result.History.First().PageId);
                ViewBag.Config.Title = $"{result.History.First().Name} History";
                ViewBag.PaginationCount = result.History.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if(page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(result);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Revert(string pageNavigation, int pageRevision, PageRevertModel model)
        {
            if (context.CanModerate == false)
            {
                return new HttpUnauthorizedResult();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            bool confirmAction = bool.Parse(Request.Form["Action"]);
            if (confirmAction == true)
            {
                var revisionPage = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision);
                SavePage(revisionPage);
            }

            return RedirectToAction("Display", "Page", new { pageNavigation = pageNavigation });
        }

        [Authorize]
        [HttpGet]
        public ActionResult Revert(string pageNavigation, int pageRevision)
        {
            if (context.CanModerate == false)
            {
                return new HttpUnauthorizedResult();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var mostCurrentPage = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            var revisionPage = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision);

            ViewBag.PageName = revisionPage.Name;
            ViewBag.CountOfRevisions = mostCurrentPage.Revision - revisionPage.Revision;
            ViewBag.MostCurrentRevision = mostCurrentPage.Revision;

            var result = new PageRevertModel()
            {

            };

            if (revisionPage != null)
            {
                context.SetPageId(revisionPage.Id, pageRevision);
                ViewBag.Config.Title = $"{revisionPage.Name} Revert";
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

                var wiki = new Wikifier(context, page, pageRevision, Request.QueryString);
                ViewBag.Config.Title = page.Name;
                ViewBag.Body = wiki.ProcessedBody;
            }
            else if (pageRevision != null)
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Basic", "Revision Does Not Exists Page");
                string notExistPageNavigation = WikiUtility.CleanPartialURI(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation);

                context.SetPageId(null, pageRevision);

                var wiki = new Wikifier(context, notExistsPage, null, Request.QueryString);
                ViewBag.Config.Title = notExistsPage.Name;
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

                context.SetPageId(null, null);

                var wiki = new Wikifier(context, notExistsPage, null, Request.QueryString);
                ViewBag.Config.Title = notExistsPage.Name;
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
                ViewBag.Config.Title = page.Name;

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
                ViewBag.Error = "The page name cannot be empty.";

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

                    page.Id = SavePage(page);

                    context.SetPageId(page.Id);

                    if (ModelState.IsValid)
                    {
                        ViewBag.Success = "The page was successfully created!";
                    }

                    return RedirectToAction("Edit", "Page", new { pageNavigation = page.Navigation });
                }
                else
                {
                    string originalNavigation = string.Empty;

                    page = PageRepository.GetPageRevisionById(editPage.Id);

                    if (page.Navigation != editPage.Navigation)
                    {
                        originalNavigation = page.Navigation; //So we can clear cache.
                    }

                    page.ModifiedDate = DateTime.UtcNow;
                    page.ModifiedByUserId = context.User.Id;
                    page.Body = editPage.Body ?? "";
                    page.Name = editPage.Name;
                    page.Navigation = WikiUtility.CleanPartialURI(editPage.Name);
                    page.Description = editPage.Description ?? "";

                    SavePage(page);

                    context.SetPageId(page.Id);

                    if (ModelState.IsValid)
                    {
                        ViewBag.Success = "The page was saved successfully!";
                    }

                    if (page != null && string.IsNullOrWhiteSpace(originalNavigation) == false)
                    {
                        Cache.ClearClass($"Page:{originalNavigation}");
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
