using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AsapWiki.Shared.Classes
{
    public class MethodCallInfo
    {
        public string FunctionName { get; private set; }
        /// <summary>
        /// Variables set by ordinal.
        /// </summary>
        public List<string> Ordinals { get; set; } = new List<string>();
        /// <summary>
        /// Variables set by name.
        /// </summary>
        public List<NamedParam> Named { get; private set; } = new List<NamedParam>();
        public List<PrototypeSegment> PrototypeSegments { get; private set; }

        public static MethodCallInfo CreateInstance(List<string> args, string prototype)
        {
            return CreateInstance(args.ToArray(), prototype);
        }

        public static MethodCallInfo CreateInstance(string []args, string prototype)
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

                    set.Named.Add(new NamedParam(name, value));
                }
                else
                {
                    set.Ordinals.Add(arg);
                }
            }

            set.ApplyPrototype(prototype);

            return set;
        }


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

        private void EnforcePrototypeParamValue(PrototypeSegment segment, string value)
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

        private void EnforcePrototype()
        {
            int index = 0;

            //Keep a list of the arguments as they are associated with the prototype so that we can leter referecne them by name.
            var namedToAddLater = new List<NamedParam>();

            //Hanldle non-infinite ordinal based required parameters:
            for (; index < PrototypeSegments.Count; index++)
            {
                var seg = PrototypeSegments[index];

                if (seg.IsRequired == false)
                {
                    break;
                }
                if (seg.IsInfinite == true)
                {
                    break;
                }

                if (Ordinals.Count > index)
                {
                    //Good, we have a value.
                    string value = Ordinals[index].ToLower();
                    EnforcePrototypeParamValue(seg, value);

                    namedToAddLater.Add(new NamedParam(seg.Name, value));
                }
                else
                {
                    throw new Exception($"Required parameter ({seg.Name}) was not passed. Keep in mind that required parameters are always the first parameters and cannot be passed by name.");
                }
            }

            bool hasEncounteredOptionalParameter = false;

            //Hanlde remaining optional parameters:
            for (; index < PrototypeSegments.Count; index++)
            {
                var seg = PrototypeSegments[index];

                if (seg.IsInfinite == true)
                {
                    if (seg.IsRequired == true)
                    {
                        //Make sure we have at least one of these required infinite parameters passed.
                        if (Ordinals.Count > index)
                        {
                            //Good, we have a value.
                            string value = Ordinals[index].ToLower();
                            EnforcePrototypeParamValue(seg, value);
                        }
                        else
                        {
                            throw new Exception($"Required infinite parameter ({seg.Name}) was not passed. Keep in mind that required parameters are always the first parameters and cannot be passed by name.");
                        }
                    }

                    //Now that we have encountered an infinite parameter, it will swallow up all other ordnial based arguments. Might as well check the types and exit the loop.
                    for (; index < Ordinals.Count; index++)
                    {
                        string value = Ordinals[index].ToLower();
                        EnforcePrototypeParamValue(seg, value);
                        namedToAddLater.Add(new NamedParam(seg.Name, value));
                    }

                    break;
                }

                if (seg.IsRequired == false)
                {
                    hasEncounteredOptionalParameter = true;
                }

                if (seg.IsRequired == true && hasEncounteredOptionalParameter)
                {
                    throw new Exception($"Required parameter ({seg.Name}) found after other optional parameters.");
                }
                else if (seg.IsInfinite == true)
                {
                    throw new Exception($"Encountered an unexpected number of infinite parameters for ({seg.Name}).");
                }

                if (Ordinals.Count > index)
                {
                    string value = Ordinals[index].ToLower();
                    EnforcePrototypeParamValue(seg, value);
                    namedToAddLater.Add(new NamedParam(seg.Name, value));
                }
            }

            foreach (var named in Named)
            {
                var seg = PrototypeSegments.Where(o => o.Name.ToLower() == named.Name.ToLower()).FirstOrDefault();
                if (seg == null)
                {
                    throw new Exception($"Passed named parameter ({named.Name}) is not defined in the prototype for ({FunctionName}).");
                }

                EnforcePrototypeParamValue(seg, named.Value);
            }

            Named.AddRange(namedToAddLater);
        }

        private void ApplyPrototype(string prototype)
        {
            int nameEndIndex = prototype.IndexOf(':');

            FunctionName = prototype.Substring(0, nameEndIndex).Trim();
            prototype = prototype.Substring(nameEndIndex + 1).Trim();
            PrototypeSegments = new List<PrototypeSegment>();

            if (prototype.Length == 0)
            {
                //No parameters.
                return;
            }

            //<type> //Only supports string, int, float, bool and string:infinite, int:infinite, and float:infinite.
            //{Optional (allowed|values) }='Default Value' or [Required (allowed|values) ]='Default Value'
            //All required parameters must come before the optional parameters.

            //... indicates infinite parameters, these should typically come after required parameters but
            //  can come before optional parameters as long as the optional parameters are passed by name.

            //prototype = "<string>[boxType(bullets,bullets-ordered)] | <string>{title}='' | ...";

            var segments = prototype.Trim().Split('|').Select(o => o.Trim());

            foreach (var segment in segments)
            {
                var prototypeSegment = new PrototypeSegment();

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
                            throw new Exception($"Parser error, expected 'infinite' got '{segs[1]}' for {FunctionName}");
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
                    throw new Exception($"Parser error, expected '<' for {FunctionName}");
                }

                PrototypeSegments.Add(prototypeSegment);
            }

            EnforcePrototype();
        }

        public List<string> GetStringList(string name)
        {
            name = name.ToLower();
            return Named.Where(o => o.Name.ToLower() == name)?.Select(o=>o.Value)?.ToList();
        }

        public string GetString(string name)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name.ToLower() == name).FirstOrDefault()?.Value;
            if (value == null)
            {
                var prototype = PrototypeSegments.Where(o => o.Name.ToLower() == name).First();
                value = prototype.DefaultValue;
            }

            return value;
        }

        public bool GetBool(string name)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name.ToLower() == name).FirstOrDefault()?.Value;
            if (value != null)
            {
                if (bool.TryParse(value, out bool parsed))
                {
                    return parsed;
                }
            }

            var prototype = PrototypeSegments.Where(o => o.Name.ToLower() == name).First();
            return bool.Parse(prototype.DefaultValue);
        }

        public int GetInt(string name)
        {
            name = name.ToLower();
            var value = Named.Where(o => o.Name.ToLower() == name).FirstOrDefault()?.Value;
            if (value != null)
            {
                if (int.TryParse(value, out int parsed))
                {
                    return parsed;
                }
            }

            var prototype = PrototypeSegments.Where(o => o.Name.ToLower() == name).First();
            return int.Parse(prototype.DefaultValue);
        }

        public List<int> GetIntList(string name)
        {
            var intList = new List<int>();
            var stringList = GetStringList(name);
            foreach (var s in stringList)
            {
                if (int.TryParse(s, out int parsed))
                {
                    intList.Add(parsed);
                }
            }
            return intList;
        }
    }
}

