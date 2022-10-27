using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace TightWiki.Shared.Wiki.MethodCall
{
    public static class MethodParser
    {
        public static MethodCallInstance ParseMethodCallInfo(OrderedMatch methodMatch, out int parseEndIndex)
        {
            List<string> rawArguments = new List<string>();

            string methodName = null;

            var firstLine = methodMatch.Value.Split('\n')?.FirstOrDefault();

            if (firstLine.Where(x => (x == '(')).Count() != firstLine.Where(x => (x == ')')).Count())
            {
                throw new Exception($"Method parentheses mismatch.");
            }

            MatchCollection matches = (new Regex(@"(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)")).Matches(firstLine);
            if (matches.Count > 0)
            {
                var match = matches[0];

                int paramStartIndex = match.Value.IndexOf('(');

                methodName = match.Value[..paramStartIndex].ToLower().TrimStart(new char[] { '{', '#', '@' }).Trim();

                parseEndIndex = match.Index + match.Length;

                string rawArgTrimmed = match.ToString().Substring(paramStartIndex + 1, (match.ToString().Length - paramStartIndex) - 2);
                rawArguments = rawArgTrimmed.ToString().Split(new char[] { '|' }, StringSplitOptions.RemoveEmptyEntries).Select(o => o.Trim()).ToList();
            }
            else //The method call has no parameters.
            {
                int endOfLine = methodMatch.Value.Substring(2).TakeWhile(c => char.IsLetterOrDigit(c)).Count(); //Find the first non-alphanumeric after the method identifier (##, @@, etc).
                methodName = methodMatch.Value.Substring(2, endOfLine).ToLower().TrimStart(new char[] { '{', '#', '@' }).Trim();
                parseEndIndex = endOfLine + 2;
            }

            var prototype = MethodPrototypeDefinitions.Get(methodName);
            if (prototype == null)
            {
                throw new Exception($"Method ({methodName}) does not have a defined prototype.");
            }

            return new MethodCallInstance(prototype, rawArguments);
        }
    }
}
