using NTDLS.Helpers;
using System.Reflection;
using System.Text;
using TightWiki.Engine.Function;
using TightWiki.Engine.Implementation.Utility;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.Library;
using TightWiki.Models;
using TightWiki.Models.DataModels;
using TightWiki.Repository;
using static TightWiki.Engine.Function.FunctionPrototypeCollection;
using static TightWiki.Engine.Library.Constants;

namespace TightWiki.Engine.Implementation.Handlers
{
    /// <summary>
    /// Handled standard function calls, where the function call is replaced with the function result.
    /// </summary>
    public class StandardFunctionHandler : IStandardFunctionHandler
    {
        private static FunctionPrototypeCollection? _collection;

        public FunctionPrototypeCollection Prototypes
        {
            get
            {
                if (_collection == null)
                {
                    //---------------------------------------------------------------------------------------------------------
                    // Example function prototypes:                                                                           -
                    //---------------------------------------------------------------------------------------------------------
                    // Function with an optional parameter whose value is constrained to a given set of values:               -
                    //     Example: functionName (parameterType parameterName[allowable,values]='default value')              -
                    //--                                                                                                      -
                    // Function with an optional parameter, which is just a parameter with a default value.                   -
                    //     Example: functionName (parameterType parameterName='default value')                                -
                    //--                                                                                                      -
                    // Function with more than one required parameter:                                                        -
                    //     Example: functionName (parameterType parameterName1, parameterType parameterName2)                 -
                    //--                                                                                                      -
                    // Function with a required parameter and an optional parameter.                                          -
                    // Note that required parameter cannot come after optional parameters.                                    -
                    //     Example: functionName (parameterType parameterName1, parameterType parameterName2='default value') -
                    //--                                                                                                      -
                    // Notes:                                                                                                 -
                    //     Parameter types are defined by the enum: WikiFunctionParamType                                     -
                    //     All values, with the exception of NULL should be enclosed in single-quotes                         -
                    //     The single-quote enclosed escape character is back-slash (e.g. 'John\'s Literal')                  -
                    //--                                                                                                      -
                    //---------------------------------------------------------------------------------------------------------

                    _collection = new FunctionPrototypeCollection(WikiFunctionType.Standard);
                    _collection.Add("Snippet (String name)");
                    _collection.Add("Seq (String key='Default')");
                    _collection.Add("Set (String key, String value)");
                    _collection.Add("Get (String key)");
                    _collection.Add("Color (String color, String text)");
                    _collection.Add("Tag (InfiniteString pageTags)"); //This is left here for backwards compatibility, Tag does not change the output, so it should be a processing instruction.
                    _collection.Add("SearchList (String searchPhrase, String styleName['List','Full']='Full', Integer pageSize='5', Boolean pageSelector='true', Boolean allowFuzzyMatching='false', Boolean showNamespace='false')");
                    _collection.Add("TagList (InfiniteString pageTags, Integer Top='1000', String styleName['List','Full']='Full', Boolean showNamespace='false')");
                    _collection.Add("NamespaceGlossary (InfiniteString namespaces, Integer Top='1000', String styleName['List','Full']='Full', Boolean showNamespace='false')");
                    _collection.Add("NamespaceList (InfiniteString namespaces, Integer Top='1000', String styleName['List','Full']='Full', Boolean showNamespace='false')");
                    _collection.Add("TagGlossary (InfiniteString pageTags, Integer Top='1000', String styleName['List','Full']='Full', Boolean showNamespace='false')");
                    _collection.Add("RecentlyModified (Integer Top='1000', String styleName['List','Full']='Full', Boolean showNamespace='false')");
                    _collection.Add("TextGlossary (String searchPhrase, Integer Top='1000', String styleName['List','Full']='Full', Boolean showNamespace='false')");
                    _collection.Add("Image (String name, Integer scale='100', String altText=null, String class=null)");
                    _collection.Add("File (String name, String linkText, Boolean showSize='false')");
                    _collection.Add("Related (String styleName['List','Flat','Full']='Full', Integer pageSize='10', Boolean pageSelector='true')");
                    _collection.Add("Similar (Integer similarity='80', String styleName['List','Flat','Full']='Full', Integer pageSize='10', Boolean pageSelector='true')");
                    _collection.Add("EditLink (String linkText='edit')");
                    _collection.Add("Inject (String pageName)");
                    _collection.Add("Include (String pageName)");
                    _collection.Add("BR (Integer Count='1')");
                    _collection.Add("HR (Integer Height='1')");
                    _collection.Add("Revisions (String styleName['Full','List']='Full', Integer pageSize='5', Boolean pageSelector='true', String pageName=null)");
                    _collection.Add("Attachments (String styleName['Full','List']='Full', Integer pageSize='5', Boolean pageSelector='true', String pageName=null)");
                    _collection.Add("Title ()");
                    _collection.Add("Navigation ()");
                    _collection.Add("Name ()");
                    _collection.Add("SiteName ()");
                    _collection.Add("Namespace ()");
                    _collection.Add("Created ()");
                    _collection.Add("LastModified ()");
                    _collection.Add("AppVersion ()");
                    _collection.Add("ProfileGlossary (Integer Top='1000', Integer pageSize='100', String searchToken=null)");
                    _collection.Add("ProfileList (Integer Top='1000', Integer pageSize='100', String searchToken=null)");

                    //System functions (we don't advertise these, but they aren't unsafe):
                    _collection.Add("SystemEmojiCategoryList ()");
                    _collection.Add("SystemEmojiList ()");
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

        private static void MergeUserVariables(ref ITightEngineState state, Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                state.Variables[item.Key] = item.Value;
            }
        }

        private static void MergeSnippets(ref ITightEngineState state, Dictionary<string, string> items)
        {
            foreach (var item in items)
            {
                state.Snippets[item.Key] = item.Value;
            }
        }

        /// <summary>
        /// Called to handle function calls when proper prototypes are matched.
        /// </summary>
        /// <param name="state">Reference to the wiki state object</param>
        /// <param name="function">The parsed function call and all its parameters and their values.</param>
        /// <param name="scopeBody">This is not a scope function, this should always be null</param>
        public HandlerResult Handle(ITightEngineState state, FunctionCall function, string? scopeBody = null)
        {
            switch (function.Name.ToLower())
            {
                //------------------------------------------------------------------------------------------------------------------------------
                //Creates a glossary all user profiles.
                case "profileglossary":
                    {
                        if (!GlobalConfiguration.EnablePublicProfiles)
                        {
                            return new HandlerResult("Public profiles are disabled.");
                        }

                        var html = new StringBuilder();
                        string refTag = state.GetNextQueryToken();
                        int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var searchToken = function.Parameters.GetNullable<string>("searchToken");
                        var topCount = function.Parameters.Get<int>("top");
                        var profiles = UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

                        string glossaryName = "glossary_" + new Random().Next(0, 1000000).ToString();
                        var alphabet = profiles.Select(p => p.AccountName.Substring(0, 1).ToUpper()).Distinct();

                        if (profiles.Count > 0)
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
                                foreach (var profile in profiles.Where(p => p.AccountName.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
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
                        if (!GlobalConfiguration.EnablePublicProfiles)
                        {
                            return new HandlerResult("Public profiles are disabled.");
                        }

                        var html = new StringBuilder();
                        string refTag = state.GetNextQueryToken();
                        int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var searchToken = function.Parameters.GetNullable<string>("searchToken");
                        var profiles = UsersRepository.GetAllPublicProfilesPaged(pageNumber, pageSize, searchToken);

                        if (profiles.Count() > 0)
                        {
                            html.Append("<ul>");

                            foreach (var profile in profiles)
                            {
                                html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/Profile/{profile.Navigation}/Public\">{profile.AccountName}</a>");
                                html.Append("</li>");
                            }

                            html.Append("</ul>");
                        }

                        if (profiles.Count > 0 && profiles.First().PaginationPageCount > 1)
                        {
                            html.Append(PageSelectorGenerator.Generate(refTag, state.QueryString, profiles.First().PaginationPageCount));
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "attachments":
                    {
                        string refTag = state.GetNextQueryToken();

                        int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

                        var navigation = NamespaceNavigation.CleanAndValidate(function.Parameters.Get("pageName", state.Page.Navigation));
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        var attachments = PageFileRepository.GetPageFilesInfoByPageNavigationAndPageRevisionPaged(navigation, pageNumber, pageSize, state.Revision);
                        var html = new StringBuilder();

                        if (attachments.Count > 0)
                        {
                            html.Append("<ul>");
                            foreach (var file in attachments)
                            {
                                if (state.Revision != null)
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/Page/Binary/{state.Page.Navigation}/{file.FileNavigation}/{state.Revision}\">{file.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/Page/Binary/{state.Page.Navigation}/{file.FileNavigation}\">{file.Name} </a>");
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
                                html.Append(PageSelectorGenerator.Generate(refTag, state.QueryString, attachments.First().PaginationPageCount));
                            }
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "revisions":
                    {
                        if (state.Session == null)
                        {
                            throw new Exception($"Localization is not supported without SessionState.");
                        }

                        string refTag = state.GetNextQueryToken();

                        int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));

                        var navigation = NamespaceNavigation.CleanAndValidate(function.Parameters.Get("pageName", state.Page.Navigation));
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        var revisions = PageRepository.GetPageRevisionsInfoByNavigationPaged(navigation, pageNumber, null, null, pageSize);
                        var html = new StringBuilder();

                        if (revisions.Count > 0)
                        {
                            html.Append("<ul>");
                            foreach (var item in revisions)
                            {
                                html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{item.Navigation}/{item.Revision}\">{item.Revision} by {item.ModifiedByUserName} on {state.Session.LocalizeDateTime(item.ModifiedDate)}</a>");

                                if (styleName == "full")
                                {
                                    var thisRev = PageRepository.GetPageRevisionByNavigation(state.Page.Navigation, item.Revision);
                                    var prevRev = PageRepository.GetPageRevisionByNavigation(state.Page.Navigation, item.Revision - 1);

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
                                html.Append(PageSelectorGenerator.Generate(refTag, state.QueryString, revisions.First().PaginationPageCount));
                            }
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "seq": //##Seq({Key})   Inserts a sequence into the document.
                    {
                        var key = function.Parameters.Get<string>("Key");

                        var sequences = state.GetStateValue("_sequences", new Dictionary<string, int>());

                        if (sequences.ContainsKey(key) == false)
                        {
                            sequences.Add(key, 0);
                        }

                        sequences[key]++;

                        return new HandlerResult(sequences[key].ToString())
                        {
                            Instructions = [HandlerResultInstruction.OnlyReplaceFirstMatch]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "editlink": //(##EditLink(link text))
                    {
                        var linkText = function.Parameters.Get<string>("linkText");
                        return new HandlerResult($"<a href=\"{GlobalConfiguration.BasePath}" + NamespaceNavigation.CleanAndValidate($"/{state.Page.Navigation}/Edit") + $"\">{linkText}</a>");
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
                            var childState = state.TransformChild(page);

                            MergeUserVariables(ref state, childState.Variables);
                            MergeSnippets(ref state, childState.Snippets);

                            return new HandlerResult(childState.HtmlResult)
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

                        if (!state.Variables.TryAdd(key, value))
                        {
                            state.Variables[key] = value;
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

                        if (state.Variables.TryGetValue(key, out var variable))
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
                        state.Tags.AddRange(tags);
                        state.Tags = state.Tags.Distinct().ToList();

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
                        var imgClass = function.Parameters.GetNullable<string>("class");
                        int scale = function.Parameters.Get<int>("scale");

                        bool explicitNamespace = imageName.Contains("::");
                        bool isPageForeignImage = false;

                        if (imgClass != null)
                        {
                            imgClass = $"class=\"{imgClass}\"";
                        }

                        string navigation = state.Page.Navigation;
                        if (imageName.StartsWith("http", StringComparison.InvariantCultureIgnoreCase))
                        {
                            string image = $"<a href=\"{imageName}\" target=\"_blank\"><img src=\"{imageName}\" border=\"0\" alt=\"{alt}\" {imgClass} /></a>";
                            return new HandlerResult(image);
                        }
                        else if (imageName.Contains('/'))
                        {
                            //Allow loading attached images from other pages.
                            int slashIndex = imageName.IndexOf('/');
                            navigation = NamespaceNavigation.CleanAndValidate(imageName.Substring(0, slashIndex));
                            imageName = imageName.Substring(slashIndex + 1);
                            isPageForeignImage = true;
                        }

                        if (explicitNamespace == false && state.Page.Namespace != null)
                        {
                            if (PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(imageName), state.Revision) == null)
                            {
                                //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                                navigation = NamespaceNavigation.CleanAndValidate($"{state.Page.Namespace}::{imageName}");
                            }
                        }

                        if (state.Revision != null && isPageForeignImage == false)
                        {
                            //Check for isPageForeignImage because we don't version foreign page files.
                            string link = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(imageName)}/{state.Revision}";
                            string image = $"<a href=\"{GlobalConfiguration.BasePath}{link}\" target=\"_blank\"><img src=\"{GlobalConfiguration.BasePath}{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" {imgClass} /></a>";
                            return new HandlerResult(image);
                        }
                        else
                        {
                            string link = $"/Page/Image/{navigation}/{NamespaceNavigation.CleanAndValidate(imageName)}";
                            string image = $"<a href=\"{GlobalConfiguration.BasePath}{link}\" target=\"_blank\"><img src=\"{GlobalConfiguration.BasePath}{link}?Scale={scale}\" border=\"0\" alt=\"{alt}\" {imgClass} /></a>";
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

                        string navigation = state.Page.Navigation;
                        if (fileName.Contains('/'))
                        {
                            //Allow loading attached images from other pages.
                            int slashIndex = fileName.IndexOf("/");
                            navigation = NamespaceNavigation.CleanAndValidate(fileName.Substring(0, slashIndex));
                            fileName = fileName.Substring(slashIndex + 1);
                            isPageForeignFile = true;
                        }

                        if (explicitNamespace == false && state.Page.Namespace != null)
                        {
                            if (PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(fileName), state.Revision) == null)
                            {
                                //If the image does not exist, and no namespace was specified, but the page has a namespace - then default to the pages namespace.
                                navigation = NamespaceNavigation.CleanAndValidate($"{state.Page.Namespace}::{fileName}");
                            }
                        }

                        var attachment = PageFileRepository.GetPageFileAttachmentInfoByPageNavigationPageRevisionAndFileNavigation(navigation, NamespaceNavigation.CleanAndValidate(fileName), state.Revision);
                        if (attachment != null)
                        {
                            string alt = function.Parameters.Get("linkText", fileName);

                            if (function.Parameters.Get<bool>("showSize"))
                            {
                                alt += $" ({attachment.FriendlySize})";
                            }

                            if (state.Revision != null && isPageForeignFile == false)
                            {
                                //Check for isPageForeignImage because we don't version foreign page files.
                                string link = $"/Page/Binary/{navigation}/{NamespaceNavigation.CleanAndValidate(fileName)}/{state.Revision}";
                                string image = $"<a href=\"{GlobalConfiguration.BasePath}{link}\">{alt}</a>";
                                return new HandlerResult(image);
                            }
                            else
                            {
                                string link = $"/Page/Binary/{navigation}/{NamespaceNavigation.CleanAndValidate(fileName)}";
                                string image = $"<a href=\"{GlobalConfiguration.BasePath}{link}\">{alt}</a>";
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

                        if (pages.Count > 0)
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                if (showNamespace)
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
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

                        if (pages.Count > 0)
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
                                foreach (var page in pages.Where(p => p.Title.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
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

                        if (pages.Count > 0)
                        {
                            html.Append("<ul>");

                            foreach (var page in pages)
                            {
                                if (showNamespace)
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
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

                        if (pages.Count > 0)
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
                                foreach (var page in pages.Where(p => p.Title.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
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

                        if (pages.Count > 0)
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
                                foreach (var page in pages.Where(p => p.Title.StartsWith(alpha, StringComparison.InvariantCultureIgnoreCase)))
                                {
                                    if (showNamespace)
                                    {
                                        html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                                    }
                                    else
                                    {
                                        html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
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
                        string refTag = state.GetNextQueryToken();
                        int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        var allowFuzzyMatching = function.Parameters.Get<bool>("allowFuzzyMatching");
                        var searchTokens = function.Parameters.Get<string>("searchPhrase").Split(" ", StringSplitOptions.RemoveEmptyEntries).ToList();
                        var showNamespace = function.Parameters.Get<bool>("showNamespace");

                        var pages = PageRepository.PageSearchPaged(searchTokens, pageNumber, pageSize, allowFuzzyMatching);
                        var html = new StringBuilder();

                        if (pages.Count > 0)
                        {
                            html.Append("<ul>");

                            foreach (var page in pages)
                            {
                                if (showNamespace)
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
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

                        if (pageSelector && (pageNumber > 1 || pages.Count > 0 && pages.First().PaginationPageCount > 1))
                        {
                            html.Append(PageSelectorGenerator.Generate(refTag, state.QueryString, pages.FirstOrDefault()?.PaginationPageCount ?? 1));
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

                        if (pages.Count > 0)
                        {
                            html.Append("<ul>");

                            foreach (var page in pages)
                            {
                                if (showNamespace)
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Name}</a>");
                                }
                                else
                                {
                                    html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
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
                        string refTag = state.GetNextQueryToken();

                        var similarity = function.Parameters.Get<int>("similarity");
                        int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var html = new StringBuilder();

                        var pages = PageRepository.GetSimilarPagesPaged(state.Page.Id, similarity, pageNumber, pageSize);

                        if (styleName == "list")
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                            }
                            html.Append("</ul>");
                        }
                        else if (styleName == "flat")
                        {
                            foreach (var page in pages)
                            {
                                if (html.Length > 0) html.Append(" | ");
                                html.Append($"<a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                            }
                        }
                        else if (styleName == "full")
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a> - {page.Description}");
                            }
                            html.Append("</ul>");
                        }

                        if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
                        {
                            html.Append(PageSelectorGenerator.Generate(refTag, state.QueryString, pages.First().PaginationPageCount));
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays a list of other related pages based incoming links.
                case "related": //##related
                    {
                        string refTag = state.GetNextQueryToken();

                        int pageNumber = int.Parse(state.QueryString[refTag].ToString().DefaultWhenNullOrEmpty("1"));
                        var pageSize = function.Parameters.Get<int>("pageSize");
                        var pageSelector = function.Parameters.Get<bool>("pageSelector");
                        string styleName = function.Parameters.Get<string>("styleName").ToLower();
                        var html = new StringBuilder();

                        var pages = PageRepository.GetRelatedPagesPaged(state.Page.Id, pageNumber, pageSize);

                        if (styleName == "list")
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                            }
                            html.Append("</ul>");
                        }
                        else if (styleName == "flat")
                        {
                            foreach (var page in pages)
                            {
                                if (html.Length > 0) html.Append(" | ");
                                html.Append($"<a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a>");
                            }
                        }
                        else if (styleName == "full")
                        {
                            html.Append("<ul>");
                            foreach (var page in pages)
                            {
                                html.Append($"<li><a href=\"{GlobalConfiguration.BasePath}/{page.Navigation}\">{page.Title}</a> - {page.Description}");
                            }
                            html.Append("</ul>");
                        }

                        if (pageSelector && pages.Count > 0 && pages.First().PaginationPageCount > 1)
                        {
                            html.Append(PageSelectorGenerator.Generate(refTag, state.QueryString, pages.First().PaginationPageCount));
                        }

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the date and time that the current page was last modified.
                case "lastmodified":
                    {
                        if (state.Session == null)
                        {
                            throw new Exception($"Localization is not supported without SessionState.");
                        }

                        if (state.Page.ModifiedDate != DateTime.MinValue)
                        {
                            var localized = state.Session.LocalizeDateTime(state.Page.ModifiedDate);
                            return new HandlerResult($"{localized.ToShortDateString()} {localized.ToShortTimeString()}");
                        }

                        return new HandlerResult(string.Empty);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the date and time that the current page was created.
                case "created":
                    {
                        if (state.Session == null)
                        {
                            throw new Exception($"Localization is not supported without SessionState.");
                        }

                        if (state.Page.CreatedDate != DateTime.MinValue)
                        {
                            var localized = state.Session.LocalizeDateTime(state.Page.CreatedDate);
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
                        return new HandlerResult(state.Page.Title);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the title of the site.
                case "sitename":
                    {
                        return new HandlerResult(GlobalConfiguration.Name);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the title of the current page in title form.
                case "title":
                    {
                        return new HandlerResult($"<h1>{state.Page.Title}</h1>");
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the namespace of the current page.
                case "namespace":
                    {
                        return new HandlerResult(state.Page.Namespace ?? string.Empty);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                //Displays the namespace of the current page.
                case "snippet":
                    {
                        string name = function.Parameters.Get<string>("name");

                        if (state.Snippets.TryGetValue(name, out string? value))
                        {
                            return new HandlerResult(value);
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
                        return new HandlerResult(state.Page.Navigation);
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "systememojilist":
                    {
                        StringBuilder html = new();

                        html.Append($"<table class=\"table table-striped table-bordered \">");

                        html.Append($"<thead>");
                        html.Append($"<tr>");
                        html.Append($"<td><strong>Name</strong></td>");
                        html.Append($"<td><strong>Image</strong></td>");
                        html.Append($"<td><strong>Shortcut</strong></td>");
                        html.Append($"</tr>");
                        html.Append($"</thead>");

                        string category = state.QueryString["Category"].ToString();

                        html.Append($"<tbody>");

                        if (string.IsNullOrWhiteSpace(category) == false)
                        {
                            var emojis = EmojiRepository.GetEmojisByCategory(category);

                            foreach (var emoji in emojis)
                            {
                                html.Append($"<tr>");
                                html.Append($"<td>{emoji.Name}</td>");
                                //html.Append($"<td><img src=\"/images/emoji/{emoji.Path}\" /></td>");
                                html.Append($"<td><img src=\"{GlobalConfiguration.BasePath}/File/Emoji/{emoji.Name.ToLower()}\" /></td>");
                                html.Append($"<td>{emoji.Shortcut}</td>");
                                html.Append($"</tr>");
                            }
                        }

                        html.Append($"</tbody>");
                        html.Append($"</table>");

                        return new HandlerResult(html.ToString())
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "systememojicategorylist":
                    {
                        var categories = EmojiRepository.GetEmojiCategoriesGrouped();

                        StringBuilder html = new();

                        html.Append($"<table class=\"table table-striped table-bordered \">");

                        int rowNumber = 0;

                        html.Append($"<thead>");
                        html.Append($"<tr>");
                        html.Append($"<td><strong>Name</strong></td>");
                        html.Append($"<td><strong>Count of Emojis</strong></td>");
                        html.Append($"</tr>");
                        html.Append($"</thead>");

                        foreach (var category in categories)
                        {
                            if (rowNumber == 1)
                            {
                                html.Append($"<tbody>");
                            }

                            html.Append($"<tr>");
                            html.Append($"<td><a href=\"{GlobalConfiguration.BasePath}/wiki_help::list_of_emojis_by_category?category={category.Category}\">{category.Category}</a></td>");
                            html.Append($"<td>{category.EmojiCount:N0}</td>");
                            html.Append($"</tr>");
                            rowNumber++;
                        }

                        html.Append($"</tbody>");
                        html.Append($"</table>");

                        return new HandlerResult(html.ToString())
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                        };
                    }
            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
