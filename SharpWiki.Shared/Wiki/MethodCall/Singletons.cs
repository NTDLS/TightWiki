using SharpWiki.Shared.Library;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace SharpWiki.Shared.Wiki.MethodCall
{
    public static class Singletons
    {
        private static MethodPrototypeCollection _methodPrototypes;
        public static MethodPrototypeCollection MethodPrototypes
        {
            get
            {
                if (_methodPrototypes == null)
                {
                    _methodPrototypes = new MethodPrototypeCollection();
                    _methodPrototypes.Add("PanelScope: <string>[boxType(bullets,bullets-ordered,alert,alert-default,alert-info,alert-danger,alert-warning,alert-success,jumbotron,block,block-default,block-primary,block-success,block-success,block-info,block-warning,block-danger,panel,panel-default,panel-primary,panel-success,panel-info,panel-warning,panel-danger)] | <string>{title}=''");
                    _methodPrototypes.Add("Tag: <string:infinite>[tags]");
                    _methodPrototypes.Add("TextList: <string:infinite>[tags] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TagList: <string:infinite>[tags] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("SearchCloud: <string:infinite>[tokens] | <int>{Top}='1000'");
                    _methodPrototypes.Add("TagGlossary: <string:infinite>[tags] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("RecentlyModified: <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TextGlossary: <string:infinite>[tokens] | <int>{Top}='1000' | <string>{view(List,Full)}='Full'");
                    _methodPrototypes.Add("TagCloud: <string>[tag] | <int>{Top}='1000'");
                    _methodPrototypes.Add("Image: <string>[name] | <int>{scale}='100' | <string>{altText}=''");
                    _methodPrototypes.Add("File: <string>[name] | <string>[linkText] | <bool>{showSize}='false'");
                    _methodPrototypes.Add("Related: <int>{Top}='1000' | <string>{view(List,Flat,Full)}='Full'");
                    _methodPrototypes.Add("Tags: <string>{view(Flat,list)}='list'");
                    _methodPrototypes.Add("Include: <string>[pageName]");
                    _methodPrototypes.Add("BR: <int>{Count}='1'");
                    _methodPrototypes.Add("NL: <int>{Count}='1'");
                    _methodPrototypes.Add("NewLine: <int>{Count}='1'");
                    _methodPrototypes.Add("TOC:");
                    _methodPrototypes.Add("Title:");
                    _methodPrototypes.Add("Navigation:");
                    _methodPrototypes.Add("Name:");
                    _methodPrototypes.Add("Created:");
                    _methodPrototypes.Add("LastModified:");
                    _methodPrototypes.Add("Files:");
                    _methodPrototypes.Add("Depreciate:");
                    _methodPrototypes.Add("Protect:<bool>{isSilent}='false'");
                    _methodPrototypes.Add("Remplate:");
                    _methodPrototypes.Add("Review:");
                    _methodPrototypes.Add("Include:");
                    _methodPrototypes.Add("Draft:");
                }

                return _methodPrototypes;
            }
        }

        public static MethodCallInstance ParseMethodCallInfo(OrderedMatch methodMatch, out int parseEndIndex, string methodName = null)
        {
            List<string> rawArguments = new List<string>();

            MatchCollection matches = (new Regex(@"(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)")).Matches(methodMatch.Value);
            if (matches.Count > 0)
            {
                var match = matches[0];

                int paramStartIndex = match.Value.IndexOf('(');

                if (methodName == null)
                {
                    methodName = match.Value.Substring(0, paramStartIndex).ToLower().TrimStart(new char[] { '{', '#', '@' } );
                }

                parseEndIndex = match.Index + match.Length;

                string rawArgTrimmed = match.ToString().Substring(paramStartIndex + 1, (match.ToString().Length - paramStartIndex) - 2);
                rawArguments = rawArgTrimmed.ToString().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToList();
            }
            else if (methodName == null)
            {
                methodName = methodMatch.Value.Substring(2, methodMatch.Value.Length - 2).ToLower(); ; //The match has no parameter.
                parseEndIndex = methodMatch.Value.Length;
            }
            else
            {
                parseEndIndex = -1;
            }

            var prototype = Singletons.MethodPrototypes.Get(methodName);
            if (prototype == null)
            {
                throw new Exception($"Method ({methodName}) does not have a defined prototype.");
            }

            return new MethodCallInstance(prototype, rawArguments);
        }

    }
}
