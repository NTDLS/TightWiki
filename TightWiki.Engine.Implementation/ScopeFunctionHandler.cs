using System.Text;
using TightWiki.Engine.Implementation.Utility;
using TightWiki.Engine.Library;
using TightWiki.Engine.Library.Interfaces;
using TightWiki.EngineFunction;
using static TightWiki.Engine.Library.Constants;
using static TightWiki.EngineFunction.FunctionPrototypeCollection;

namespace TightWiki.Engine.Implementation
{
    public class ScopeFunctionHandler : IScopeFunctionHandler
    {
        private static FunctionPrototypeCollection? _collection;

        public FunctionPrototypeCollection Prototypes
        {
            get
            {
                if (_collection == null)
                {
                    _collection = new FunctionPrototypeCollection(WikiFunctionType.Scoped);

                    #region Prototypes.

                    _collection.Add("$$Code: <string>{language(auto,wiki,cpp,lua,graphql,swift,r,yaml,kotlin,scss,shell,vbnet,json,objectivec,perl,diff,wasm,php,xml,bash,csharp,css,go,ini,javascript,less,makefile,markdown,plaintext,python,python-repl,ruby,rust,sql,typescript)}='auto'");
                    _collection.Add("$$Bullets: <string>{type(unordered,ordered)}='unordered'");
                    _collection.Add("$$Order: <string>{direction(ascending,descending)}='ascending'");
                    _collection.Add("$$Jumbotron:");
                    _collection.Add("$$Callout: <string>{styleName(default,primary,secondary,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _collection.Add("$$Background: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _collection.Add("$$Foreground: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger,muted)}='default'");
                    _collection.Add("$$Alert: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _collection.Add("$$Card: <string>{styleName(default,primary,secondary,light,dark,success,info,warning,danger)}='default' | <string>{titleText}=''");
                    _collection.Add("$$Collapse: <string>{linkText}='Show'");
                    _collection.Add("$$Table: <boolean>{hasBorder}='true' | <boolean>{isFirstRowHeader}='true'");
                    _collection.Add("$$StripedTable: <boolean>{hasBorder}='true' | <boolean>{isFirstRowHeader}='true'");
                    _collection.Add("$$DefineSnippet: <string>[name]");

                    #endregion
                }

                return _collection;
            }
        }

        public HandlerResult Handle(ITightEngineState state, FunctionCall function, string scopeBody)
        {
            switch (function.Name.ToLower())
            {
                //------------------------------------------------------------------------------------------------------------------------------
                case "code":
                    {
                        var html = new StringBuilder();

                        string language = function.Parameters.Get<string>("language");
                        if (string.IsNullOrEmpty(language) || language?.ToLower() == "auto")
                        {
                            html.Append($"<pre>");
                            html.Append($"<code>{scopeBody.Replace("\r\n", "\n").Replace("\n", SoftBreak)}</code></pre>");
                        }
                        else
                        {
                            html.Append($"<pre class=\"language-{language}\">");
                            html.Append($"<code>{scopeBody.Replace("\r\n", "\n").Replace("\n", SoftBreak)}</code></pre>");
                        }

                        return new HandlerResult(html.ToString())
                        {
                            Instructions = [HandlerResultInstruction.DisallowNestedProcessing]
                        };
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "stripedtable":
                case "table":
                    {
                        var html = new StringBuilder();

                        var hasBorder = function.Parameters.Get<bool>("hasBorder");
                        var isFirstRowHeader = function.Parameters.Get<bool>("isFirstRowHeader");

                        html.Append($"<table class=\"table");

                        if (function.Name.ToLower() == "stripedtable")
                        {
                            html.Append(" table-striped");
                        }
                        if (hasBorder)
                        {
                            html.Append(" table-bordered");
                        }

                        html.Append($"\">");

                        var lines = scopeBody.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                        int rowNumber = 0;

                        foreach (var lineText in lines)
                        {
                            var columns = lineText.Split("||");

                            if (rowNumber == 0 && isFirstRowHeader)
                            {
                                html.Append($"<thead>");
                            }
                            else if (rowNumber == 1 && isFirstRowHeader || rowNumber == 0 && isFirstRowHeader == false)
                            {
                                html.Append($"<tbody>");
                            }

                            html.Append($"<tr>");
                            foreach (var columnText in columns)
                            {
                                if (rowNumber == 0 && isFirstRowHeader)
                                {
                                    html.Append($"<td><strong>{columnText}</strong></td>");
                                }
                                else
                                {
                                    html.Append($"<td>{columnText}</td>");
                                }
                            }

                            if (rowNumber == 0 && isFirstRowHeader)
                            {
                                html.Append($"</thead>");
                            }
                            html.Append($"</tr>");

                            rowNumber++;
                        }

                        html.Append($"</tbody>");
                        html.Append($"</table>");

                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "bullets":
                    {
                        var html = new StringBuilder();

                        string type = function.Parameters.Get<string>("type");

                        if (type == "unordered")
                        {
                            var lines = scopeBody.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                            int currentLevel = 0;

                            foreach (var line in lines)
                            {
                                int newIndent = 0;
                                for (; newIndent < line.Length && line[newIndent] == '>'; newIndent++)
                                {
                                    //Count how many '>' are at the start of the line.
                                }
                                newIndent++;

                                if (newIndent < currentLevel)
                                {
                                    for (; currentLevel != newIndent; currentLevel--)
                                    {
                                        html.Append($"</ul>");
                                    }
                                }
                                else if (newIndent > currentLevel)
                                {
                                    for (; currentLevel != newIndent; currentLevel++)
                                    {
                                        html.Append($"<ul>");
                                    }
                                }

                                html.Append($"<li>{line.Trim(new char[] { '>' })}</li>");
                            }

                            for (; currentLevel > 0; currentLevel--)
                            {
                                html.Append($"</ul>");
                            }
                        }
                        else if (type == "ordered")
                        {
                            var lines = scopeBody.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).Where(o => o.Length > 0);

                            int currentLevel = 0;

                            foreach (var line in lines)
                            {
                                int newIndent = 0;
                                for (; newIndent < line.Length && line[newIndent] == '>'; newIndent++)
                                {
                                    //Count how many '>' are at the start of the line.
                                }
                                newIndent++;

                                if (newIndent < currentLevel)
                                {
                                    for (; currentLevel != newIndent; currentLevel--)
                                    {
                                        html.Append($"</ol>");
                                    }
                                }
                                else if (newIndent > currentLevel)
                                {
                                    for (; currentLevel != newIndent; currentLevel++)
                                    {
                                        html.Append($"<ol>");
                                    }
                                }

                                html.Append($"<li>{line.Trim(new char[] { '>' })}</li>");
                            }

                            for (; currentLevel > 0; currentLevel--)
                            {
                                html.Append($"</ol>");
                            }
                        }
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "definesnippet":
                    {
                        var html = new StringBuilder();

                        string name = function.Parameters.Get<string>("name");

                        if (state.Snippets.ContainsKey(name))
                        {
                            state.Snippets[name] = scopeBody;
                        }
                        else
                        {
                            state.Snippets.Add(name, scopeBody);
                        }
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "alert":
                    {
                        var html = new StringBuilder();

                        string titleText = function.Parameters.Get<string>("titleText");
                        string style = function.Parameters.Get<string>("styleName").ToLower();
                        style = style == "default" ? "" : $"alert-{style}";

                        if (!string.IsNullOrEmpty(titleText)) scopeBody = $"<h1>{titleText}</h1>{scopeBody}";
                        html.Append($"<div class=\"alert {style}\">{scopeBody}</div>");
                        return new HandlerResult(html.ToString());
                    }

                case "order":
                    {
                        var html = new StringBuilder();

                        string direction = function.Parameters.Get<string>("direction");
                        var lines = scopeBody.Split("\n").Select(o => o.Trim()).ToList();

                        if (direction == "ascending")
                        {
                            html.Append(string.Join("\r\n", lines.OrderBy(o => o)));
                        }
                        else
                        {
                            html.Append(string.Join("\r\n", lines.OrderByDescending(o => o)));
                        }
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "jumbotron":
                    {
                        var html = new StringBuilder();

                        string titleText = function.Parameters.Get("titleText", "");
                        html.Append($"<div class=\"mt-4 p-5 bg-secondary text-white rounded\">");
                        if (!string.IsNullOrEmpty(titleText)) html.Append($"<h1>{titleText}</h1>");
                        html.Append($"<p>{scopeBody}</p>");
                        html.Append($"</div>");
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "foreground":
                    {
                        var html = new StringBuilder();

                        var style = BGFGStyle.GetForegroundStyle(function.Parameters.Get("styleName", "default")).Swap();
                        html.Append($"<p class=\"{style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</p>");
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "background":
                    {
                        var html = new StringBuilder();

                        var style = BGFGStyle.GetBackgroundStyle(function.Parameters.Get("styleName", "default"));
                        html.Append($"<div class=\"p-3 mb-2 {style.ForegroundStyle} {style.BackgroundStyle}\">{scopeBody}</div>");
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "collapse":
                    {
                        var html = new StringBuilder();

                        string linkText = function.Parameters.Get<string>("linktext");
                        string uid = "A" + Guid.NewGuid().ToString().Replace("-", "");
                        html.Append($"<a data-bs-toggle=\"collapse\" href=\"#{uid}\" role=\"button\" aria-expanded=\"false\" aria-controls=\"{uid}\">{linkText}</a>");
                        html.Append($"<div class=\"collapse\" id=\"{uid}\">");
                        html.Append($"<div class=\"card card-body\"><p class=\"card-text\">{scopeBody}</p></div></div>");
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "callout":
                    {
                        var html = new StringBuilder();

                        string titleText = function.Parameters.Get<string>("titleText");
                        string style = function.Parameters.Get<string>("styleName").ToLower();
                        style = style == "default" ? "" : style;

                        html.Append($"<div class=\"bd-callout bd-callout-{style}\">");
                        if (string.IsNullOrWhiteSpace(titleText) == false) html.Append($"<h4>{titleText}</h4>");
                        html.Append($"{scopeBody}");
                        html.Append($"</div>");
                        return new HandlerResult(html.ToString());
                    }

                //------------------------------------------------------------------------------------------------------------------------------
                case "card":
                    {
                        var html = new StringBuilder();

                        string titleText = function.Parameters.Get<string>("titleText");
                        var style = BGFGStyle.GetBackgroundStyle(function.Parameters.Get("styleName", "default"));

                        html.Append($"<div class=\"card {style.ForegroundStyle} {style.BackgroundStyle} mb-3\">");
                        if (string.IsNullOrEmpty(titleText) == false)
                        {
                            html.Append($"<div class=\"card-header\">{titleText}</div>");
                        }
                        html.Append("<div class=\"card-body\">");
                        html.Append($"<p class=\"card-text\">{scopeBody}</p>");
                        html.Append("</div>");
                        html.Append("</div>");
                        return new HandlerResult(html.ToString());

                    }

            }

            return new HandlerResult() { Instructions = [HandlerResultInstruction.Skip] };
        }
    }
}
