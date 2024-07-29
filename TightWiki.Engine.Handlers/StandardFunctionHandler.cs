using NTDLS.Helpers;
using System.Reflection;
using System.Text;
using TightWiki.Engine.Handlers.Utility;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.EngineFunction;
using TightWiki.Library;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using static TightWiki.Engine.Library.Constants;
using static TightWiki.EngineFunction.FunctionPrototypeCollection;

namespace TightWiki.Engine.Handlers
{
    public class StandardFunctionHandler : IStandardFunctionHandler
    {
        private readonly Dictionary<string, int> _sequences = new();
        private static FunctionPrototypeCollection? _collection;

        public FunctionPrototypeCollection Prototypes
        {
            get
            {
                if (_collection == null)
                {
                    _collection = new FunctionPrototypeCollection(WikiFunctionType.Standard);

                    #region Prototypes.

                    _collection.Add("##Snippet: <string>[name]");
                    _collection.Add("##Seq: <string>{key}='Default'");
                    _collection.Add("##Set: <string>[key] | <string>[value]");
                    _collection.Add("##Get: <string>[key]");
                    _collection.Add("##Color: <string>[color] | <string>[text]");
                    _collection.Add("##Tag: <string:infinite>[pageTags]");
                    _collection.Add("##SearchList: <string>[searchPhrase] | <string>{styleName(List,Full)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <bool>{allowFuzzyMatching}='false' | <bool>{showNamespace}='false'");
                    _collection.Add("##TagList: <string:infinite>[pageTags] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##NamespaceGlossary: <string:infinite>[namespaces] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##NamespaceList: <string:infinite>[namespaces] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##TagGlossary: <string:infinite>[pageTags] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##RecentlyModified: <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##TextGlossary: <string>[searchPhrase] | <integer>{Top}='1000' | <string>{styleName(List,Full)}='Full' | <bool>{showNamespace}='false'");
                    _collection.Add("##Image: <string>[name] | <integer>{scale}='100' | <string>{altText}=''");
                    _collection.Add("##File: <string>[name] | <string>{linkText} | <bool>{showSize}='false'");
                    _collection.Add("##Related: <string>{styleName(List,Flat,Full)}='Full' | <integer>{pageSize}='10' | <bool>{pageSelector}='true'");
                    _collection.Add("##Similar: <integer>{similarity}='80' | <string>{styleName(List,Flat,Full)}='Full' | <integer>{pageSize}='10' | <bool>{pageSelector}='true'");
                    _collection.Add("##EditLink: <string>{linkText}='edit'");
                    _collection.Add("##Inject: <string>[pageName]");
                    _collection.Add("##Include: <string>[pageName]");
                    _collection.Add("##BR: <integer>{Count}='1'");
                    _collection.Add("##HR: <integer>{Height}='1'");
                    _collection.Add("##Revisions:<string>{styleName(Full,List)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <string>{pageName}=''");
                    _collection.Add("##Attachments:<string>{styleName(Full,List)}='Full' | <integer>{pageSize}='5' | <bool>{pageSelector}='true' | <string>{pageName}=''");
                    _collection.Add("##Title:");
                    _collection.Add("##Navigation:");
                    _collection.Add("##Name:");
                    _collection.Add("##Namespace:");
                    _collection.Add("##Created:");
                    _collection.Add("##LastModified:");
                    _collection.Add("##AppVersion:");
                    _collection.Add("##ProfileGlossary: <integer>{Top}='1000' | <integer>{pageSize}='100' | <string>{searchToken}=''");
                    _collection.Add("##ProfileList: <integer>{Top}='1000' | <integer>{pageSize}='100' | <string>{searchToken}=''");

                    #endregion
                }

                return _collection;
            }
        }

        private static Page? GetPageFromPathInfo(string routeData)
        {
            routeData = NamespaceNavigation.CleanAndValidate(routeData);
            var page = PageRepository.GetPageRevisionByNavigation(routeData);
            return page;
        }

        private void MergeUserVariables(ref IWikifierSession wikifierSession, Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                wikifierSession.Variables[item.Key] = item.Value;
            }
        }

