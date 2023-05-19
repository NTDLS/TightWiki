using DuoVia.FuzzyStrings;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TightWiki.Controllers;
using TightWiki.Shared;
using TightWiki.Shared.Library;
using TightWiki.Shared.Models.Data;
using TightWiki.Shared.Models.View;
using TightWiki.Shared.Repository;
using TightWiki.Shared.Wiki;
using static TightWiki.Shared.Library.Constants;

namespace TightWiki.Site.Controllers
{
    [Authorize]
    public class PageController : ControllerHelperBase
    {
        [AllowAnonymous]
        [Route("/robots.txt")]
        public ContentResult RobotsTxt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *")
                .AppendLine("Allow: /");

            return this.Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Search(int page)
        {
            ViewBag.Context.Title = $"Page Search";

            if (page <= 0) page = 1;

            string searchTokens = Request.Query["Tokens"];
            if (searchTokens != null)
            {
                var tokens = searchTokens.Split(new char[] { ' ', '\t', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries).Select(o => o.ToLower()).Distinct();

                var searchTerms = (from o in tokens
                                   select new PageToken
                                   {
                                       Token = o,
                                       DoubleMetaphone = o.ToDoubleMetaphone()
                                   }).ToList();

                var model = new PageSearchModel()
                {
                    Pages = PageRepository.PageSearchPaged(searchTerms, page, 0),
                    SearchTokens = Request.Query["Tokens"]
                };

                if (model.Pages != null && model.Pages.Any())
                {
                    ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                    ViewBag.CurrentPage = page;

                    if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                    if (page > 1) ViewBag.PreviousPage = page - 1;
                }

                return View(model);
            }

            return View(new PageSearchModel()
            {
                Pages = new List<Page>(),
                SearchTokens = String.Empty
            });
        }

        [AllowAnonymous]
        [HttpPost]
        public ActionResult Search(int page, PageSearchModel model)
        {
            ViewBag.Context.Title = $"Page Search";

            page = 1;

            var searchTerms = new List<PageToken>();
            if (model.SearchTokens != null)
            {
                var tokens = model.SearchTokens.Split(new char[] { ' ', '\t', '_', '-' }, System.StringSplitOptions.RemoveEmptyEntries).Select(o => o.ToLower()).Distinct();

                searchTerms.AddRange((from o in tokens
                                      select new PageToken
                                      {
                                          Token = o,
                                          DoubleMetaphone = o.ToDoubleMetaphone()
                                      }).ToList());
            }

            model = new PageSearchModel()
            {
                Pages = PageRepository.PageSearchPaged(searchTerms, page, 0),
                SearchTokens = model.SearchTokens
            };

            if (model.Pages != null && model.Pages.Any())
            {
                ViewBag.PaginationCount = model.Pages.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult Comments(string pageNavigation, int page)
        {
            if (context.CanView == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var pageInfo = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (pageInfo == null)
            {
                return NotFound();
            }

            if (page <= 0) page = 1;

            var deleteAction = Request.Query["Delete"].ToString();
            if (string.IsNullOrEmpty(deleteAction) == false && context.IsAuthenticated)
            {
                PageRepository.DeletePageCommentById(pageInfo.Id, context.User.Id, int.Parse(deleteAction));
            }

            var model = new PageCommentsModel()
            {
                Comments = PageRepository.GetPageCommentsPaged(pageNavigation, page)
            };

            model.Comments.ForEach(o =>
            {
                o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
            });

            context.SetPageId(pageInfo.Id);
            ViewBag.Context.Title = $"{pageInfo.Name}";

            if (model.Comments != null && model.Comments.Any())
            {
                ViewBag.PaginationCount = model.Comments.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Comments(PageCommentsModel model, string pageNavigation, int page)
        {
            if (context.CanEdit == false)
            {
                return Unauthorized();
            }

            string errorMessage = null;

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var pageInfo = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (pageInfo == null)
            {
                return NotFound();
            }

            if (ModelState.IsValid && string.IsNullOrWhiteSpace(model.Comment) == false && model.Comment.Trim().Length > 1)
            {
                PageRepository.InsetPageComment(pageInfo.Id, context.User.Id, model.Comment);
            }
            else
            {
                errorMessage = "A valid comment is over 1 character, not including whitespace.";
            }

            if (page <= 0) page = 1;

            model = new PageCommentsModel()
            {
                Comments = PageRepository.GetPageCommentsPaged(pageNavigation, page),
                ErrorMessage = errorMessage
            };

            model.Comments.ForEach(o =>
            {
                o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
            });

            context.SetPageId(pageInfo.Id);
            ViewBag.Context.Title = $"{pageInfo.Name} Comments";

            if (model.Comments != null && model.Comments.Any())
            {
                ViewBag.PaginationCount = model.Comments.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        #region Content.
        [Authorize]
        [HttpGet]
        public ActionResult History(string pageNavigation, int page)
        {
            if (context.CanView == false)
            {
                return Unauthorized();
            }

            if (page <= 0) page = 1;

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var model = new PageHistoryModel()
            {
                History = PageRepository.GetPageRevisionHistoryInfoByNavigationPaged(pageNavigation, page)
            };

            model.History.ForEach(o =>
            {
                o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = context.LocalizeDateTime(o.ModifiedDate);
            });

            foreach (var p in model.History)
            {
                var thisRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeSummary = Differentiator.GetComparisionSummary(thisRev.Body, prevRev?.Body ?? "");
            }

            if (model.History != null && model.History.Any())
            {
                context.SetPageId(model.History.First().PageId);
                ViewBag.Context.Title = $"{model.History.First().Name} History";
                ViewBag.PaginationCount = model.History.First().PaginationCount;
                ViewBag.CurrentPage = page;

                if (page < ViewBag.PaginationCount) ViewBag.NextPage = page + 1;
                if (page > 1) ViewBag.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost]
        public ActionResult Delete(string pageNavigation, PageDeleteModel model)
        {
            if (context.CanDelete == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
            if (instructions.Any(o => o.Instruction == WikiInstruction.Protect))
            {
                return Unauthorized();
            }

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
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);

            ViewBag.PageName = page.Name;
            ViewBag.MostCurrentRevision = page.Revision;
            ViewBag.CountOfAttachments = PageRepository.GetCountOfPageAttachmentsById(page.Id);

            var model = new PageDeleteModel()
            {
            };

            if (page != null)
            {
                context.SetPageId(page.Id);
                ViewBag.Context.Title = $"{page.Name} Delete";
            }

            var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);

            if (instructions.Any(o => o.Instruction == WikiInstruction.Protect))
            {
                model.ErrorMessage = "The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion.";
                return View(model);
            }

            return View(model);
        }


        [Authorize]
        [HttpPost]
        public ActionResult Revert(string pageNavigation, int pageRevision, PageRevertModel model)
        {
            if (context.CanModerate == false)
            {
                return Unauthorized();
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
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var mostCurrentPage = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            mostCurrentPage.CreatedDate = context.LocalizeDateTime(mostCurrentPage.CreatedDate);
            mostCurrentPage.ModifiedDate = context.LocalizeDateTime(mostCurrentPage.ModifiedDate);

            var revisionPage = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision);
            revisionPage.CreatedDate = context.LocalizeDateTime(revisionPage.CreatedDate);
            revisionPage.ModifiedDate = context.LocalizeDateTime(revisionPage.ModifiedDate);

            ViewBag.PageName = revisionPage.Name;
            ViewBag.CountOfRevisions = mostCurrentPage.Revision - revisionPage.Revision;
            ViewBag.MostCurrentRevision = mostCurrentPage.Revision;

            var model = new PageRevertModel();

            if (revisionPage != null)
            {
                context.SetPageId(revisionPage.Id, pageRevision);
                ViewBag.Context.Title = $"{revisionPage.Name} Revert";
            }

            return View(model);
        }

        [AllowAnonymous]
        public ActionResult Display(string pageNavigation, int? pageRevision)
        {
            Singletons.DoFirstRun(this);

            var model = new DisplayModel();

            if (context.CanView == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision);
            if (page != null)
            {
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                ViewBag.HideFooterComments = instructions.Where(o => o.Instruction == WikiInstruction.HideFooterComments).Any();

                if (page.Revision == page.LatestRevision)
                {
                    pageRevision = null;
                }

                context.SetPageId(page.Id, pageRevision);
                ViewBag.Context.Title = page.Title;

                bool allowCache = GlobalSettings.PageCacheSeconds > 0;

                if (allowCache)
                {
                    string queryKey = string.Empty;
                    foreach (var query in Request.Query)
                    {
                        queryKey += $"{query.Key}:{query.Value}";
                    }

                    string cacheKey = $"Page:{page.Navigation}:{page.Revision}:{queryKey}";
                    var result = Cache.Get<string>(cacheKey);
                    if (result != null)
                    {
                        model.Body = result;
                        Cache.Put(cacheKey, result); //Update the cache expiration.
                    }
                    else
                    {
                        var wiki = new Wikifier(context, page, pageRevision, Request.Query);

                        if (GlobalSettings.WritePageStatistics)
                        {
                            PageRepository.InsertPageStatistics(page.Id,
                                wiki.ProcessingTime.TotalMilliseconds,
                                wiki.MatchCount,
                                wiki.ErrorCount,
                                wiki.OutgoingLinks.Count,
                                wiki.Tags.Count,
                                wiki.ProcessedBody.Length,
                                page.Body.Length);
                        }

                        model.Body = wiki.ProcessedBody;

                        if (wiki.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                        {
                            Cache.Put(cacheKey, wiki.ProcessedBody, GlobalSettings.PageCacheSeconds); //This is cleared with the call to Cache.ClearClass($"Page:{page.Navigation}");
                        }
                    }
                }
                else
                {
                    var wiki = new Wikifier(context, page, pageRevision, Request.Query);

                    if (GlobalSettings.WritePageStatistics)
                    {
                        PageRepository.InsertPageStatistics(page.Id,
                            wiki.ProcessingTime.TotalMilliseconds,
                            wiki.MatchCount,
                            wiki.ErrorCount,
                            wiki.OutgoingLinks.Count,
                            wiki.Tags.Count,
                            wiki.ProcessedBody.Length,
                            page.Body.Length);
                    }
                    model.Body = wiki.ProcessedBody;
                }

                if (GlobalSettings.EnablePageComments && GlobalSettings.ShowCommentsOnPageFooter && ViewBag.HideFooterComments == false)
                {
                    model.Comments = PageRepository.GetPageCommentsPaged(pageNavigation, 1);
                }
            }
            else if (pageRevision != null)
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Revision Does Not Exists Page");
                string notExistPageNavigation = WikiUtility.CleanPartialURI(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation);

                context.SetPageId(null, pageRevision);

                var wiki = new Wikifier(context, notExistsPage, null, Request.Query);
                ViewBag.Context.Title = notExistsPage.Name;
                model.Body = wiki.ProcessedBody;

                if (context.IsAuthenticated && context.CanCreate)
                {
                    ViewBag.CreatePage = false;
                }
            }
            else
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Page Not Exists Page");
                string notExistPageNavigation = WikiUtility.CleanPartialURI(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation);

                context.SetPageId(null, null);

                var wiki = new Wikifier(context, notExistsPage, null, Request.Query);
                ViewBag.Context.Title = notExistsPage.Name;
                model.Body = wiki.ProcessedBody;

                if (context.IsAuthenticated && context.CanCreate)
                {
                    ViewBag.CreatePage = true;
                }
            }

#if !DEBUG
            if (ConfigurationRepository.IsAdminPasswordDefault(DEFAULTPASSWORD))
            {
                StringBuilder text = new StringBuilder();
                text.Append("The admin password is set to its default value, it is recommended that you change it immediately!<br />");
                text.Append("<br />");
                text.Append("You can change this password by logging in and changing the password on the My-&gt;Change Password page or by running the stored procedure <i>SetUserPasswordHash</i> in the TightWiki database.<br />");
                text.Append("<br />");
                text.Append("<strong>Current admin login</strong><br />");
                text.Append("&nbsp;&nbsp;&nbsp;<strong>Username:</strong> admin<br />");
                text.Append($"&nbsp;&nbsp;&nbsp;<strong>Password:</strong> \"{DEFAULTPASSWORD}\"<br /> ");
                text.Append("<br />");

                model.Body = Utility.WarningCard("Default password has not been changed", text.ToString()) + model.Body;
            }
#endif

            if (page != null)
            {
                model.ModifiedByUserName = page.ModifiedByUserName;
                model.ModifiedDate = context.LocalizeDateTime(page.ModifiedDate);
            }

            return View(model);
        }

        #endregion

        #region Edit.

        [Authorize]
        [HttpGet]
        public ActionResult Edit(string pageNavigation)
        {
            if (context.CanEdit == false)
            {
                return Unauthorized();
            }

            pageNavigation = WikiUtility.CleanPartialURI(pageNavigation);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                context.SetPageId(page.Id);

                //Editing an existing page.
                ViewBag.Context.Title = page.Title;

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
                var pageName = Request.Query["Name"].ToString().IsNullOrEmpty(pageNavigation);

                string templateName = ConfigurationRepository.Get<string>("Customization", "New Page Template");
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
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(model.Name))
            {
                model.ErrorMessage = "The page name cannot be empty.";
                return View(model);
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
                        model.ErrorMessage = "The page name you entered already exists.";
                        return View(model);
                    }

                    page.Id = SavePage(page);

                    context.SetPageId(page.Id);

                    if (ModelState.IsValid)
                    {
                        model.SuccessMessage = "The page was successfully created!";
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
                            model.ErrorMessage = "The page name you entered already exists.";
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

                    ViewBag.Context.Title = page.Title;

                    SavePage(page);

                    context.SetPageId(page.Id);

                    if (ModelState.IsValid)
                    {
                        model.SuccessMessage = "The page was saved successfully!";
                    }

                    if (page != null && string.IsNullOrWhiteSpace(originalNavigation) == false)
                    {
                        Cache.ClearClass($"Page:{originalNavigation}");
                    }

                    return View(model);
                }
            }

            return View(model);
        }

        #endregion
    }
}
