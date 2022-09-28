using System;
using System.Collections.Generic;
using System.Linq;

namespace AsapWiki.Shared.Classes
{
    public class MethodCallInfo
    {
        /// <summary>
        /// The name of the method being called.
        /// </summary>
        public string Name { get; private set; }
        public List<PrototypeParameter> PrototypeParameters { get; private set; }

        public MethodParameters Parameters { get; private set; }

        public MethodCallInfo()
        {
            Parameters = new MethodParameters(this);
        }

        public static MethodCallInfo CreateInstance(List<string> args, string prototype)
        {
            return CreateInstance(args.ToArray(), prototype);
        }

        public static MethodCallInfo CreateInstance(string[] args, string prototype)
        {
            var set = new MethodCallInfo();

            foreach (var arg in args)
            {
                if (arg.StartsWith(":") && arg.Contains("="))
                {
                    var parsed = arg.Substring(1); //Skip the colon.

                    int index = parsed.IndexOf("=");

                    var name = parsed.Substring(0, index).Trim().ToLower();
                    var value = parsed.Substring(index + 1).Trim();

                    set.Parameters.Named.Add(new NamedParam(name, value));
                }
                else
                {
                    set.Parameters.Ordinals.Add(arg);
                }
            }

            set.ParsePrototype(prototype);

            return set;
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

        /// <summary>
        /// Checks the passed value agains the method prototype to ensure that the variable is the correct type, value, etc.
        /// </summary>
        /// <param name="segment"></param>
        /// <param name="value"></param>
        /// <exception cref="Exception"></exception>
        private void EnforcePrototypeParamValue(PrototypeParameter segment, string value)
        {
            if (segment.Type == "bool")
            {
                if (bool.TryParse(value, out bool val) == false)
                {
                    throw new Exception($"Value ({value}) passed to parameter ({segment.Name}) could not be converted to boolean.");
                }
            }
            if (segment.Type == "int")
            {
                if (int.TryParse(value, out int val) == false)
                {
                    throw new Exception($"Value ({value}) passed to parameter ({segment.Name}) could not be converted to integer.");
                }
            }
            else if (segment.Type == "float")
            {
                if (double.TryParse(value, out double val) == false)
                {
                    throw new Exception($"Value ({value}) passed to parameter ({segment.Name}) could not be converted to float.");
                }
            }

            if (segment.AllowedValues != null && segment.AllowedValues.Count > 0)
            {
                if (segment.AllowedValues.Contains(value.ToLower()) == false)
                {
                    throw new Exception($"Value ({value}) passed to parameter ({segment.Name}) is not allowed. Allowed values are {string.Join(",", segment.AllowedValues)}");
                }
            }
        }

        /// <summary>
        /// Rolls through the supplied arguments and applies them to the prototype. Also identifies which supplied arguments are associated with each 
        /// prototype argument and adds the ordinal based arguments to the name based collection. Ensures that each argument conforms with the prototype.
        /// </summary>
        /// <exception cref="Exception"></exception>
        private void ApplyPrototype()
        {
            int index = 0;

            //Keep a list of the arguments as they are associated with the prototype so that we can leter referecne them by name.
            var namedToAddLater = new List<NamedParam>();

            //Hanldle non-infinite ordinal based required parameters:
            for (; index < PrototypeParameters.Count; index++)
            {
                var param = PrototypeParameters[index];

                if (param.IsRequired == false)
                {
                    break;
                }
                if (param.IsInfinite == true)
                {
                    break;
                }

                if (Parameters.Ordinals.Count > index)
                {
                    //Good, we have a value.
                    string value = Parameters.Ordinals[index];
                    EnforcePrototypeParamValue(param, value.ToLower());

                    namedToAddLater.Add(new NamedParam(param.Name, value));
                }
                else
                {
                    throw new Exception($"Required parameter ({param.Name}) was not passed. Keep in mind that required parameters are always the first parameters and cannot be passed by name.");
                }
            }

            bool hasEncounteredOptionalParameter = false;

            //Hanlde remaining optional parameters:
            for (; index < PrototypeParameters.Count; index++)
            {
                var param = PrototypeParameters[index];

                if (param.IsInfinite == true)
                {
                    if (param.IsRequired == true)
                    {
                        //Make sure we have at least one of these required infinite parameters passed.
                        if (Parameters.Ordinals.Count > index)
                        {
                            //Good, we have a value.
                            string value = Parameters.Ordinals[index];
                            EnforcePrototypeParamValue(param, value.ToLower());
                        }
                        else
                        {
                            throw new Exception($"Required infinite parameter ({param.Name}) was not passed. Keep in mind that required parameters are always the first parameters and cannot be passed by name.");
                        }
                    }

                    //Now that we have encountered an infinite parameter, it will swallow up all other ordnial based arguments. Might as well check the types and exit the loop.
                    for (; index < Parameters.Ordinals.Count; index++)
                    {
                        string value = Parameters.Ordinals[index];
                        EnforcePrototypeParamValue(param, value.ToLower());
                        namedToAddLater.Add(new NamedParam(param.Name, value));
                    }

                    break;
                }

                if (param.IsRequired == false)
                {
                    hasEncounteredOptionalParameter = true;
                }

                if (param.IsRequired == true && hasEncounteredOptionalParameter)
                {
                    throw new Exception($"Required parameter ({param.Name}) found after other optional parameters.");
                }
                else if (param.IsInfinite == true)
                {
                    throw new Exception($"Encountered an unexpected number of infinite parameters for ({param.Name}).");
                }

                if (Parameters.Ordinals.Count > index)
                {
                    string value = Parameters.Ordinals[index];
                    EnforcePrototypeParamValue(param, value.ToLower());
                    namedToAddLater.Add(new NamedParam(param.Name, value));
                }
            }

            foreach (var named in Parameters.Named)
            {
                var param = PrototypeParameters.Where(o => o.Name.ToLower() == named.Name.ToLower()).FirstOrDefault();
                if (param == null)
                {
                    throw new Exception($"Passed named parameter ({named.Name}) is not defined in the prototype for ({Name}).");
                }

                EnforcePrototypeParamValue(param, named.Value);
            }

            Parameters.Named.AddRange(namedToAddLater);
        }

        /// <summary>
        /// Parses a ASAPWiki method protype string.
        /// </summary>
        /// <param name="prototype"></param>
        /// <exception cref="Exception"></exception>
        private void ParsePrototype(string prototype)
        {
            int nameEndIndex = prototype.IndexOf(':');

            Name = prototype.Substring(0, nameEndIndex).Trim();
            prototype = prototype.Substring(nameEndIndex + 1).Trim();
            PrototypeParameters = new List<PrototypeParameter>();

            if (prototype.Length == 0)
            {
                //No parameters.
                return;
            }

            var segments = prototype.Trim().Split('|').Select(o => o.Trim());

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
                        var segs = prototypeSegment.Type.Split(':');
                        prototypeSegment.Type = segs[0];
                        if (segs[1].ToLower() == "infinite")
                        {
                            prototypeSegment.IsInfinite = true;
                        }
                        else
                        {
                            throw new Exception($"Parser error, expected 'infinite' got '{segs[1]}' for {Name}");
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
                        throw new Exception("Parser error, expected '{' or '[' for {FunctionName}");
                    }

                    SkipWhiteSpace(segment, ref index);

                    if (index < segment.Length && segment[index] == '=')
                    {
                        index++; //Skip the '='
                        SkipWhiteSpace(segment, ref index);

                        if (segment[index] != '\'')
                        {
                            throw new Exception("Parser error, expected '\'' for {FunctionName}");
                        }

                        index++; //Skip the '''

                        prototypeSegment.DefaultValue = segment.Substring(index, (segment.Length - index) - 1);

                        index = segment.Length - 1;

                        if (index < segment.Length && segment[index] != '\'')
                        {
                            throw new Exception("Parser error, expected '\'' for {FunctionName}");
                        }
                    }
                }
                else
                {
                    throw new Exception($"Parser error, expected '<' for {Name}");
                }

                PrototypeParameters.Add(prototypeSegment);
            }

            ApplyPrototype();
        }
    }
}