        private void MergeSnippets(ref IWikifierSession wikifierSession, Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                wikifierSession.Snippets[item.Key] = item.Value;
            }
        }

        public HandlerResult Handle(IWikifierSession wikifierSession, FunctionCall function, string scopeBody)
        {
            switch (function.Name.ToLower())
            {
                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a glossary all user profiles.
                case "profileglossary":
                    {
                        var html = new StringBuilder();
                        string refTag = wikifierSession.CreateNextQueryToken();
                        int pageNumber = int.Parse(wikifierSession.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var searchToken = function.Parameters.Get<string>("searchToken");
                        var topCount = function.Parameters.Get<int>("top");
                        var profiles = UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

                        string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                        var alphabet = profiles.Select(p => p.AccountName.Substring(0, 1).ToUpper()).Distinct();

                        if (profiles.Count() > 0)
                        {
                            html.Append("<center>");
                            foreach (var alpha in alphabet)
                            {
                                html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                            }
                            html.Append("</center>");

                            html.Append("<ul>");
                            foreach (var alpha in alphabet)
                            {
                                html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                html.Append("<ul>");
                                foreach (var profile in profiles.Where(p => p.AccountName.ToLower().StartsWith(alpha.ToLower())))
                                {
                                    html.Append($"<li><a href=\"/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            html.Append("</ul>");
                        }
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a list of all user profiles.
                case "profilelist":
                    {
                        var html = new StringBuilder();
                        string refTag = wikifierSession.CreateNextQueryToken();
                        int pageNumber = int.Parse(wikifierSession.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var searchToken = function.Parameters.Get<string>("searchToken");
                        var profiles = UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

                        if (profiles.Count() > 0)
                        {
                            html.Append("<ul>");

                            foreach (var profile in profiles)
                            {
                                html.Append($"<li><a href=\"/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                                html.Append("</li>");
                            }

                            html.Append("</ul>");
                        }

                        if (profiles.Count > 0 && profiles.First().PaginationPageCount > 1)
                        {
                            html.Append(PageSelectorGenerator.Generate(refTag, wikifierSession.QueryString, profiles.First().PaginationPageCount));
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "attachments":
                    {
                        string refTag = wikifierSession.CreateNextQueryToken();

                        int pageNumber = int.Parse(wikifierSession.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

                        var navigation = NamespaceNavigation.CleanAndValidate(function.Parameters.Get("pageName", wikifierSession.Page.Navigation));
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        var attachments = PageFileRepository.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(navigation, pageNumber, pageSize, wikifierSession.Revision);
                        var html = new StringBuilder();

                        if (attachments.Count() > 0)
                        {
                            html.Append("<ul>");
                            foreach (var file in attachments)
                            {
                                if (wikifierSession.Revision != null)
                                {
                                    html.Append($"<li><a href=\"/Page/Binary/{wikifierSession.Page.Navigation}/{file.FileNavigation}/{wikifierSession.Revision}\">{file.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"/Page/Binary/{wikifierSession.Page.Navigation}/{file.FileNavigation}\">{file.Name} </a>");
                                }

                                if (styleName == "full")
                                {
                                    html.Append($" - ({file.FriendlySize})");
                                }

                                html.Append("</li>");
                            }
                            html.Append("</ul>");

                            if (pageSelector && attachments.Count > 0 && attachments.First().PaginationPageCount > 1)
                            {
                                html.Append(PageSelectorGenerator.Generate(refTag, wikifierSession.QueryString, attachments.First().PaginationPageCount));
                            }
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "revisions":
                    {
                        if (wikifierSession.SessionState == null)
                        {
                            throw new Exception($"Localization is not supported without SessionState.");
                        }

                        string refTag = wikifierSession.CreateNextQueryToken();

                        int pageNumber = int.Parse(wikifierSession.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

                        var navigation = NamespaceNavigation.CleanAndValidate(function.Parameters.Get("pageName", wikifierSession.Page.Navigation));
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        var revisions = PageRepository.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, null, null, pageSize);
                        var html = new StringBuilder();

                        if (revisions.Count() > 0)
                        {
                            html.Append("<ul>");
                            foreach (var item in revisions)
                            {
                                html.Append($"<li><a href=\"/{item.Navigation}/{item.Revision}\">{item.Revision} by {item.ModifiedByUserName} on {wikifierSession.SessionState.LocalizeDateTime(item.ModifiedDate)}</a>");

                                if (styleName == "full")
                                {
                                    var thisRev = PageRepository.GetPageRevisionByNavigation(wikifierSession.Page.Navigation, item.Revision);
                                    var prevRev = PageRepository.GetPageRevisionByNavigation(wikifierSession.Page.Navigation, item.Revision - 1);

                                    var summaryText = Differentiator.GetComparisonSummary(thisRev?.Body ?? string.Empty, prevRev?.Body ?? string.Empty);

                                    if (summaryText.Length > 0)
                                    {
                                        html.Append(" - " + summaryText);
                                    }
                                }
                                html.Append("</li>");
                            }
                            html.Append("</ul>");

                            if (pageSelector && revisions.Count > 0 && revisions.First().PaginationPageCount > 1)
                            {
                                html.Append(PageSelectorGenerator.Generate(refTag, wikifierSession.QueryString, revisions.First().PaginationPageCount));
                            }
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "seq": //##Seq({Key})   Inserts a sequence into the document.
                    {
                        var key = function.Parameters.Get<string>("Key");

                        if (_sequences.ContainsKey(key) == false)
                        {
                            _sequences.Add(key, 0);
                        }

                        _sequences[key]++;

                        return new HandlerResult(_sequences[key].ToString())
                        {
                            Instructions = [HandlerResultInstruction.OnlyReplaceFirstMatch]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "editlink": //(##EditLink(link text))
                    {
                        var linkText = function.Parameters.Get<string>("linkText");
                        return new HandlerResult("<a href=\"" + NamespaceNavigation.CleanAndValidate($"/{wikifierSession.Page.Navigation}/Edit") + $"\">{linkText}</a>");
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //injects an un-processed wiki body into the calling page.
                case "inject": //(PageName)
                    {
                        var navigation = function.Parameters.Get<string>("pageName");

                        var page = GetPageFromPathInfo(navigation);
                        if (page != null)
                        {
                            return new HandlerResult(page.Body)
                            {
                                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                            };
                        }
                        throw new Exception($"The include page was not found: [{navigation}]");

                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Includes a processed wiki body into the calling page.
                case "include": //(PageName)
                    {
                        var navigation = function.Parameters.Get<string>("pageName");

                        var page = GetPageFromPathInfo(navigation);
                        if (page != null)
                        {
                            var childWikifier = wikifierSession.Wikifier.CreateChild(page);
                            var childWikifierSession = childWikifier.Process(wikifierSession.SessionState, page);

                            MergeUserVariables(ref wikifierSession, childWikifierSession.Variables);
                            MergeSnippets(ref wikifierSession, childWikifierSession.Snippets);

                            return new HandlerResult(childWikifierSession.BodyResult)
                            {
                                Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                            };
                        }
                        throw new Exception($"The include page was not found: [{navigation}]");
                    }
                //------------------------------------------------------------------------------------------------------------------------------

                case "set":
                    {
                        var key = function.Parameters.Get<string>("key");
                        var value = function.Parameters.Get<string>("value");

                        if (wikifierSession.Variables.ContainsKey(key))
                        {
                            wikifierSession.Variables[key] = value;
                        }
                        else
                        {
                            wikifierSession.Variables.Add(key, value);
                        }

                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }
                //------------------------------------------------------------------------------------------------------------------------------

                case "get":
                    {
                        var key = function.Parameters.Get<string>("key");

                        if (wikifierSession.Variables.TryGetValue(key, out var variable))
                        {
                            return new HandlerResult(variable);
                        }

                        throw new Exception($"The wiki variable {key} is not defined. It should be set with ##Set() before calling Get().");
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "color":
                    {
                        var color = function.Parameters.Get<string>("color");
                        var text = function.Parameters.Get<string>("text");

                        return new HandlerResult($"<font color=\"{color}\">{text}</font>");
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Associates tags with a page. These are saved with the page and can also be displayed.
                case "tag": //##tag(pipe|separated|list|of|tags)
                    {
                        var tags = function.Parameters.GetList<string>("pageTags");
                        wikifierSession.Tags.AddRange(tags);
                        wikifierSession.Tags = wikifierSession.Tags.Distinct().ToList();

                        return new HandlerResult(string.Empty)
                        {
                            Instructions = [HandlerResultInstruction.TruncateTrailingLine]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays an image that is attached to the page.
                case "image": //##Image(Name, [optional:default=100]Scale, [optional:default=""]Alt-Text)
                    {
                        string imageName = function.Parameters.Get<string>("name");
                        string alt = function.Parameters.Get("alttext", imageName);
                        int scale = function.Parameters.Get<int>("scale");

                        bool explicitNamespace = imageName.Contains("::");
                        bool isPageForeignImage = false;

                        string navigation = wikifierSession.Page.Navigation;
                        if (imageName.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string image = $"<a href=\"{imageName}\" target=\"_blank\"><img src=\"{imageName}\" border=\"0\" alt=\"{alt}\" /></a>";
                            return new HandlerResult(image);
                        }
                        else if (imageName.Contains('/'))
                        {
                            //Allow loading attached images from other pages.
                            int slashIndex = imageName.IndexOf("/");
                            navigation = NamespaceNavigation.CleanAndValidate(imageName.Substring(0, slashIndex));
                            imageName = imageName.Substring(slashIndex + 1);
                            isPageForeignImage = true;
                        }

                        if (explicitNamespace == false && wikifierSession.Page.Namespace != null)
                        {
                            if (PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(imageName), wikifierSession.Revision) == null)
                            {
                                //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                                navigation = NamespaceNavigation.CleanAndValidate($"{wikifierSession.Page.Namespace}::{imageName}");
                            }
                        }

                        if (wikifierSession.Revision != null && isPageForeignImage == false)
                        {
                            //Check for isPageForeignImage because we don't version foreign page files.
                            string link = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(imageName)}/{wikifierSession.Revision}";
                            string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";
                            return new HandlerResult(image);
                        }
                        else
                        {
                            string link = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(imageName)}";
                            string image = $"<a href=\"{link}\" target=\"_blank\"><img src=\"{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" /></a>";
                            return new HandlerResult(image);
                        }
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays an file download link
                case "file": //##file(Name | Alt-Text | [optional display file size] true/false)
                    {
                        string fileName = function.Parameters.Get<string>("name");

                        bool explicitNamespace = fileName.Contains("::");
                        bool isPageForeignFile = false;

                        string navigation = wikifierSession.Page.Navigation;
                        if (fileName.Contains('/'))
                        {
                            //Allow loading attached images from other pages.
                            int slashIndex = fileName.IndexOf("/");
                            navigation = NamespaceNavigation.CleanAndValidate(fileName.Substring(0, slashIndex));
                            fileName = fileName.Substring(slashIndex + 1);
                            isPageForeignFile = true;
                        }

                        if (explicitNamespace == false && wikifierSession.Page.Namespace != null)
                        {
                            if (PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(fileName), wikifierSession.Revision) == null)
                            {
                                //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                                navigation = NamespaceNavigation.CleanAndValidate($"{wikifierSession.Page.Namespace}::{fileName}");
                            }
                        }

                        var attachment = PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(fileName), wikifierSession.Revision);
                        if (attachment != null)
                        {
                            string alt = function.Parameters.Get("linkText", fileName);

                            if (function.Parameters.Get<bool>("showSize"))
                            {
                                alt += $" ({attachment.FriendlySize})";
                            }

                            if (wikifierSession.Revision != null && isPageForeignFile == false)
                            {
                                //Check for isPageForeignImage because we don't version foreign page files.
                                string link = $"/Page/Binary/{navigation}/{NamespaceNavigation.CleanAndValidate(fileName)}/{wikifierSession.Revision}";
                                string image = $"<a href=\"{link}\">{alt}</a>";
                                return new HandlerResult(image);
                            }
                            else
                            {
                                string link = $"/Page/Binary/{navigation}/{NamespaceNavigation.CleanAndValidate(fileName)}";
                                string image = $"<a href=\"{link}\">{alt}</a>";
                                return new HandlerResult(image);
                            }
                        }
                        throw new Exception($"File not found [{fileName}]");
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a list of pages that have been recently modified.
                case "recentlymodified": //##RecentlyModified(TopCount)
                    {
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var takeCount = function.Parameters.Get<int>("top");
                        var showNamespace = function.Parameters.Get<bool>("showNamespace");

                        var pages = PageRepository.GetTopRecentlyModifiedPagesInfo(takeCount)
                            .OrderByDescending(o => o.ModifiedDate).ThenBy(o => o.Title).ToList();

                        var html = new StringBuilder();

                        if (pages.Count() > 0)
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                if (showNamespace)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                }

                                if (styleName == "full")
                                {
                                    if (page?.Description?.Length > 0)
                                    {
                                        html.Append(" - " + page.Description);
                                    }
                                }
                                html.Append("</li>");
                            }
                            html.Append("</ul>");
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a glossary of pages in the specified namespace.
                case "namespaceglossary":
                    {
                        string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                        var namespaces = function.Parameters.GetList<string>("namespaces");

                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var topCount = function.Parameters.Get<int>("top");
                        var pages = PageRepository.GetPageInfoByNamespaces(namespaces).Take(topCount).OrderBy(o => o.Name).ToList();
                        var html = new StringBuilder();
                        var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpper()).Distinct();
                        var showNamespace = function.Parameters.Get<bool>("showNamespace");

                        if (pages.Count() > 0)
                        {
                            html.Append("<center>");
                            foreach (var alpha in alphabet)
                            {
                                html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                            }
                            html.Append("</center>");

                            html.Append("<ul>");
                            foreach (var alpha in alphabet)
                            {
                                html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                html.Append("<ul>");
                                foreach (var page in pages.Where(p => p.Title.ToLower().StartsWith(alpha.ToLower())))
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        if (page?.Description?.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            html.Append("</ul>");
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a list of pages by searching the page tags.
                case "namespacelist":
                    {
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var topCount = function.Parameters.Get<int>("top");
                        var namespaces = function.Parameters.GetList<string>("namespaces");
                        var showNamespace = function.Parameters.Get<bool>("showNamespace");

                        var pages = PageRepository.GetPageInfoByNamespaces(namespaces).Take(topCount).OrderBy(o => o.Name).ToList();
                        var html = new StringBuilder();

                        if (pages.Count() > 0)
                        {
                            html.Append("<ul>");

                            foreach (var page in pages)
                            {
                                if (showNamespace)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                }

                                if (styleName == "full")
                                {
                                    if (page?.Description?.Length > 0)
                                    {
                                        html.Append(" - " + page.Description);
                                    }
                                }

                                html.Append("</li>");
                            }

                            html.Append("</ul>");
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a glossary of pages with the specified comma separated tags.
                case "tagglossary":
                    {
                        string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                        var tags = function.Parameters.GetList<string>("pageTags");

                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var topCount = function.Parameters.Get<int>("top");
                        var pages = PageRepository.GetPageInfoByTags(tags).Take(topCount).OrderBy(o => o.Name).ToList();
                        var html = new StringBuilder();
                        var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpper()).Distinct();
                        var showNamespace = function.Parameters.Get<bool>("showNamespace");

                        if (pages.Count() > 0)
                        {
                            html.Append("<center>");
                            foreach (var alpha in alphabet)
                            {
                                html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                            }
                            html.Append("</center>");

                            html.Append("<ul>");
                            foreach (var alpha in alphabet)
                            {
                                html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                html.Append("<ul>");
                                foreach (var page in pages.Where(p => p.Title.ToLower().StartsWith(alpha.ToLower())))
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        if (page?.Description?.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            html.Append("</ul>");
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a glossary by searching page's body text for the specified comma separated list of words.
                case "textglossary":
                    {
                        string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                        var searchTokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                        var topCount = function.Parameters.Get<int>("top");
                        var showNamespace = function.Parameters.Get<bool>("showNamespace");

                        var pages = PageRepository.PageSearch(searchTokens).Take(topCount).OrderBy(o => o.Name).ToList();
                        var html = new StringBuilder();
                        var alphabet = pages.Select(p => p.Title.Substring(0, 1).ToUpper()).Distinct();
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();

                        if (pages.Count() > 0)
                        {
                            html.Append("<center>");
                            foreach (var alpha in alphabet)
                            {
                                html.Append("<a href=\"#" + glossaryName + "_" + alpha + "\">" + alpha + "</a>&nbsp;");
                            }
                            html.Append("</center>");

                            html.Append("<ul>");
                            foreach (var alpha in alphabet)
                            {
                                html.Append("<li><a name=\"" + glossaryName + "_" + alpha + "\">" + alpha + "</a></li>");

                                html.Append("<ul>");
                                foreach (var page in pages.Where(p => p.Title.ToLower().StartsWith(alpha.ToLower())))
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                    }

                                    if (styleName == "full")
                                    {
                                        if (page?.Description?.Length > 0)
                                        {
                                            html.Append(" - " + page.Description);
                                        }
                                    }
                                    html.Append("</li>");
                                }
                                html.Append("</ul>");
                            }

                            html.Append("</ul>");
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a list of pages by searching the page body for the specified text.
                case "searchlist":
                    {
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        string refTag = wikifierSession.CreateNextQueryToken();
                        int pageNumber = int.Parse(wikifierSession.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        var allowFuzzyMatching = function.Parameters.Get<bool>("allowFuzzyMatching");
                        var searchTokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                        var showNamespace = function.Parameters.Get<bool>("showNamespace");

                        var pages = PageRepository.PageSearchPaged(searchTokens, pageNumber, pageSize, allowFuzzyMatching);
                        var html = new StringBuilder();

                        if (pages.Count() > 0)
                        {
                            html.Append("<ul>");

                            foreach (var page in pages)
                            {
                                if (showNamespace)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                }

                                if (styleName == "full")
                                {
                                    if (page?.Description?.Length > 0)
                                    {
                                        html.Append(" - " + page.Description);
                                    }
                                }

                                html.Append("</li>");
                            }

                            html.Append("</ul>");
                        }

                        if (pageSelector && (pageNumber > 1 || (pages.Count > 0 && pages.First().PaginationPageCount > 1)))
                        {
                            html.Append(PageSelectorGenerator.Generate(refTag, wikifierSession.QueryString, pages.FirstOrDefault()?.PaginationPageCount ?? 1));
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a list of pages by searching the page tags.
                case "taglist":
                    {
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var topCount = function.Parameters.Get<int>("top");
                        var tags = function.Parameters.GetList<string>("pageTags");
                        var showNamespace = function.Parameters.Get<bool>("showNamespace");

                        var pages = PageRepository.GetPageInfoByTags(tags).Take(topCount).OrderBy(o => o.Name).ToList();
                        var html = new StringBuilder();

                        if (pages.Count() > 0)
                        {
                            html.Append("<ul>");

                            foreach (var page in pages)
                            {
                                if (showNamespace)
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                                }

                                if (styleName == "full")
                                {
                                    if (page?.Description?.Length > 0)
                                    {
                                        html.Append(" - " + page.Description);
                                    }
                                }

                                html.Append("</li>");
                            }

                            html.Append("</ul>");
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays a list of other related pages based on tags.
                case "similar": //##Similar()
                    {
                        string refTag = wikifierSession.CreateNextQueryToken();

                        var similarity = function.Parameters.Get<int>("similarity");
                        int pageNumber = int.Parse(wikifierSession.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var html = new StringBuilder();

                        var pages = PageRepository.GetSimilarPagesPaged(wikifierSession.Page.Id, similarity, pageNumber, pageSize);

                        if (styleName == "list")
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                            }
                            html.Append("</ul>");
                        }
                        else if (styleName == "flat")
                        {
                            foreach (var page in pages)
                            {
                                if (html.Length > 0) html.Append(" | ");
                                html.Append($"<a href=\"/{page.Navigation}\">{page.Title}</a>");
                            }
                        }
                        else if (styleName == "full")
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a> - {page.Description}");
                            }
                            html.Append("</ul>");
                        }

                        if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
                        {
                            html.Append(PageSelectorGenerator.Generate(refTag, wikifierSession.QueryString, pages.First().PaginationPageCount));
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays a list of other related pages based incoming links.
                case "related": //##related
                    {
                        string refTag = wikifierSession.CreateNextQueryToken();

                        int pageNumber = int.Parse(wikifierSession.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var html = new StringBuilder();

                        var pages = PageRepository.GetRelatedPagesPaged(wikifierSession.Page.Id, pageNumber, pageSize);

                        if (styleName == "list")
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a>");
                            }
                            html.Append("</ul>");
                        }
                        else if (styleName == "flat")
                        {
                            foreach (var page in pages)
                            {
                                if (html.Length > 0) html.Append(" | ");
                                html.Append($"<a href=\"/{page.Navigation}\">{page.Title}</a>");
                            }
                        }
                        else if (styleName == "full")
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                html.Append($"<li><a href=\"/{page.Navigation}\">{page.Title}</a> - {page.Description}");
                            }
                            html.Append("</ul>");
                        }

                        if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
                        {
                            html.Append(PageSelectorGenerator.Generate(refTag, wikifierSession.QueryString, pages.First().PaginationPageCount));
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the date and time that the current page was last modified.
                case "lastmodified":
                    {
                        if (wikifierSession.SessionState == null)
                        {
                            throw new Exception($"Localization is not supported without SessionState.");
                        }

                        if (wikifierSession.Page.ModifiedDate != DateTime.MinValue)
                        {
                            var localized = wikifierSession.SessionState.LocalizeDateTime(wikifierSession.Page.ModifiedDate);
                            return new HandlerResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}");
                        }

                        return new HandlerResult(string.Empty);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the date and time that the current page was created.
                case "created":
                    {
                        if (wikifierSession.SessionState == null)
                        {
                            throw new Exception($"Localization is not supported without SessionState.");
                        }

                        if (wikifierSession.Page.CreatedDate != DateTime.MinValue)
                        {
                            var localized = wikifierSession.SessionState.LocalizeDateTime(wikifierSession.Page.CreatedDate);
                            return new HandlerResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}");
                        }

                        return new HandlerResult(string.Empty);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the version of the wiki.
                case "appversion":
                    {
                        var version = string.Join('.', (Assembly.GetExecutingAssembly()
                            .GetName().Version?.ToString() ?? "0.0.0.0").Split('.').Take(3)); //Major.Minor.Patch

                        return new HandlerResult(version);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the title of the current page.
                case "name":
                    {
                        return new HandlerResult(wikifierSession.Page.Title);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the title of the current page in title form.
                case "title":
                    {
                        return new HandlerResult($"<h1>{wikifierSession.Page.Title}</h1>");
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the namespace of the current page.
                case "namespace":
                    {
                        return new HandlerResult(wikifierSession.Page.Namespace ?? string.Empty);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the namespace of the current page.
                case "snippet":
                    {
                        string name = function.Parameters.Get<string>("name");

                        if (wikifierSession.Snippets.ContainsKey(name))
                        {
                            return new HandlerResult(wikifierSession.Snippets[name]);
                        }
                        else
                        {
                            return new HandlerResult(string.Empty);
                        }
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Inserts empty lines into the page.
                case "br":
                case "nl":
                case "newline": //##NewLine([optional:default=1]count)
                    {
                        var lineBreaks = new StringBuilder();
                        int count = function.Parameters.Get<int>("Count");
                        for (int i = 0; i < count; i++)
                        {
                            lineBreaks.Append("<br />");
                        }
                        return new HandlerResult(lineBreaks.ToString());
                    }

                //Inserts a horizontal rule
                case "hr":
                    {
                        int size = function.Parameters.Get<int>("height");
                        return new HandlerResult($"<hr class=\"mt-{size} mb-{size}\">");
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the navigation text for the current page.
                case "navigation":
                    {
                        return new HandlerResult(wikifierSession.Page.Navigation);
                    }
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
