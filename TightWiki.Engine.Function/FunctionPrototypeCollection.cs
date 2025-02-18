using System.Runtime.CompilerServices;
using System.Text;
using TightWiki.Engine.Function.Exceptions;

namespace TightWiki.Engine.Function
{
    public class FunctionPrototypeCollection
    {
        public enum WikiFunctionType
        {
            Standard,
            Scoped,
            Instruction
        }

        public enum WikiFunctionParamType
        {
            Undefined,
            String,
            InfiniteString,
            Integer,
            Double,
            Boolean
        }

        public WikiFunctionType FunctionTypes { get; private set; }
        public List<PrototypeSet> Items { get; set; } = new();

        public FunctionPrototypeCollection(WikiFunctionType functionTypes)
        {
            FunctionTypes = functionTypes;
        }

        public void Add(string prototypeString)
        {
            var prototype = Parse(prototypeString);

            var demarcation = FunctionTypes switch
            {
                WikiFunctionType.Standard => "##",
                WikiFunctionType.Scoped => "$$",
                WikiFunctionType.Instruction => "@@",
                _ => string.Empty,
            };

            Items.Add(new PrototypeSet()
            {
                Demarcation = demarcation,
                ProperName = prototype.ProperName,
                FunctionName = prototype.ProperName.ToLower(),
                Value = prototype
            });
        }

        public bool Exists(string functionDemarcation, string functionName)
        {
            functionName = functionName.ToLower();

            //$$ are scope functions and are not called by demarcation, we only have demarcations to make it easier to parse
            //  the functions in the wikiText and scope functions are easy enough since they start with curly braces.
            return Items.Any(o => (o.Demarcation == functionDemarcation || o.Demarcation == "$$") && o.FunctionName == functionName);
        }

        public FunctionPrototype Get(string functionDemarcation, string functionName)
        {
            functionName = functionName.ToLower();

            //$$ are scope functions and are not called by demarcation, we only have demarcations to make it easier to parse
            //  the functions in the wikiText and scope functions are easy enough since they start with curly braces.
            var functionPrototype = Items.Where(o => (o.Demarcation == functionDemarcation || o.Demarcation == "$$") && o.FunctionName == functionName).FirstOrDefault()?.Value;

            return functionPrototype
                ?? throw new WikiFunctionPrototypeNotDefinedException($"Function ({functionName}) does not have a defined prototype.");
        }

