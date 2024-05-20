using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TightWiki.DataModels;
using TightWiki.Library;
using TightWiki.Repository;
using TightWiki.ViewModels.Page;
using TightWiki.Wiki;
using static TightWiki.Library.Constants;

namespace TightWiki.Controllers
{
    [Route("")]
    public class PageController : ControllerBase
    {
        public PageController(SignInManager<IdentityUser> signInManager, UserManager<IdentityUser> userManager)
            : base(signInManager, userManager)
        {
        }

        [AllowAnonymous]
        [Route("/robots.txt")]
        public ContentResult RobotsTxt()
        {
            var sb = new StringBuilder();
            sb.AppendLine("User-agent: *")
                .AppendLine("Allow: /");

            return Content(sb.ToString(), "text/plain", Encoding.UTF8);
        }

        /// <summary>
        /// Default controller for root requests. e.g. http://127.0.0.1/
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public IActionResult Display()
            => Display("home", null);

        [HttpGet("{givenCanonical}/{pageRevision:int?}")]
        public IActionResult Display(string givenCanonical, int? pageRevision)
        {
            WikiContext.RequireViewPermission();

            var model = new PageDisplayViewModel();
            var navigation = new NamespaceNavigation(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(navigation.Canonical, pageRevision);
            if (page != null)
            {
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                WikiContext.HideFooterComments = instructions.Where(o => o.Instruction == WikiInstruction.HideFooterComments).Any();

                if (page.Revision == page.LatestRevision)
                {
                    pageRevision = null;
                }

                WikiContext.SetPageId(page.Id, pageRevision);
                WikiContext.Title = page.Title;

                bool allowCache = GlobalSettings.PageCacheSeconds > 0;

                if (allowCache)
                {
                    string queryKey = string.Empty;
                    foreach (var query in Request.Query)
                    {
                        queryKey += $"{query.Key}:{query.Value}";
                    }

                    var cacheKey = WikiCacheKeyFunction.Build(WikiCache.Category.Page, [page.Navigation, page.Revision, queryKey]);
                    var result = WikiCache.Get<string>(cacheKey);
                    if (result != null)
                    {
                        model.Body = result;
                        WikiCache.Put(cacheKey, result); //Update the cache expiration.
                    }
                    else
                    {
                        var wiki = new Wikifier(WikiContext, page, pageRevision, Request.Query);

                        model.Body = wiki.ProcessedBody;

                        if (wiki.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                        {
                            WikiCache.Put(cacheKey, wiki.ProcessedBody, GlobalSettings.PageCacheSeconds); //This is cleared with the call to Cache.ClearCategory($"Page:{page.Navigation}");
                        }
                    }
                }
                else
                {
                    var wiki = new Wikifier(WikiContext, page, pageRevision, Request.Query);
                    model.Body = wiki.ProcessedBody;
                }

                if (GlobalSettings.EnablePageComments && GlobalSettings.ShowCommentsOnPageFooter && WikiContext.HideFooterComments == false)
                {
                    model.Comments = PageRepository.GetPageCommentsPaged(navigation.Canonical, 1);
                }
            }
            else if (pageRevision != null)
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Revision Does Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation).EnsureNotNull();

                WikiContext.SetPageId(null, pageRevision);

                var wiki = new Wikifier(WikiContext, notExistsPage, null, Request.Query);
                WikiContext.Title = notExistsPage.Name;
                model.Body = wiki.ProcessedBody;

                if (WikiContext.IsAuthenticated && WikiContext.CanCreate)
                {
                    WikiContext.CreatePage = false;
                }
            }
            else
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Page Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation).EnsureNotNull();

                WikiContext.SetPageId(null, null);

                var wiki = new Wikifier(WikiContext, notExistsPage, null, Request.Query);
                WikiContext.Title = notExistsPage.Name;
                model.Body = wiki.ProcessedBody;

                if (WikiContext.IsAuthenticated && WikiContext.CanCreate)
                {
                    WikiContext.CreatePage = true;
                }
            }

