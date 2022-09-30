﻿using AsapWiki.Shared.Models;
using AsapWiki.Shared.Repository;
using AsapWiki.Shared.Wiki;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;

namespace AsapWikiCom.Controllers
{
    [Authorize]
    public class WikiController : ControllerHelperBase
    {
        #region Content.

        [AllowAnonymous]
        public ActionResult Content()
        {
            Configure();
            if (context.CanView == false)
            {
                return new HttpUnauthorizedResult();
            }

            string navigation = WikiUtility.CleanPartialURI(RouteValue("navigation"));

            var page = PageRepository.GetPageByNavigation(navigation);
            if (page != null)
            {
                context.SetPageId(page.Id);

                var wiki = new Wikifier(context, page);
                ViewBag.Title = page.Name;
                ViewBag.Body = wiki.ProcessedBody;
            }
            else
            {
                var notExistPageName = ConfigurationEntryRepository.Get<string>("Basic", "Page Not Exists Page");
                string notExistPageNavigation = WikiUtility.CleanPartialURI(notExistPageName);
                var notExistsPage = PageRepository.GetPageByNavigation(notExistPageNavigation);

                context.SetPageId(null);

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
        public ActionResult Edit()
        {
            Configure();
            if (context.CanEdit == false)
            {
                return new HttpUnauthorizedResult();
            }

            string navigation = WikiUtility.CleanPartialURI(RouteValue("navigation"));

            var page = PageRepository.GetPageByNavigation(navigation);
            if (page != null)
            {
                context.SetPageId(page.Id);

                //Editing an existing page.
                ViewBag.Title = page.Name;

                return View(new EditPage()
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
                var pageName = Request.QueryString["Name"] ?? navigation;

                string templateName = ConfigurationEntryRepository.Get<string>("Basic", "New Page Template");
                string templateNavigation = WikiUtility.CleanPartialURI(templateName);
                var templatePage = PageRepository.GetPageByNavigation(templateNavigation);

                if (templatePage == null)
                {
                    templatePage = new Page();
                }

                return View(new EditPage()
                {
                    Body = templatePage.Body,
                    Name = pageName,
                    Navigation = WikiUtility.CleanPartialURI(navigation)
                });
            }
        }

        [Authorize]
        [HttpPost]
        public ActionResult Edit(EditPage editPage)
        {
            Configure();
            if (context.CanEdit == false)
            {
                return new HttpUnauthorizedResult();
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

                    return RedirectToAction("Edit", "Wiki", new { navigation = page.Navigation });
                }
                else
                {
                    page = PageRepository.GetPageById(editPage.Id);
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

                    return View(new EditPage()
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
