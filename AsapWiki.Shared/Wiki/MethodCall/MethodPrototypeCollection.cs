using System;
using System.Collections.Generic;
using System.Linq;

namespace AsapWiki.Shared.Wiki.MethodCall
{
    public class MethodPrototypeCollection
    {
        private class PrototypeSet
        {
            public string MethodName { get; set; }
            public MethodPrototype Value { get; set; }
        }

        private List<PrototypeSet> Items { get; set; } = new List<PrototypeSet>();

        public void Add(string prototypeString)
        {
            var prototype = ParsePrototype(prototypeString);

            Items.Add(new PrototypeSet()
            {
                MethodName = prototype.MethodName.ToLower(),
                Value = prototype
            });
        }

        public MethodPrototype Get(string methodName)
        {
            methodName = methodName.ToLower();

            return Items.Where(o => o.MethodName == methodName).FirstOrDefault()?.Value;
        }

        private MethodPrototype ParsePrototype(string prototypeString)
        {
            int nameEndIndex = prototypeString.IndexOf(':');
            string methodName = prototypeString.Substring(0, nameEndIndex).Trim().ToLower();
            prototypeString = prototypeString.Substring(nameEndIndex + 1).Trim();

            var prototype = new MethodPrototype() { MethodName = methodName };

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
                        if (splitSeg[1].ToLower() == "infinite")
                        {
                            prototypeSegment.IsInfinite = true;
                            if (prototype.Parameters.Any(o => o.IsInfinite))
                            {
                                throw new Exception($"Method [{methodName}], prototype error: cannot contain more than one [infinite] parameter.");
                            }
                        }
                        else
                        {
                            throw new Exception($"Method [{methodName}], prototype error: expected [infinite] got [{splitSeg[1]}].");
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
                            string roteRequiredValues = segment.Substring(index + 1, (allowedValueEndIndex - index) - 1);
                            prototypeSegment.AllowedValues = roteRequiredValues.Trim().Split(',').Select(o => o.Trim().ToLower()).ToList();

                            index = allowedValueEndIndex;
                            index++; //Skip the ')'
                            SkipWhiteSpace(segment, ref index);
                        }

                        index++; //Skip the ']' or '}'
                    }
                    else
                    {
                        throw new Exception($"Method [{methodName}], prototype error: expected [{{] or [[].");
                    }

                    SkipWhiteSpace(segment, ref index);

                    if (index < segment.Length && segment[index] == '=')
                    {
                        index++; //Skip the '='
                        SkipWhiteSpace(segment, ref index);

                        if (segment[index] != '\'')
                        {
                            throw new Exception($"Method [{methodName}], prototype error: expected [\'].");
                        }

                        index++; //Skip the '''

                        prototypeSegment.DefaultValue = segment.Substring(index, (segment.Length - index) - 1);

                        index = segment.Length - 1;

                        if (index < segment.Length && segment[index] != '\'')
                        {
                            throw new Exception($"Method [{methodName}], prototype error: expected [\'].");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Method [{methodName}], prototype error: expected [<].");
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

            string allowed = "<>{}[]()";

            for (; index < str.Length; index++)
            {
                if (allowed.Contains(str[index]))
                {
                    break;
                }

                token += str[index];
            }

            SkipWhiteSpace(str, ref index);

            return token;
        }

        private void SkipWhiteSpace(string str, ref int index)
        {
            while (index < str.Length && char.IsWhiteSpace(str[index]))
            {
                index++;
            }
        }
    }
}