            if (page != null)
            {
                model.ModifiedByUserName = page.ModifiedByUserName;
                model.ModifiedDate = WikiContext.LocalizeDateTime(page.ModifiedDate);

                if (model.Comments != null)
                {
                    model.Comments.ForEach(o =>
                    {
                        o.Body = WikifierLite.Process(o.Body);
                        o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                    });
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet("/Page/Search/{page=1}")]
        public ActionResult Search(int page)
        {
            WikiContext.Title = $"Page Search";

            if (page <= 0) page = 1;

            string searchTokens = GetQueryString("Tokens").DefaultWhenNullOrEmpty(string.Empty);
            if (searchTokens != null)
            {
                var tokens = searchTokens.Split(new char[] { ' ', '\t', '_', '-' },
                    StringSplitOptions.RemoveEmptyEntries).Select(o => o.ToLower()).Distinct().ToList();

                var model = new PageSearchViewModel()
                {
                    Pages = PageRepository.PageSearchPaged(tokens, page),
                    SearchTokens = GetQueryString("Tokens").DefaultWhenNullOrEmpty(string.Empty)
                };

                if (model.Pages != null && model.Pages.Any())
                {
                    WikiContext.PaginationCount = model.Pages.First().PaginationCount;
                    WikiContext.CurrentPage = page;

                    if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                    if (page > 1) WikiContext.PreviousPage = page - 1;
                }

                return View(model);
            }

            return View(new PageSearchViewModel()
            {
                Pages = new List<Page>(),
                SearchTokens = String.Empty
            });
        }

        [AllowAnonymous]
        [HttpPost("/Page/Search/{page=1}")]
        public ActionResult Search(int page, PageSearchViewModel model)
        {
            WikiContext.Title = $"Page Search";

            if (page <= 0) page = 1;

            if (model.SearchTokens != null)
            {
                var tokens = model.SearchTokens.Split(new char[] { ' ', '\t', '_', '-' },
                    StringSplitOptions.RemoveEmptyEntries).Select(o => o.ToLower()).Distinct().ToList();

                model = new PageSearchViewModel()
                {
                    Pages = PageRepository.PageSearchPaged(tokens, page),
                    SearchTokens = GetQueryString("Tokens").DefaultWhenNullOrEmpty(string.Empty)
                };

                if (model.Pages != null && model.Pages.Any())
                {
                    WikiContext.PaginationCount = model.Pages.First().PaginationCount;
                    WikiContext.CurrentPage = page;

                    if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                    if (page > 1) WikiContext.PreviousPage = page - 1;
                }

                return View(model);
            }

            return View(new PageSearchViewModel()
            {
                Pages = new List<Page>(),
                SearchTokens = String.Empty
            });

        }

        [AllowAnonymous]
        [HttpGet("{givenCanonical}/{page:int?}/Comments")]
        [HttpGet("{givenCanonical}/Comments")]
        public ActionResult Comments(string givenCanonical, int page = 0)
        {
            WikiContext.RequireViewPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageInfo = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (pageInfo == null)
            {
                return NotFound();
            }

            if (page <= 0) page = 1;

            var deleteAction = GetQueryString("Delete");
            if (string.IsNullOrEmpty(deleteAction) == false && WikiContext.IsAuthenticated)
            {
                if (WikiContext.CanModerate)
                {
                    //Moderators and administrators can delete comments that they do not own.
                    PageRepository.DeletePageCommentById(pageInfo.Id, int.Parse(deleteAction));
                }
                else
                {
                    PageRepository.DeletePageCommentByUserAndId(pageInfo.Id, WikiContext.Profile.EnsureNotNull().UserId, int.Parse(deleteAction));
                }
            }

            var model = new PageCommentsViewModel()
            {
                Comments = PageRepository.GetPageCommentsPaged(pageNavigation, page)
            };

            model.Comments.ForEach(o =>
            {
                o.Body = WikifierLite.Process(o.Body);
                o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
            });

            WikiContext.SetPageId(pageInfo.Id);
            WikiContext.Title = $"{pageInfo.Name}";

            if (model.Comments != null && model.Comments.Any())
            {
                WikiContext.PaginationCount = model.Comments.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("{givenCanonical}/{page=1}/Comments")]
        [HttpPost("{givenCanonical}/Comments")]
        public ActionResult Comments(PageCommentsViewModel model, string givenCanonical, int page)
        {
            WikiContext.RequireEditPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            string? errorMessage = null;

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageInfo = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (pageInfo == null)
            {
                return NotFound();
            }

            PageRepository.InsertPageComment(pageInfo.Id, WikiContext.Profile.EnsureNotNull().UserId, model.Comment);

            model = new PageCommentsViewModel()
            {
                Comments = PageRepository.GetPageCommentsPaged(pageNavigation, page),
                ErrorMessage = errorMessage.DefaultWhenNull(string.Empty)
            };

            model.Comments.ForEach(o =>
            {
                o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
            });

            WikiContext.SetPageId(pageInfo.Id);
            WikiContext.Title = $"{pageInfo.Name} Comments";

            if (model.Comments != null && model.Comments.Any())
            {
                WikiContext.PaginationCount = model.Comments.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        #region Content.

        [Authorize]
        [HttpGet("{givenCanonical}/Refresh")]
        public ActionResult Refresh(string givenCanonical)
        {
            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            RefreshPageProperties(pageNavigation);

            return Redirect($"/{pageNavigation}");
        }

        [Authorize]
        [HttpGet("{givenCanonical}/{page:int?}/History")]
        [HttpGet("{givenCanonical}/History")]
        public ActionResult History(string givenCanonical, int page = 0)
        {
            WikiContext.RequireViewPermission();

            if (page <= 0) page = 1;

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var model = new PageHistoryViewModel()
            {
                History = PageRepository.GetPageRevisionHistoryInfoByNavigationPaged(pageNavigation, page)
            };

            model.History.ForEach(o =>
            {
                o.CreatedDate = WikiContext.LocalizeDateTime(o.CreatedDate);
                o.ModifiedDate = WikiContext.LocalizeDateTime(o.ModifiedDate);
            });

            foreach (var p in model.History)
            {
                var thisRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision);
                var prevRev = PageRepository.GetPageRevisionByNavigation(p.Navigation, p.Revision - 1);
                p.ChangeSummary = Differentiator.GetComparisionSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            if (model.History != null && model.History.Any())
            {
                WikiContext.SetPageId(model.History.First().PageId);
                WikiContext.Title = $"{model.History.First().Name} History";
                WikiContext.PaginationCount = model.History.First().PaginationCount;
                WikiContext.CurrentPage = page;

                if (page < WikiContext.PaginationCount) WikiContext.NextPage = page + 1;
                if (page > 1) WikiContext.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("{givenCanonical}/Delete")]
        public ActionResult Delete(string givenCanonical, PageDeleteViewModel model)
        {
            WikiContext.RequireDeletePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.EnsureNotNull().Id);
            if (instructions.Any(o => o.Instruction == WikiInstruction.Protect))
            {
                return Unauthorized();
            }

            bool confirmAction = bool.Parse(GetFormString("Action").EnsureNotNull());
            if (confirmAction == true && page != null)
            {
                PageRepository.DeletePageById(page.Id);
                WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [page.Navigation]));
            }

            return Redirect($"/Home");
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Delete")]
        public ActionResult Delete(string givenCanonical)
        {
            WikiContext.RequireDeletePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation).EnsureNotNull();

            WikiContext.PageName = page.Name;
            WikiContext.MostCurrentRevision = page.Revision;
            WikiContext.CountOfAttachments = PageRepository.GetCountOfPageAttachmentsById(page.Id);

            var model = new PageDeleteViewModel()
            {
            };

            WikiContext.SetPageId(page.Id);
            WikiContext.Title = $"{page.Name} Delete";

            var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);

            if (instructions.Any(o => o.Instruction == WikiInstruction.Protect))
            {
                model.ErrorMessage = "The page is protected and cannot be deleted. A moderator or an administrator must remove the protection before deletion.";
                return View(model);
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("{givenCanonical}/Revert/{pageRevision:int}")]
        public ActionResult Revert(string givenCanonical, int pageRevision, PageRevertViewModel model)
        {
            WikiContext.RequireModeratePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            bool confirmAction = bool.Parse(GetFormString("Action").EnsureNotNullOrEmpty());
            if (confirmAction == true)
            {
                var page = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision).EnsureNotNull();
                SavePage(page);
            }

            return Redirect($"/{pageNavigation}");
        }

        [Authorize]
        [HttpGet("{givenCanonical}/Revert/{pageRevision:int}")]
        public ActionResult Revert(string givenCanonical, int pageRevision)
        {
            WikiContext.RequireModeratePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var mostCurrentPage = PageRepository.GetPageRevisionByNavigation(pageNavigation).EnsureNotNull();
            mostCurrentPage.CreatedDate = WikiContext.LocalizeDateTime(mostCurrentPage.CreatedDate);
            mostCurrentPage.ModifiedDate = WikiContext.LocalizeDateTime(mostCurrentPage.ModifiedDate);

            var revisionPage = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision).EnsureNotNull();
            revisionPage.CreatedDate = WikiContext.LocalizeDateTime(revisionPage.CreatedDate);
            revisionPage.ModifiedDate = WikiContext.LocalizeDateTime(revisionPage.ModifiedDate);

            WikiContext.PageName = revisionPage.Name;
            WikiContext.CountOfRevisions = mostCurrentPage.Revision - revisionPage.Revision;
            WikiContext.MostCurrentRevision = mostCurrentPage.Revision;

            var model = new PageRevertViewModel();

            if (revisionPage != null)
            {
                WikiContext.SetPageId(revisionPage.Id, pageRevision);
                WikiContext.Title = $"{revisionPage.Name} Revert";
            }

            return View(model);
        }

