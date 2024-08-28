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

        public WikiFunctionType FunctionTypes { get; private set; }
        public List<PrototypeSet> Items { get; set; } = new();

        public FunctionPrototypeCollection(WikiFunctionType functionTypes)
        {
            FunctionTypes = functionTypes;
        }

        public void Add(string prototypeString)
        {
            var prototype = ParsePrototype(prototypeString);

            Items.Add(new PrototypeSet()
            {
                FunctionPrefix = prototype.FunctionPrefix,
                ProperName = prototype.ProperName,
                FunctionName = prototype.FunctionName.ToLower(),
                Value = prototype
            });
        }

        public bool Exists(string functionPrefix, string functionName)
        {
            functionName = functionName.ToLower();

            //$$ are scope functions and are not called by prefix, we only have prefixes to make it easier to parse
            //  the functions in the wikiText and scope functions are easy enough since they start with curly braces.
            return Items.Any(o => (o.FunctionPrefix == functionPrefix || o.FunctionPrefix == "$$") && o.FunctionName == functionName);
        }

        public FunctionPrototype Get(string functionPrefix, string functionName)
        {
            functionName = functionName.ToLower();

            //$$ are scope functions and are not called by prefix, we only have prefixes to make it easier to parse
            //  the functions in the wikiText and scope functions are easy enough since they start with curly braces.
            var functionPrototype = Items.Where(o => (o.FunctionPrefix == functionPrefix || o.FunctionPrefix == "$$") && o.FunctionName == functionName).FirstOrDefault()?.Value;

            return functionPrototype
                ?? throw new WikiFunctionPrototypeNotDefinedException($"Function ({functionName}) does not have a defined prototype.");
        }

        private FunctionPrototype ParsePrototype(string prototypeString)
        {
            int nameStartIndex = prototypeString.TakeWhile(c => char.IsLetterOrDigit(c) == false).Count();
            int nameEndIndex = prototypeString.IndexOf(':');
            string properName = prototypeString.Substring(nameStartIndex, nameEndIndex - nameStartIndex).Trim();
            string functionName = properName.ToLower();
            string functionPrefix = prototypeString.Substring(0, nameStartIndex).Trim();

            prototypeString = prototypeString.Substring(nameEndIndex + 1).Trim();

            var prototype = new FunctionPrototype() { FunctionPrefix = functionPrefix, ProperName = properName, FunctionName = functionName };

            if (prototypeString.Length == 0)
            {
                //No parameters.
                return prototype;
            }

            var segments = prototypeString.Trim().Split('|').Select(o => o.Trim());

            foreach (var segment in segments)
            {
                var prototypeSegment = new PrototypeParameter();

                int index = 0;

                if (segment[index] == '<')
                {
                    index++; //Skip the '<'
                    prototypeSegment.Type = Tok(segment, ref index);
                    index++; //Skip the '>'

                    if (prototypeSegment.Type.Contains(':'))
                    {
                        var splitSeg = prototypeSegment.Type.Split(':');
                        prototypeSegment.Type = splitSeg[0];
                        if (splitSeg[1].Equals("infinite", StringComparison.InvariantCultureIgnoreCase))
                        {
                            prototypeSegment.IsInfinite = true;
                            if (prototype.Parameters.Any(o => o.IsInfinite))
                            {
                                throw new Exception($"Function [{functionName}], prototype error: cannot contain more than one [infinite] parameter.");
                            }
                        }
                        else
                        {
                            throw new Exception($"Function [{functionName}], prototype error: expected [infinite] got [{splitSeg[1]}].");
                        }
                    }

                    SkipWhiteSpace(segment, ref index);

                    if (index < segment.Length && segment[index] == '{' || segment[index] == '[')
                    {
                        if (index < segment.Length && segment[index] == '[')
                        {
                            prototypeSegment.IsRequired = true;
                        }

                        index++; //Skip the '[' or '{'

                        prototypeSegment.Name = Tok(segment, ref index);

                        if (index < segment.Length && segment[index] == '(') //Parse allowed values.
                        {
                            int allowedValueEndIndex = segment.IndexOf(')', index);
                            string roteRequiredValues = segment.Substring(index + 1, allowedValueEndIndex - index - 1);
                            prototypeSegment.AllowedValues = roteRequiredValues.Trim().Split(',').Select(o => o.Trim().ToLower()).ToList();

                            index = allowedValueEndIndex;
                            index++; //Skip the ')'
                            SkipWhiteSpace(segment, ref index);
                        }

                        index++; //Skip the ']' or '}'
                    }
                    else
                    {
                        throw new Exception($"Function [{functionName}], prototype error: expected [{{] or [[].");
                    }

                    SkipWhiteSpace(segment, ref index);

                    if (index < segment.Length && segment[index] == '=')
                    {
                        index++; //Skip the '='
                        SkipWhiteSpace(segment, ref index);

                        if (segment[index] != '\'')
                        {
                            throw new Exception($"Function [{functionName}], prototype error: expected [\'].");
                        }

                        index++; //Skip the '''

                        prototypeSegment.DefaultValue = segment.Substring(index, (segment.Length - index) - 1);

                        index = segment.Length - 1;

                        if (index < segment.Length && segment[index] != '\'')
                        {
                            throw new Exception($"Function [{functionName}], prototype error: expected [\'].");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Function [{functionName}], prototype error: expected [<].");
                }

                prototype.Parameters.Add(prototypeSegment);
            }

            return prototype;
        }

        /// <summary>
        /// Gets the next token in a string.
        /// </summary>
        /// <param name="str"></param>
        /// <param name="index"></param>
        /// <returns></returns>
        private string Tok(string str, ref int index)
        {
            var token = string.Empty;

            SkipWhiteSpace(str, ref index);

            for (; index < str.Length; index++)
            {
                if ("<>{}[]()".Contains(str[index]))
                {
                    break;
                }

                token += str[index];
            }

            SkipWhiteSpace(str, ref index);

            return token;
        }

        private static void SkipWhiteSpace(string str, ref int index)
        {
            while (index < str.Length && char.IsWhiteSpace(str[index]))
            {
                index++;
            }
        }
    }
}
