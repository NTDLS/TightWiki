using System;
using System.Linq;
using System.Web.Mvc;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;

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

            var model = new PageHistoryModel()
            {
                History = PageRepository.GetPageRevisionHistoryInfoByNavigation(pageNavigation, page)
            };

            foreach (var p in model.History)
            {
                var thisRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeSummary = Differentiator.GetComparisionSummary(thisRev.Body, prevRev?.Body ?? "");
            }

            if (model.History != null && model.History.Any())
            {
                context.SetPageId(model.History.First().PageId);
                ViewBag.Config.Title = $"{model.History.First().Name} History";
                ViewBag.PaginationCount = model.History.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if(page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Delete(string pageNavigation, PageDeleteModel model)
        {
            if (context.CanDelete == false)
            {
                return new HttpUnauthorizedResult();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);

            bool confirmAction = bool.Parse(Request.Form["Action"]);
            if (confirmAction == true && page != null)
            {
                PageRepository.DeletePageById(page.Id);
                Cache.ClearClass($"Page:{page.Navigation}");
            }

            return RedirectToAction("Display", "Page", new { pageNavigation = "Home" });
        }

        [Authorize]
        [HttpGet]
        public ActionResult Delete(string pageNavigation)
        {
            if (context.CanDelete == false)
            {
                return new HttpUnauthorizedResult();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);

            ViewBag.PageName = page.Name;
            ViewBag.MostCurrentRevision = page.Revision;
            ViewBag.CountOfAttachments = PageRepository.GetCountOfPageAttachmentsById(page.Id);

            var model = new PageDeleteModel()
            {
            };

            PageRepository.DeletePageById(page.Id);

            if (page != null)
            {
                context.SetPageId(page.Id, page.Revision);
                ViewBag.Config.Title = $"{page.Name} Delete";
            }

            return View(model);
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
                var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision);
                SavePage(page);
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

            var model = new PageRevertModel()
            {

            };

            if (revisionPage != null)
            {
                context.SetPageId(revisionPage.Id, pageRevision);
                ViewBag.Config.Title = $"{revisionPage.Name} Revert";
            }

            return View(model);
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
        public ActionResult Edit(EditPageModel model)
        {
            if (context.CanEdit == false)
            {
                return new HttpUnauthorizedResult();
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                ViewBag.Error = "The page name cannot be empty.";

                return View(new EditPageModel()
                {
                    Id = model.Id,
                    Body = model.Body,
                    Name = model.Name,
                    Navigation = model.Navigation,
                    Description = model.Description
                });
            }

            if (ModelState.IsValid)
            {
                Page page;

                if (model.Id == 0) //Saving a new page.
                {
                    page = new Page()
                    {
                        CreatedDate = DateTime.UtcNow,
                        CreatedByUserId = context.User.Id,
                        ModifiedDate = DateTime.UtcNow,
                        ModifiedByUserId = context.User.Id,
                        Body = model.Body ?? "",
                        Name = model.Name,
                        Navigation = WikiUtility.CleanPartialURI(model.Name),
                        Description = model.Description ?? ""
                    };

                    if (PageRepository.GetPageInfoByNavigation(page.Navigation) != null)
                    {
                        ViewBag.Error = "The page name you entered already exists.";
                        return View(model);
                    }

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

                    page = PageRepository.GetPageRevisionById(model.Id);

                    model.Navigation = WikiUtility.CleanPartialURI(model.Name);

                    if (page.Navigation.ToLower() != model.Navigation.ToLower())
                    {
                        if (PageRepository.GetPageInfoByNavigation(model.Navigation) != null)
                        {
                            ViewBag.Error = "The page name you entered already exists.";
                            return View(model);
                        }

                        originalNavigation = page.Navigation; //So we can clear cache.
                    }

                    page.ModifiedDate = DateTime.UtcNow;
                    page.ModifiedByUserId = context.User.Id;
                    page.Body = model.Body ?? "";
                    page.Name = model.Name;
                    page.Navigation = WikiUtility.CleanPartialURI(model.Name);
                    page.Description = model.Description ?? "";

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