        #endregion

        #region Edit.

        [Authorize]
        [HttpGet("{givenCanonical}/Edit")]
        [HttpGet("Page/Create")]
        public ActionResult Edit(string givenCanonical)
        {
            WikiContext.RequireEditPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                WikiContext.SetPageId(page.Id);

                //Editing an existing page.
                WikiContext.Title = page.Title;

                return View(new PageEditViewModel()
                {
                    Id = page.Id,
                    Body = page.Body,
                    Name = page.Name,
                    Navigation = NamespaceNavigation.CleanAndValidate(page.Navigation),
                    Description = page.Description
                });
            }
            else
            {
                var pageName = GetQueryString("Name").DefaultWhenNullOrEmpty(pageNavigation);

                string templateName = ConfigurationRepository.Get<string>("Customization", "New Page Template").EnsureNotNull();
                string templateNavigation = NamespaceNavigation.CleanAndValidate(templateName);
                var templatePage = PageRepository.GetPageRevisionByNavigation(templateNavigation);

                if (templatePage == null)
                {
                    templatePage = new Page();
                }

                return View(new PageEditViewModel()
                {
                    Body = templatePage.Body,
                    Name = pageName?.Replace('_', ' ') ?? string.Empty,
                    Navigation = NamespaceNavigation.CleanAndValidate(pageNavigation)
                });
            }
        }

        [Authorize]
        [HttpPost("{givenCanonical}/Edit")]
        [HttpPost("Page/Create")]
        public ActionResult Edit(PageEditViewModel model)
        {
            WikiContext.RequireEditPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            if (model.Id == 0) //Saving a new page.
            {
                var page = new Page()
                {
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = WikiContext.Profile.EnsureNotNull().UserId,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedByUserId = WikiContext.Profile.UserId,
                    Body = model.Body ?? "",
                    Name = model.Name,
                    Navigation = NamespaceNavigation.CleanAndValidate(model.Name),
                    Description = model.Description ?? ""
                };

                if (PageRepository.GetPageInfoByNavigation(page.Navigation) != null)
                {
                    model.ErrorMessage = "The page name you entered already exists.";
                    return View(model);
                }

                page.Id = SavePage(page);

                WikiContext.SetPageId(page.Id);

                model.SuccessMessage = "The page was successfully created!";

                return Redirect($"/{page.Navigation}/Edit");
            }
            else
            {
                var page = PageRepository.GetPageRevisionById(model.Id).EnsureNotNull();

                string originalNavigation = string.Empty;

                model.Navigation = NamespaceNavigation.CleanAndValidate(model.Name);

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
                page.ModifiedByUserId = WikiContext.Profile.EnsureNotNull().UserId;
                page.Body = model.Body ?? "";
                page.Name = model.Name;
                page.Navigation = NamespaceNavigation.CleanAndValidate(model.Name);
                page.Description = model.Description ?? "";

                WikiContext.Title = page.Title;

                SavePage(page);

                WikiContext.SetPageId(page.Id);

                model.SuccessMessage = "The page was saved successfully!";

                if (page != null && string.IsNullOrWhiteSpace(originalNavigation) == false)
                {
                    WikiCache.ClearCategory(WikiCacheKey.Build(WikiCache.Category.Page, [originalNavigation]));
                }

                return View(model);
            }
        }

        #endregion
    }
}