        private static FunctionPrototype Parse(string prototypeString)
        {
            int index = 0;

            //Get function name.
            var token = GetNextToken(prototypeString, ref index);
            if (prototypeString[index] != '(')
            {
                throw new Exception($"Unexpected token '{prototypeString[index]}' found  when parsing: [{prototypeString}]");
            }
            index++; //Skip the opening parenthesis.
            SkipWhiteSpace(prototypeString, ref index);

            var prototype = new FunctionPrototype(token);

            while (true)
            {
                if (prototypeString[index] == ')') //Found end of parameter list?
                {
                    index++; //Skip the closing parenthesis.
                    SkipWhiteSpace(prototypeString, ref index);

                    if (index != prototypeString.Length)
                    {
                        throw new Exception($"Unexpected token ')' found when parsing [{prototype.ProperName}].");
                    }
                    return prototype; //Function with no parameters.
                }

                //Get parameter type.
                token = GetNextToken(prototypeString, ref index);
                if (string.IsNullOrEmpty(token))
                {
                    throw new Exception($"Unexpected empty token when parsing [{prototype.ProperName}].");
                }

                //Parse parameter type.
                if (Enum.TryParse<WikiFunctionParamType>(token, true, out var parsedParamType)
                    && Enum.IsDefined(parsedParamType) && int.TryParse(token, out _) == false)
                {
                    //Get parameter name.
                    string parameterName = GetNextToken(prototypeString, ref index);

                    var prototypeParameter = new PrototypeParameter(parsedParamType, parameterName);
                    if (prototypeParameter.IsInfinite && prototype.Parameters.Any(o => o.IsInfinite))
                    {
                        throw new Exception($"Function [{prototype.ProperName}] cannot contain more than one infinite parameter.");
                    }

                    #region Parse valid values list.

                    if (prototypeString[index] == '[')
                    {
                        index++; //Skip the opening bracket.
                        SkipWhiteSpace(prototypeString, ref index);

                        while (true)
                        {
                            var literalValue = ParseLiteral(prototypeString, ref index)
                                ?? throw new Exception($"Null values are not allowed in allowable value list for: [{prototype.ProperName}].");

                            prototypeParameter.AllowedValues.Add(literalValue);

                            if (prototypeString[index] == ',')
                            {
                                //All good, we still have literals to parse.
                                index++; //Skip the comma.
                                SkipWhiteSpace(prototypeString, ref index);
                            }
                            else if (prototypeString[index] == ']')
                            {
                                //Found the end of the allowable values list.
                                index++; //Skip the closing bracket.
                                SkipWhiteSpace(prototypeString, ref index);
                                break;
                            }
                            else
                            {
                                throw new Exception($"Unexpected token '{prototypeString[index]}' found at index {index:n0} when parsing [{prototype.ProperName}].");
                            }
                        }
                    }

                    #endregion

                    #region Parse any default values, these make a parameter optional.

                    SkipWhiteSpace(prototypeString, ref index);

                    if (prototypeString[index] == '=')
                    {
                        index++; //Skip the equal sign.
                        SkipWhiteSpace(prototypeString, ref index);

                        prototypeParameter.DefaultValue = ParseLiteral(prototypeString, ref index);
                        prototypeParameter.IsRequired = false;
                    }
                    else
                    {
                        if (prototype.Parameters.Any(o => o.IsRequired == false))
                        {
                            throw new Exception($"Unexpected required parameter '{prototypeParameter.Name}' found when parsing [{prototype.ProperName}], required parameters cannot proceed optional parameters.");
                        }

                        prototypeParameter.IsRequired = true;
                    }

                    #endregion

                    prototype.Parameters.Add(prototypeParameter);

                    SkipWhiteSpace(prototypeString, ref index);
                }
                else
                {
                    throw new Exception($"Unexpected parameter type '{token}' found at index {index:n0} when parsing [{prototype.ProperName}].");
                }

                if (index >= prototypeString.Length)
                {
                    throw new Exception($"Unexpected end of string found at index {index:n0} when parsing [{prototype.ProperName}].");
                }
                else if (prototypeString[index] == ',')
                {
                    //All good. We have remaining parameters to parse.
                    index++; //Skip the comma.
                    SkipWhiteSpace(prototypeString, ref index);
                }
                else if (prototypeString[index] == ')')
                {
                    //Found end of parameter list.

                    int foundIndex = index;
                    index++; //Skip the closing parenthesis.

                    SkipWhiteSpace(prototypeString, ref index);

                    if (index != prototypeString.Length)
                    {
                        throw new Exception($"Unexpected token ')' found at index {foundIndex:n0} when parsing [{prototype.ProperName}].");
                    }
                    break; //Parsing complete.
                }
            }

            return prototype;
        }

        /// <summary>
        /// Gets the next token in a string.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetNextToken(string str, ref int index)
        {
            var token = string.Empty;

            SkipWhiteSpace(str, ref index);

            for (; index < str.Length; index++)
            {
                if (!char.IsAsciiLetterOrDigit(str[index]))
                {
                    break;
                }

                token += str[index];
            }

            SkipWhiteSpace(str, ref index);

            return token;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void SkipWhiteSpace(string str, ref int index)
        {
            while (index < str.Length && char.IsWhiteSpace(str[index]))
            {
                index++;
            }
        }

        /// <summary>
        /// Parses a literal value which is either "NULL" or a string enclosed in 'single quotes'.
        /// In the case that the literal value needs to include a single quote, the backslash is
        /// used as an escape character. (e.g. 'John\'s Literal')
        /// </summary>
        private static string? ParseLiteral(string str, ref int index)
        {
            if (str.Substring(index).StartsWith("null", StringComparison.InvariantCultureIgnoreCase))
            {
                index += 4; //Skip the "null" string.
                return null; //Literal NULL value.
            }
            else if (str[index] == '\'')
            {
                index++; //Skip the opening single quote.

                var literal = new StringBuilder();

                while (index < str.Length)
                {
                    bool escapeCharacter = false;
                    if (str[index] == '\\')
                    {
                        if (index >= str.Length)
                        {
                            throw new Exception($"Unexpected escape sequence found at then of string at index {index:n0} when parsing prototype: \"{str}\"");
                        }

                        index++; //Skip the escape character denotation.
                        escapeCharacter = true;
                    }

                    if (escapeCharacter == false && str[index] == '\'')
                    {
                        index++; //Skip the closing single quote.
                        //Found end of literal string, return the value.
                        return literal.ToString();
                    }

                    literal.Append(str[index++]);
                }
            }
            else
            {
                throw new Exception($"Unexpected token '{str[index]}' found at index {index:n0} when parsing prototype: \"{str}\"");
            }
            throw new Exception($"Unexpected end of string found when parsing literal for prototype: \"{str}\"");
        }
    }
}
