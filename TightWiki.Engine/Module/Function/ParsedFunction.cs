using System.Text;
using TightWiki.Plugin;

namespace TightWiki.Engine.Module.Function
{
    /// <summary>
    /// Represnets a function call that has been parsed of its name, type and arguments.
    /// </summary>
    public class ParsedFunction(string demarcation, string name, List<string> arguments, string? bodyText)
    {
        public string Demarcation { get; set; } = demarcation;
        public string Name { get; set; } = name;
        public List<string> Arguments { get; set; } = arguments;
        public string? BodyText { get; } = bodyText;

        /// <summary>
        /// Parses function parameters into a list of arguments based on comma separation.
        /// String do not need to be enclosed in double-quotes unless they contain commas.
        /// </summary>
        public static List<string> ParseArgumentsAddParenthesis(string paramString)
        {
            if (paramString.StartsWith('(') || paramString.EndsWith(')'))
            {
                throw new Exception($"Unexpected '(' or ')'.");
            }

            return ParseArguments($"({paramString})");
        }

        public static ParsedFunction Create(string functionCall)
        {
            string functionName = string.Empty;
            var arguments = new List<string>();
            var firstLine = functionCall.Split('\n')?.FirstOrDefault();

            if (firstLine == null || firstLine.Where(x => x == '(').Count() != firstLine.Where(x => x == ')').Count())
            {
                throw new Exception($"Function parentheses mismatch.");
            }

            string? bodyText = null;

            var functionDemarcation = functionCall.Substring(0, 2);
            if (string.IsNullOrEmpty(functionDemarcation))
            {
                throw new Exception($"Function demarcation is missing.");
            }

            var parameterMatches = PrecompiledRegex.FunctionCallParser().Matches(firstLine);
            if (parameterMatches.Count > 0)
            {
                var match = parameterMatches[0];
                int argumentStartIndex = match.Value.IndexOf('(');

                functionName = match.Value[..argumentStartIndex].ToLowerInvariant().TrimStart(['{', '#', '@']).Trim();
                int parseEndIndex = match.Index + match.Length;

                var trimmedArguments = match.ToString().Substring(argumentStartIndex);
                arguments = ParseArguments(trimmedArguments);

                if (ParseDemarcation(functionDemarcation) == TwFunctionType.Scoped)
                {
                    bodyText = functionCall.Substring(parseEndIndex, (functionCall.Length - parseEndIndex) - functionDemarcation.Length).Trim();
                }
            }
            else //The function call has no parameters and no open/close parentheses.
            {
                //What we are tring to do here is find the name of the function and (for scoped functions) also parse
                //  the body content after the function name - making sure to excluse the open and close demarcation.
                int skippedCharsCount = functionDemarcation.Length;
                for (; skippedCharsCount < functionCall.Length; skippedCharsCount++)
                {
                    //Skip whitespace between the demarcation and the start of the function name.
                    if (functionCall[skippedCharsCount] != ' ' && functionCall[skippedCharsCount] != '\t')
                    {
                        break;
                    }
                }

                for (; skippedCharsCount < functionCall.Length; skippedCharsCount++)
                {
                    //Skip characters until the first non-alphanumeric character after the function name.
                    if (!char.IsLetterOrDigit(functionCall[skippedCharsCount]))
                    {
                        break;
                    }
                }

                functionName = functionCall.Substring(functionDemarcation.Length, skippedCharsCount - functionDemarcation.Length).ToLowerInvariant().TrimStart(['{', '#', '@']).Trim();

                if (ParseDemarcation(functionDemarcation) == TwFunctionType.Scoped)
                {
                    //From the end of the function name, to the end of the while string, less the length of the demarcation.
                    bodyText = functionCall.Substring(skippedCharsCount, (functionCall.Length - skippedCharsCount) - functionDemarcation.Length).Trim();
                }
            }

            if (string.IsNullOrEmpty(functionName))
            {
                throw new Exception($"Function name is missing.");
            }

            return new ParsedFunction(functionDemarcation, functionName, arguments, bodyText);
        }

        private static TwFunctionType ParseDemarcation(string demarcation)
        {
            return demarcation switch
            {
                "##" => TwFunctionType.Standard,
                "{{" => TwFunctionType.Scoped,
                "@@" => TwFunctionType.Instruction,
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
