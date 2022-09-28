using System;
using System.Collections.Generic;
using System.Linq;

namespace AsapWiki.Shared.Classes
{
    public class MethodCallInstance
    {
        /// <summary>
        /// The name of the method being called.
        /// </summary>
        public string Name { get; private set; }
        public MethodPrototype Prototype { get; set; }
        /// <summary>
        /// The arguments supplied by the caller.
        /// </summary>
        public MethodParameters Parameters { get; private set; }

        public MethodCallInstance(MethodPrototype prototype, List<string> args)
        {
            Prototype = prototype;
            Parameters = new MethodParameters(this);
            Name = prototype.MethodName;

            foreach (var arg in args)
            {
                if (arg.StartsWith(":") && arg.Contains("="))
                {
                    var parsed = arg.Substring(1); //Skip the colon.
                    int index = parsed.IndexOf("=");
                    var name = parsed.Substring(0, index).Trim().ToLower();
                    var value = parsed.Substring(index + 1).Trim();

                    Parameters.Named.Add(new NamedParam(name, value));
                }
                else
                {
                    Parameters.Ordinals.Add(arg);
                }
            }

            ApplyPrototype();
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
            for (; index < Prototype.Parameters.Count; index++)
            {
                var param = Prototype.Parameters[index];

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
            for (; index < Prototype.Parameters.Count; index++)
            {
                var param = Prototype.Parameters[index];

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
                var param = Prototype.Parameters.Where(o => o.Name.ToLower() == named.Name.ToLower()).FirstOrDefault();
                if (param == null)
                {
                    throw new Exception($"Passed named parameter ({named.Name}) is not defined in the prototype for ({Name}).");
                }

                EnforcePrototypeParamValue(param, named.Value);
            }

            Parameters.Named.AddRange(namedToAddLater);
        }

    }
}
