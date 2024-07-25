using System.Text;
using System.Text.RegularExpressions;

namespace TightWiki.EngineFunction
{
    public static partial class FunctionParser
    {
        [GeneratedRegex(@"(##|{{|@@)([a-zA-Z_\s{][a-zA-Z0-9_\s{]*)\(((?<BR>\()|(?<-BR>\))|[^()]*)+\)")]
        private static partial Regex FunctionCallParser();

        /// <summary>
        /// Parsed a function call, its parameters and matches it to a defined function and its prototype.
        /// </summary>
        /// <param name="functionCall"></param>
        /// <param name="parseEndIndex"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static FunctionCall ParseFunctionCall(string functionCall, out int parseEndIndex)
        {
            var rawArguments = new List<string>();

            string functionName = string.Empty;

            var firstLine = functionCall.Split('\n')?.FirstOrDefault();

            if (firstLine == null || firstLine.Where(x => x == '(').Count() != firstLine.Where(x => x == ')').Count())
            {
                throw new Exception($"Function parentheses mismatch.");
            }

            string functionPrefix = functionCall.Substring(0, 2);

            var parameterMatches = FunctionCallParser().Matches(firstLine);
            if (parameterMatches.Count > 0)
            {
                var match = parameterMatches[0];
                int paramStartIndex = match.Value.IndexOf('(');

                functionName = match.Value[..paramStartIndex].ToLower().TrimStart(['{', '#', '@']).Trim();
                parseEndIndex = match.Index + match.Length;

                string rawArgTrimmed = match.ToString().Substring(paramStartIndex, (match.ToString().Length - paramStartIndex));
                rawArguments = ParseRawArguments(rawArgTrimmed);
            }
            else //The function call has no parameters.
            {
                int endOfLine = functionCall.Substring(2).TakeWhile(c => char.IsLetterOrDigit(c)).Count(); //Find the first non-alphanumeric after the function identifier (##, @@, etc).
                functionName = functionCall.Substring(2, endOfLine).ToLower().TrimStart(['{', '#', '@']).Trim();
                parseEndIndex = endOfLine + 2;
            }

            var prototype = FunctionPrototypeDefinitions.Get(functionPrefix, functionName);
            if (prototype == null)
            {
                throw new Exception($"Function ({functionName}) does not have a defined prototype.");
            }

            return new FunctionCall(prototype, rawArguments);
        }

        /// <summary>
        /// Parses function parameters into a list of arguments based on comma separation.
        /// String do not need to be enclosed in double-quotes unless they contain commas.
        /// </summary>
        /// <param name="paramString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<string> ParseRawArgumentsAddParenthesis(string paramString)
        {
            if (paramString.StartsWith('(') || paramString.EndsWith(')'))
            {
                throw new Exception($"Unexpected '(' or ')'.");
            }

            return ParseRawArguments($"({paramString})");
        }

        /// <summary>
        /// Parses function parameters into a list of arguments based on comma separation.
        /// String do not need to be enclosed in double-quotes unless they contain commas.
        /// </summary>
        /// <param name="paramString"></param>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        public static List<string> ParseRawArguments(string paramString)
        {
            List<string> ps = new();

            int readPos = 0;

            var singleParam = new StringBuilder();

            if (paramString[readPos] != '(')
            {
                throw new Exception($"Expected '('.");
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
                    throw new Exception($"Expected ')'.");
                }
                else if (paramString[readPos] == ')' && parenNest == 0)
                {
                    readPos++; //Skip the )

                    if (parenNest == 0 && readPos != paramString.Length)
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
                else if (paramString[readPos] == '\"')
                {
                    readPos++; //Skip the ".

                    bool escapeChar = false;
                    for (; ; readPos++)
                    {
                        if (readPos == paramString.Length)
                        {
                            throw new Exception($"Expected end of string.");
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
