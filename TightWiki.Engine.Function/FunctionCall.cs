using static TightWiki.Engine.Function.FunctionConstants;

namespace TightWiki.Engine.Function
{
    /// <summary>
    /// Contains information about an actual function call, its supplied parameters, and is matched with a defined function.
    /// </summary>
    public class FunctionCall
    {
        /// <summary>
        /// The name of the function being called.
        /// </summary>
        public string Name { get; private set; }

        public FunctionPrototype Prototype { get; set; }

        /// <summary>
        /// The arguments supplied by the caller.
        /// </summary>
        public FunctionParameters Parameters { get; private set; }

        public FunctionCall(FunctionPrototype prototype, List<string> args)
        {
            Prototype = prototype;
            Parameters = new FunctionParameters(this);
            Name = prototype.Key;

            foreach (var arg in args)
            {
                if (arg.StartsWith(':') && arg.Contains('='))
                {
                    var parsed = arg.Substring(1); //Skip the colon.
                    int index = parsed.IndexOf('=');
                    var name = parsed.Substring(0, index).Trim().ToLower();
                    var value = parsed.Substring(index + 1).Trim();

                    Parameters.Named.Add(new NamedParameter(name, value));
                }
                else
                {
                    Parameters.Ordinals.Add(new OrdinalParameter(arg));
                }
            }

            ApplyPrototype();
        }

        /// <summary>
        /// Checks the passed value against the function prototype to ensure that the variable is the correct type, value, etc.
        /// </summary>
        private void EnforcePrototypeParamValue(PrototypeParameter param, string value)
        {
            if (param.Type == WikiFunctionParamType.Boolean)
            {
                if (bool.TryParse(value, out bool _) == false)
                {
                    throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to boolean.");
                }
            }
            if (param.Type == WikiFunctionParamType.Integer)
            {
                if (int.TryParse(value, out int _) == false)
                {
                    throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                }
            }
            else if (param.Type == WikiFunctionParamType.Double)
            {
                if (double.TryParse(value, out double _) == false)
                {
                    throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to float.");
                }
            }

            if (param.AllowedValues != null && param.AllowedValues.Count > 0)
            {
                if (param.AllowedValues.Contains(value.ToLower()) == false)
                {
                    throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] is not allowed. Allowed values are [{string.Join(",", param.AllowedValues)}].");
                }
            }
        }

        /// <summary>
        /// Rolls through the supplied arguments and applies them to the prototype. Also identifies which supplied arguments are associated with each 
        /// prototype argument and adds the ordinal based arguments to the name based collection. Ensures that each argument conforms with the prototype.
        /// </summary>
        private void ApplyPrototype()
        {
            int index = 0;

            //Keep a list of the arguments as they are associated with the prototype so that we can later reference them by name.
            var namedToAddLater = new List<NamedParameter>();

            //Handle non-infinite ordinal based required parameters:
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
                    string value = Parameters.Ordinals[index].Value;
                    Parameters.Ordinals[index].AssociateWithPrototypeParam(param.Name);
                    EnforcePrototypeParamValue(param, value.ToLower());

                    namedToAddLater.Add(new NamedParameter(param.Name, value));
                }
                else
                {
                    throw new Exception($"Function [{Name}], the required parameter [{param.Name}] was not specified.");
                }
            }

            bool hasEncounteredOptionalParameter = false;

            //Handle remaining optional parameters:
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
                            string value = Parameters.Ordinals[index].Value;
                            Parameters.Ordinals[index].AssociateWithPrototypeParam(param.Name);
                            EnforcePrototypeParamValue(param, value.ToLower());
                        }
                        else
                        {
                            throw new Exception($"Function [{Name}], the required infinite parameter [{param.Name}] was not passed.");
                        }
                    }

                    //Now that we have encountered an infinite parameter, it will swallow up all other ordinal based arguments. Might as well check the types and exit the loop.
                    for (; index < Parameters.Ordinals.Count; index++)
                    {
                        string value = Parameters.Ordinals[index].Value;
                        Parameters.Ordinals[index].AssociateWithPrototypeParam(param.Name);
                        EnforcePrototypeParamValue(param, value.ToLower());
                        namedToAddLater.Add(new NamedParameter(param.Name, value));
                    }

                    break;
                }

                if (param.IsRequired == false)
                {
                    hasEncounteredOptionalParameter = true;
                }

                if (param.IsRequired == true && hasEncounteredOptionalParameter)
                {
                    throw new Exception($"Function [{Name}], the required parameter [{param.Name}] was found after other optional parameters.");
                }
                else if (param.IsInfinite == true)
                {
                    throw new Exception($"Function [{Name}], encountered an unexpected number of infinite parameters in prototype for [{param.Name}].");
                }

                if (Parameters.Ordinals.Count > index)
                {
                    string value = Parameters.Ordinals[index].Value;
                    Parameters.Ordinals[index].AssociateWithPrototypeParam(param.Name);
                    EnforcePrototypeParamValue(param, value.ToLower());
                    namedToAddLater.Add(new NamedParameter(param.Name, value));
                }
            }

            foreach (var named in Parameters.Named)
            {
                var param = Prototype.Parameters.Where(o => o.Name.Equals(named.Name, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault()
                    ?? throw new Exception($"Function [{Name}], the named parameter [{named.Name}] is not defined in the function prototype.");

                EnforcePrototypeParamValue(param, named.Value);
            }

            Parameters.Named.AddRange(namedToAddLater);

            var unmatchedParams = Parameters.Ordinals.Where(o => o.IsMatched == false).ToList();
            if (unmatchedParams.Count != 0)
            {
                throw new Exception($"Function [{Name}], unmatched parameter value [{unmatchedParams.First().Value}].");
            }

            var nonInfiniteParams = Prototype.Parameters.Where(o => o.IsInfinite == false).Select(o => o.Name.ToLower());
            var groups = Parameters.Named.Where(o => nonInfiniteParams.Contains(o.Name.ToLower())).GroupBy(o => o.Name.ToLower()).Where(o => o.Count() > 1);

            if (groups.Any())
            {
                var group = groups.First();
                throw new Exception($"Function [{Name}], non-infinite parameter specified more than once: [{group.Key}].");
            }
        }
    }
}
