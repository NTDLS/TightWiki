using NTDLS.Helpers;
using System.Reflection;
using TightWiki.Engine.Library.Interfaces;

namespace TightWiki.Engine.Library.Function
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

        public TightEngineFunctionEnvelope Prototype { get; set; }

        /// <summary>
        /// The arguments supplied by the caller.
        /// </summary>
        public FunctionParameters Parameters { get; private set; }

        public FunctionCall(TightEngineFunctionEnvelope prototype, List<string> args)
        {
            Prototype = prototype;
            Parameters = new FunctionParameters(this);
            Name = prototype.Method.Name;

            foreach (var arg in args)
            {
                if (arg.StartsWith(':') && arg.Contains('='))
                {
                    var parsed = arg.Substring(1); //Skip the colon.
                    int index = parsed.IndexOf('=');
                    var name = parsed.Substring(0, index).Trim().ToLowerInvariant();
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
        public async Task<HandlerResult> Execute(ITightEngineState state)
        {
            var parameters = new List<object>() { state };

            var result = ((Task<HandlerResult>?)Prototype.Method.Invoke(Prototype.EngineModule.Instance, parameters.ToArray())).EnsureNotNull();
            return await result;
        }

        /// <summary>
        /// Checks the passed value against the function prototype to ensure that the variable is the correct type, value, etc.
        /// </summary>
        private void EnforcePrototypeParamValue(ParameterInfo param, string value)
        {
            var parameterType = param.ParameterType;

            if (parameterType.IsPrimitive)
            {
                switch (parameterType)
                {
                    #region Type value parsers.

                    case Type t when t == typeof(bool):
                        if (bool.TryParse(value, out bool _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to boolean.");
                        }
                        break;
                    case Type t when t == typeof(int):
                        if (int.TryParse(value, out int _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        break;
                    case Type t when t == typeof(long):
                        if (long.TryParse(value, out long _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        break;
                    case Type t when t == typeof(short):
                        if (short.TryParse(value, out short _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        break;
                    case Type t when t == typeof(byte):
                        if (byte.TryParse(value, out byte _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        break;
                    case Type t when t == typeof(ulong):
                        if (ulong.TryParse(value, out ulong _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        break;
                    case Type t when t == typeof(ushort):
                        if (ushort.TryParse(value, out ushort _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        break;
                    case Type t when t == typeof(sbyte):
                        if (sbyte.TryParse(value, out sbyte _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        break;
                    case Type t when t == typeof(uint):
                        if (uint.TryParse(value, out uint _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        break;
                    case Type t when t == typeof(double):
                        if (double.TryParse(value, out double _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to float.");
                        }
                        break;
                    case Type t when t == typeof(decimal):
                        if (decimal.TryParse(value, out decimal _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to float.");
                        }
                        break;
                    case Type t when t == typeof(float):
                        if (float.TryParse(value, out float _) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to float.");
                        }
                        break;

                    #endregion
                }
            }

            if (parameterType.IsEnum)
            {
                var allowedValues = Enum.GetNames(parameterType);

                if (allowedValues.Contains(value, StringComparer.InvariantCultureIgnoreCase) == false)
                {
                    throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] is not allowed. Allowed values are [{string.Join(",", allowedValues)}].");
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

                if (param.HasDefaultValue)
                {
                    break;
                }
                if (param.ParameterType.IsArray //It the parameter an array or List<>?
                    || (param.ParameterType.IsGenericType && param.ParameterType.GetGenericTypeDefinition() == typeof(List<>)))
                {
                    break;
                }

                if (Parameters.Ordinals.Count > index)
                {
                    //Good, we have a value.
                    string value = Parameters.Ordinals[index].Value;
                    Parameters.Ordinals[index].AssociateWithPrototypeParam(param.Name);
                    EnforcePrototypeParamValue(param, value.ToLowerInvariant());

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

                if (param.IsInfinite()) //Is the parameter an array or List<>?
                {
                    if (param.HasDefaultValue)
                    {
                        //Make sure we have at least one of these required infinite parameters passed.
                        if (Parameters.Ordinals.Count > index)
                        {
                            //Good, we have a value.
                            string value = Parameters.Ordinals[index].Value;
                            Parameters.Ordinals[index].AssociateWithPrototypeParam(param.Name);
                            EnforcePrototypeParamValue(param, value.ToLowerInvariant());
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
                        EnforcePrototypeParamValue(param, value.ToLowerInvariant());
                        namedToAddLater.Add(new NamedParameter(param.Name, value));
                    }

                    break;
                }

                if (param.HasDefaultValue)
                {
                    hasEncounteredOptionalParameter = true;
                }

                if (param.HasDefaultValue == false && hasEncounteredOptionalParameter)
                {
                    throw new Exception($"Function [{Name}], the required parameter [{param.Name}] was found after other optional parameters.");
                }
                else if (param.IsInfinite()) //Is the parameter an array or List<>?
                {
                    throw new Exception($"Function [{Name}], encountered an unexpected number of infinite parameters in prototype for [{param.Name}].");
                }

                if (Parameters.Ordinals.Count > index)
                {
                    string value = Parameters.Ordinals[index].Value;
                    Parameters.Ordinals[index].AssociateWithPrototypeParam(param.Name);
                    EnforcePrototypeParamValue(param, value.ToLowerInvariant());
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

            var nonInfiniteParams = Prototype.Parameters.Where(o => o.IsInfinite() == false).Select(o => o.Name.ToLowerInvariant());
            var groups = Parameters.Named.Where(o => nonInfiniteParams.Contains(o.Name.ToLowerInvariant())).GroupBy(o => o.Name.ToLowerInvariant()).Where(o => o.Count() > 1);

            if (groups.Any())
            {
                var group = groups.First();
                throw new Exception($"Function [{Name}], non-infinite parameter specified more than once: [{group.Key}].");
            }
        }
    }
}
