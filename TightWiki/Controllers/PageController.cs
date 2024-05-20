using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using TightWiki.Library;
using TightWiki.Library.DataModels;
using TightWiki.Library.Library;
using TightWiki.Library.Repository;
using TightWiki.Library.ViewModels.Page;
using TightWiki.Library.Wiki;
using static TightWiki.Library.Library.Constants;

namespace TightWiki.Controllers
{
    [Route("")]
    public class PageController : ControllerHelperBase
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
            context.RequireViewPermission();

            var model = new PageDisplayViewModel();
            var navigation = new NamespaceNavigation(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(navigation.Canonical, pageRevision);
            if (page != null)
            {
                var instructions = PageRepository.GetPageProcessingInstructionsByPageId(page.Id);
                context.HideFooterComments = instructions.Where(o => o.Instruction == WikiInstruction.HideFooterComments).Any();

                if (page.Revision == page.LatestRevision)
                {
                    pageRevision = null;
                }

                context.SetPageId(page.Id, pageRevision);
                context.Title = page.Title;

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
                        var wiki = new Wikifier(context, page, pageRevision, Request.Query);

                        model.Body = wiki.ProcessedBody;

                        if (wiki.ProcessingInstructions.Contains(WikiInstruction.NoCache) == false)
                        {
                            WikiCache.Put(cacheKey, wiki.ProcessedBody, GlobalSettings.PageCacheSeconds); //This is cleared with the call to Cache.ClearCategory($"Page:{page.Navigation}");
                        }
                    }
                }
                else
                {
                    var wiki = new Wikifier(context, page, pageRevision, Request.Query);
                    model.Body = wiki.ProcessedBody;
                }

