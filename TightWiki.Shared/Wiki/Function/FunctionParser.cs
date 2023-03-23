using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace TightWiki.Shared.Wiki.Function
{
    public static class FunctionParser
    {
        public static FunctionCallInstance ParseFunctionCallInfo(OrderedMatch orderedMatch, out int parseEndIndex)
        {
            List<string> rawArguments = new List<string>();

            string functionName = null;

            var firstLine = orderedMatch.Value.Split('\n')?.FirstOrDefault();

            if (firstLine.Where(x => (x == '(')).Count() != firstLine.Where(x => (x == ')')).Count())
            {
                throw new Exception($"Function parentheses mismatch.");
            }

            string functionPrefix = orderedMatch.Value.Substring(0, 2);

            MatchCollection parameterMatches = (new Regex(@"(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)")).Matches(firstLine);
            if (parameterMatches.Count > 0)
            {
                var match = parameterMatches[0];
                int paramStartIndex = match.Value.IndexOf('(');

                functionName = match.Value[..paramStartIndex].ToLower().TrimStart(new char[] { '{', '#', '@' }).Trim();
                parseEndIndex = match.Index + match.Length;

                string rawArgTrimmed = match.ToString().Substring(paramStartIndex, (match.ToString().Length - paramStartIndex));
                rawArguments = ParseRawArguments(rawArgTrimmed);
            }
            else //The function call has no parameters.
            {
                int endOfLine = orderedMatch.Value.Substring(2).TakeWhile(c => char.IsLetterOrDigit(c)).Count(); //Find the first non-alphanumeric after the function identifier (##, @@, etc).
                functionName = orderedMatch.Value.Substring(2, endOfLine).ToLower().TrimStart(new char[] { '{', '#', '@' }).Trim();
                parseEndIndex = endOfLine + 2;
            }

            var prototype = FunctionPrototypeDefinitions.Get(functionPrefix, functionName);
            if (prototype == null)
            {
                throw new Exception($"Function ({functionName}) does not have a defined prototype.");
            }

            return new FunctionCallInstance(prototype, rawArguments);
        }

        public static List<string> ParseRawArgumentsAddParens(string paramString)
        {
            return ParseRawArguments($"({paramString})");
        }

        public static List<string> ParseRawArguments(string paramString)
        {
            List<string> ps = new();

            int iRpos = 0;

            var singleParam = new StringBuilder();

            if (paramString[iRpos] != '(')
            {
                throw new Exception($"Expected '('.");
            }

            int parenNest = 1;

            //https://localhost:44349/get_standard_function_wiki_help

            iRpos++; //Skip the (

            while (iRpos < paramString.Length && char.IsWhiteSpace(paramString[iRpos])) iRpos++;

            while (true)
            {
                if (paramString[iRpos] == '(')
                {
                    parenNest++;
                }
                else if (paramString[iRpos] == ')')
                {
                    parenNest--;
                }

                if (iRpos == paramString.Length)
                {
                    throw new Exception($"Expected ')'.");
                }
                else if (paramString[iRpos] == ')' && parenNest == 0)
                {
                    iRpos++; //Skip the )

                    if (parenNest == 0 && iRpos != paramString.Length)
                    {
                        throw new Exception($"Expected end of statement.");
                    }

                    if (singleParam.Length > 0)
                    {
                        ps.Add(singleParam.ToString());
                    }
                    singleParam.Clear();

                    if (parenNest == 0)
                    {
                        break;
                    }
                }
                else if (paramString[iRpos] == '\"')
                {
                    iRpos++; //Skip the ".

                    bool escapeChar = false;
                    for (; ; iRpos++)
                    {
                        if (iRpos == paramString.Length)
                        {
                            throw new Exception($"Expected end of string.");
                        }
                        else if (paramString[iRpos] == '\\')
                        {
                            escapeChar = true;
                            continue;
                        }
                        else if (paramString[iRpos] == '\"' && escapeChar == false)
                        {
                            //Found the end of the string:
                            /*

                            Regex rgx = new Regex(@"\{.+\}", RegexOptions.IgnoreCase);
                            var matches = rgx.Matches(singleParam.ToString());
                            foreach (Match match in matches.Cast<Match>())
                            {
                            }
                            */

                            iRpos++; //Skip the ".
                            break;
                        }
                        else
                        {
                            singleParam.Append(paramString[iRpos]);
                        }
                        escapeChar = false;
                    }

                    while (iRpos < paramString.Length && char.IsWhiteSpace(paramString[iRpos])) iRpos++;
                }
                else if (paramString[iRpos] == ',')
                {
                    iRpos++; //Skip the ,
                    while (iRpos < paramString.Length && char.IsWhiteSpace(paramString[iRpos])) iRpos++;

                    ps.Add(singleParam.ToString());
                    singleParam.Clear();
                    continue;
                }
                else
                {
                    singleParam.Append(paramString[iRpos]);

                    if (paramString[iRpos] == '(')
                    {
                        iRpos++;
                        while (iRpos < paramString.Length && char.IsWhiteSpace(paramString[iRpos])) iRpos++;
                    }
                    else if (paramString[iRpos] == ')')
                    {
                        iRpos++;
                        while (iRpos < paramString.Length && char.IsWhiteSpace(paramString[iRpos])) iRpos++;
                    }
                    else
                    {
                        iRpos++;
                    }
                }


            }

            for (int i = 0; i < ps.Count; i++)
            {
                ps[i] = ps[i].Trim();
            }

            return ps;
        }
    }
}
