using System.Text;
using System.Text.RegularExpressions;
using TightWiki.Engine.Function.Exceptions;

namespace TightWiki.Engine.Function
{
    public static partial class FunctionParser
    {
        [GeneratedRegex(@"(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)")]
        private static partial Regex FunctionCallParser();

        /// <summary>
        /// Parsed a function call, its parameters and matches it to a defined function and its prototype.
        /// </summary>
        public static FunctionCall ParseAndGetFunctionCall(FunctionPrototypeCollection prototypes, string functionCall, out int parseEndIndex)
        {
            var parsed = ParseFunctionCall(functionCall);

            var prototype = prototypes.Get(parsed.Demarcation, parsed.Name)
                ?? throw new WikiFunctionPrototypeNotDefinedException($"Function ({parsed.Name}) does not have a defined prototype.");

            parseEndIndex = parsed.EndIndex;

            return new FunctionCall(prototype, parsed.Arguments);
        }

        public static ParsedFunctionCall ParseFunctionCall(string functionCall)
        {
            string functionName = string.Empty;
            int parseEndIndex = 0;
            var arguments = new List<string>();

            var firstLine = functionCall.Split('\n')?.FirstOrDefault();

            if (firstLine == null || firstLine.Where(x => x == '(').Count() != firstLine.Where(x => x == ')').Count())
            {
                throw new WikiFunctionPrototypeSyntaxError($"Function parentheses mismatch.");
            }

            string functionDemarcation = functionCall.Substring(0, 2);

            var parameterMatches = FunctionCallParser().Matches(firstLine);
            if (parameterMatches.Count > 0)
            {
                var match = parameterMatches[0];
                int argumentStartIndex = match.Value.IndexOf('(');

                functionName = match.Value[..argumentStartIndex].ToLower().TrimStart(['{', '#', '@']).Trim();
                parseEndIndex = match.Index + match.Length;

                var trimmedArguments = match.ToString().Substring(argumentStartIndex);
                arguments = ParseArguments(trimmedArguments);
            }
            else //The function call has no parameters.
            {
                int endOfLine = functionCall.Substring(2).TakeWhile(c => char.IsLetterOrDigit(c)).Count(); //Find the first non-alphanumeric after the function demarcation (##, @@, etc).
                functionName = functionCall.Substring(2, endOfLine).ToLower().TrimStart(['{', '#', '@']).Trim();
                parseEndIndex = endOfLine + 2;
            }

            return new ParsedFunctionCall(functionDemarcation, functionName, parseEndIndex, arguments);
        }

        /// <summary>
        /// Parses function parameters into a list of arguments based on comma separation.
        /// String do not need to be enclosed in double-quotes unless they contain commas.
        /// </summary>
        public static List<string> ParseArgumentsAddParenthesis(string paramString)
        {
            if (paramString.StartsWith('(') || paramString.EndsWith(')'))
            {
                throw new WikiFunctionPrototypeSyntaxError($"Unexpected '(' or ')'.");
            }

            return ParseArguments($"({paramString})");
        }

        /// <summary>
        /// Parses function parameters into a list of arguments based on comma separation.
        /// String do not need to be enclosed in double-quotes unless they contain commas.
        /// </summary>
        public static List<string> ParseArguments(string paramString)
        {
            List<string> ps = new();

            int readPos = 0;

            var singleParam = new StringBuilder();

            if (paramString[readPos] != '(')
            {
                throw new WikiFunctionPrototypeSyntaxError($"Expected '('.");
            }

            int parenNest = 1;

            readPos++; //Skip the (

            while (readPos < paramString.Length && char.IsWhiteSpace(paramString[readPos])) readPos++;

            while (true)
            {
                if (paramString[readPos] == '(')
                {
                    parenNest++;
                }
                else if (paramString[readPos] == ')')
                {
                    parenNest--;
                }

                if (readPos == paramString.Length)
                {
                    throw new WikiFunctionPrototypeSyntaxError($"Expected ')'.");
                }
                else if (paramString[readPos] == ')' && parenNest == 0)
                {
                    readPos++; //Skip the )

                    if (parenNest == 0 && readPos != paramString.Length)
                    {
                        throw new WikiFunctionPrototypeSyntaxError($"Expected end of statement.");
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
                else if (paramString[readPos] == '\"')
                {
                    readPos++; //Skip the ".

                    bool escapeChar = false;
                    for (; ; readPos++)
                    {
                        if (readPos == paramString.Length)
                        {
                            throw new WikiFunctionPrototypeSyntaxError($"Expected end of string.");
                        }
                        else if (paramString[readPos] == '\\')
                        {
                            escapeChar = true;
                            continue;
                        }
                        else if (paramString[readPos] == '\"' && escapeChar == false)
                        {
                            //Found the end of the string:
                            readPos++; //Skip the ".
                            break;
                        }
                        else
                        {
                            singleParam.Append(paramString[readPos]);
                        }
                        escapeChar = false;
                    }

                    while (readPos < paramString.Length && char.IsWhiteSpace(paramString[readPos])) readPos++;
                }
                else if (paramString[readPos] == ',')
                {
                    readPos++; //Skip the ,
                    while (readPos < paramString.Length && char.IsWhiteSpace(paramString[readPos])) readPos++;

                    ps.Add(singleParam.ToString());
                    singleParam.Clear();
                    continue;
                }
                else
                {
                    singleParam.Append(paramString[readPos]);

                    if (paramString[readPos] == '(')
                    {
                        readPos++;
                        while (readPos < paramString.Length && char.IsWhiteSpace(paramString[readPos])) readPos++;
                    }
                    else if (paramString[readPos] == ')')
                    {
                        readPos++;
                        while (readPos < paramString.Length && char.IsWhiteSpace(paramString[readPos])) readPos++;
                    }
                    else
                    {
                        readPos++;
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