                if (GlobalSettings.EnablePageComments && GlobalSettings.ShowCommentsOnPageFooter && context.HideFooterComments == false)
                {
                    model.Comments = PageRepository.GetPageCommentsPaged(navigation.Canonical, 1);
                }
            }
            else if (pageRevision != null)
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Revision Does Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation).EnsureNotNull();

                context.SetPageId(null, pageRevision);

                var wiki = new Wikifier(context, notExistsPage, null, Request.Query);
                context.Title = notExistsPage.Name;
                model.Body = wiki.ProcessedBody;

                if (context.IsAuthenticated && context.CanCreate)
                {
                    context.CreatePage = false;
                }
            }
            else
            {
                var notExistPageName = ConfigurationRepository.Get<string>("Customization", "Page Not Exists Page");
                string notExistPageNavigation = NamespaceNavigation.CleanAndValidate(notExistPageName);
                var notExistsPage = PageRepository.GetPageRevisionByNavigation(notExistPageNavigation).EnsureNotNull();

                context.SetPageId(null, null);

                var wiki = new Wikifier(context, notExistsPage, null, Request.Query);
                context.Title = notExistsPage.Name;
                model.Body = wiki.ProcessedBody;

                if (context.IsAuthenticated && context.CanCreate)
                {
                    context.CreatePage = true;
                }
            }

            if (page != null)
            {
                model.ModifiedByUserName = page.ModifiedByUserName;
                model.ModifiedDate = context.LocalizeDateTime(page.ModifiedDate);

                if (model.Comments != null)
                {
                    model.Comments.ForEach(o =>
                    {
                        o.Body = WikifierLite.Process(o.Body);
                        o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
                    });
                }
            }

            return View(model);
        }

        [AllowAnonymous]
        [HttpGet("/Page/Search/{page=1}")]
        public ActionResult Search(int page)
        {
            context.Title = $"Page Search";

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
                    context.PaginationCount = model.Pages.First().PaginationCount;
                    context.CurrentPage = page;

                    if (page < context.PaginationCount) context.NextPage = page + 1;
                    if (page > 1) context.PreviousPage = page - 1;
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
            context.Title = $"Page Search";

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
                    context.PaginationCount = model.Pages.First().PaginationCount;
                    context.CurrentPage = page;

                    if (page < context.PaginationCount) context.NextPage = page + 1;
                    if (page > 1) context.PreviousPage = page - 1;
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
            context.RequireViewPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var pageInfo = PageRepository.GetPageInfoByNavigation(pageNavigation);
            if (pageInfo == null)
            {
                return NotFound();
            }

            if (page <= 0) page = 1;

            var deleteAction = GetQueryString("Delete");
            if (string.IsNullOrEmpty(deleteAction) == false && context.IsAuthenticated)
            {
                if (context.CanModerate)
                {
                    //Moderators and administrators can delete comments that they do not own.
                    PageRepository.DeletePageCommentById(pageInfo.Id, int.Parse(deleteAction));
                }
                else
                {
                    PageRepository.DeletePageCommentByUserAndId(pageInfo.Id, context.User.EnsureNotNull().UserId, int.Parse(deleteAction));
                }
            }

            var model = new PageCommentsViewModel()
            {
                Comments = PageRepository.GetPageCommentsPaged(pageNavigation, page)
            };

            model.Comments.ForEach(o =>
            {
                o.Body = WikifierLite.Process(o.Body);
                o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
            });

            context.SetPageId(pageInfo.Id);
            context.Title = $"{pageInfo.Name}";

            if (model.Comments != null && model.Comments.Any())
            {
                context.PaginationCount = model.Comments.First().PaginationCount;
                context.CurrentPage = page;

                if (page < context.PaginationCount) context.NextPage = page + 1;
                if (page > 1) context.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("{givenCanonical}/{page=1}/Comments")]
        [HttpPost("{givenCanonical}/Comments")]
        public ActionResult Comments(PageCommentsViewModel model, string givenCanonical, int page)
        {
            context.RequireEditPermission();

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

            PageRepository.InsertPageComment(pageInfo.Id, context.User.EnsureNotNull().UserId, model.Comment);

            model = new PageCommentsViewModel()
            {
                Comments = PageRepository.GetPageCommentsPaged(pageNavigation, page),
                ErrorMessage = errorMessage.DefaultWhenNull(string.Empty)
            };

            model.Comments.ForEach(o =>
            {
                o.CreatedDate = context.LocalizeDateTime(o.CreatedDate);
            });

            context.SetPageId(pageInfo.Id);
            context.Title = $"{pageInfo.Name} Comments";

            if (model.Comments != null && model.Comments.Any())
            {
                context.PaginationCount = model.Comments.First().PaginationCount;
                context.CurrentPage = page;

                if (page < context.PaginationCount) context.NextPage = page + 1;
                if (page > 1) context.PreviousPage = page - 1;
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
            context.RequireViewPermission();

            if (page <= 0) page = 1;

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var model = new PageHistoryViewModel()
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
                p.ChangeSummary = Differentiator.GetComparisionSummary(thisRev?.Body ?? "", prevRev?.Body ?? "");
            }

            if (model.History != null && model.History.Any())
            {
                context.SetPageId(model.History.First().PageId);
                context.Title = $"{model.History.First().Name} History";
                context.PaginationCount = model.History.First().PaginationCount;
                context.CurrentPage = page;

                if (page < context.PaginationCount) context.NextPage = page + 1;
                if (page > 1) context.PreviousPage = page - 1;
            }

            return View(model);
        }

        [Authorize]
        [HttpPost("{givenCanonical}/Delete")]
        public ActionResult Delete(string givenCanonical, PageDeleteViewModel model)
        {
            context.RequireDeletePermission();

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
            context.RequireDeletePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation).EnsureNotNull();

            context.PageName = page.Name;
            context.MostCurrentRevision = page.Revision;
            context.CountOfAttachments = PageRepository.GetCountOfPageAttachmentsById(page.Id);

            var model = new PageDeleteViewModel()
            {
            };

            context.SetPageId(page.Id);
            context.Title = $"{page.Name} Delete";

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
            context.RequireModeratePermission();

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
            context.RequireModeratePermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var mostCurrentPage = PageRepository.GetPageRevisionByNavigation(pageNavigation).EnsureNotNull();
            mostCurrentPage.CreatedDate = context.LocalizeDateTime(mostCurrentPage.CreatedDate);
            mostCurrentPage.ModifiedDate = context.LocalizeDateTime(mostCurrentPage.ModifiedDate);

            var revisionPage = PageRepository.GetPageRevisionByNavigation(pageNavigation, pageRevision).EnsureNotNull();
            revisionPage.CreatedDate = context.LocalizeDateTime(revisionPage.CreatedDate);
            revisionPage.ModifiedDate = context.LocalizeDateTime(revisionPage.ModifiedDate);

            context.PageName = revisionPage.Name;
            context.CountOfRevisions = mostCurrentPage.Revision - revisionPage.Revision;
            context.MostCurrentRevision = mostCurrentPage.Revision;

            var model = new PageRevertViewModel();

            if (revisionPage != null)
            {
                context.SetPageId(revisionPage.Id, pageRevision);
                context.Title = $"{revisionPage.Name} Revert";
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
            context.RequireEditPermission();

            var pageNavigation = NamespaceNavigation.CleanAndValidate(givenCanonical);

            var page = PageRepository.GetPageRevisionByNavigation(pageNavigation);
            if (page != null)
            {
                context.SetPageId(page.Id);

                //Editing an existing page.
                context.Title = page.Title;

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
            context.RequireEditPermission();

            if (!model.ValidateModelAndSetErrors(ModelState))
            {
                return View(model);
            }

            if (model.Id == 0) //Saving a new page.
            {
                var page = new Page()
                {
                    CreatedDate = DateTime.UtcNow,
                    CreatedByUserId = context.User.EnsureNotNull().UserId,
                    ModifiedDate = DateTime.UtcNow,
                    ModifiedByUserId = context.User.UserId,
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

                context.SetPageId(page.Id);

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
                page.ModifiedByUserId = context.User.EnsureNotNull().UserId;
                page.Body = model.Body ?? "";
                page.Name = model.Name;
                page.Navigation = NamespaceNavigation.CleanAndValidate(model.Name);
                page.Description = model.Description ?? "";

                context.Title = page.Title;

                SavePage(page);

                context.SetPageId(page.Id);

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
