using System.Text;
using TightWiki.Engine.Library.Function.Exceptions;

namespace TightWiki.Engine.Library.Function
{
    /// <summary>
    /// Represnets a function call that has been parsed of its name, type and arguments.
    /// </summary>
    public class ParsedFunction
    {
        public string Demarcation { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int EndIndex { get; set; }
        public List<string> Arguments { get; set; } = new List<string>();
        public string? BodyText { get; }

        public ParsedFunction(string demarcation, string name, int endIndex, List<string> arguments, string? bodyText)
        {
            Demarcation = demarcation;
            Name = name;
            EndIndex = endIndex;
            Arguments = arguments;
            BodyText = bodyText;
        }

        /// <summary>
        /// Parses function parameters into a list of arguments based on comma separation.
        /// String do not need to be enclosed in double-quotes unless they contain commas.
        /// </summary>
        public static List<string> ParseArgumentsAddParenthesis(string paramString)
        {
            if (paramString.StartsWith('(') || paramString.EndsWith(')'))
            {
                throw new WikiFunctionParserError($"Unexpected '(' or ')'.");
            }

            return ParseArguments($"({paramString})");
        }

        public static ParsedFunction Create(string functionCall)
        {
            string functionName = string.Empty;
            int parseEndIndex = 0;
            var arguments = new List<string>();
            var firstLine = functionCall.Split('\n')?.FirstOrDefault();

            if (firstLine == null || firstLine.Where(x => x == '(').Count() != firstLine.Where(x => x == ')').Count())
            {
                throw new WikiFunctionParserError($"Function parentheses mismatch.");
            }

            string? bodyText = null;

            string functionDemarcation = functionCall.Substring(0, 2);

            var parameterMatches = PrecompiledRegex.FunctionCallParser().Matches(firstLine);
            if (parameterMatches.Count > 0)
            {
                var match = parameterMatches[0];
                int argumentStartIndex = match.Value.IndexOf('(');

                functionName = match.Value[..argumentStartIndex].ToLowerInvariant().TrimStart(['{', '#', '@']).Trim();
                parseEndIndex = match.Index + match.Length;

                var trimmedArguments = match.ToString().Substring(argumentStartIndex);
                arguments = ParseArguments(trimmedArguments);

                if (ParseDemarcation(functionDemarcation) == WikiFunctionType.Scoped)
                {
                    bodyText = functionCall.Substring(parseEndIndex, (functionCall.Length - parseEndIndex) - 2).Trim();
                }
            }
            else //The function call has no parameters.
            {
                int endOfLine = functionCall.Substring(2).TakeWhile(c => char.IsLetterOrDigit(c)).Count(); //Find the first non-alphanumeric after the function demarcation (##, @@, etc).
                functionName = functionCall.Substring(2, endOfLine).ToLowerInvariant().TrimStart(['{', '#', '@']).Trim();
                parseEndIndex = endOfLine + 2;

                if (ParseDemarcation(functionDemarcation) == WikiFunctionType.Scoped)
                {
                    bodyText = functionCall.Substring(parseEndIndex, (functionCall.Length - parseEndIndex) - 2).Trim();
                }
            }

            return new ParsedFunction(functionDemarcation, functionName, parseEndIndex, arguments, bodyText);
        }

        private static WikiFunctionType ParseDemarcation(string demarcation)
        {
            return demarcation switch
            {
                "##" => WikiFunctionType.Standard,
                "{{" => WikiFunctionType.Scoped,
                "@@" => WikiFunctionType.Instruction,
                _ => throw new Exception("Invalid demarcation string."),
            };
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
                throw new WikiFunctionParserError($"Expected '('.");
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
                    throw new WikiFunctionParserError($"Expected ')'.");
                }
                else if (paramString[readPos] == ')' && parenNest == 0)
                {
                    readPos++; //Skip the )

                    if (parenNest == 0 && readPos != paramString.Length)
                    {
                        throw new WikiFunctionParserError($"Expected end of statement.");
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
                            throw new WikiFunctionParserError($"Expected end of string.");
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
