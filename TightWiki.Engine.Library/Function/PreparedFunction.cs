using NTDLS.Helpers;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TightWiki.Engine.Library.Interfaces;

namespace TightWiki.Engine.Library.Function
{
    /// <summary>
    /// Contains information about an actual function call, its supplied parameters, and is matched with a defined function.
    /// </summary>
    public class PreparedFunction
    {
        /// <summary>
        /// The name of the function being called.
        /// </summary>
        public string Name { get; private set; }

        public TightEngineFunctionEnvelope Prototype { get; set; }

        /// <summary>
        /// The arguments supplied by the caller.
        /// </summary>
        public List<NamedParameter> Parameters { get; private set; } = new();

        public PreparedFunction(ITightEngineState state, TightEngineFunctionEnvelope prototype, List<string> args)
        {
            Prototype = prototype;
            Name = prototype.Method.Name;

            int givenArgIndex = 0;
            int prototypeArgIndex = 1; //We start at 1 because the first argument is always the state.

            if (prototype.Parameters.Count == 0)
            {
                throw new Exception($"Function [{Name}] was called with arguments, but the function does not accept any parameters.");
            }

            //The first parameter must always be the state, so we add it to the parameters list before processing the caller supplied arguments.
            var firstParam = prototype.Parameters.First();
            if (firstParam.ParameterType != typeof(ITightEngineState))
            {
                throw new Exception($"Function [{Name}] must have the first parameter of type ITightEngineState.");
            }
            Parameters.Add(new NamedParameter(firstParam.Name.EnsureNotNull(), state));

            for (; prototypeArgIndex < prototype.Parameters.Count && givenArgIndex < args.Count;)
            {
                var param = prototype.Parameters[prototypeArgIndex];

                if (TryGetArgPassedByName(args[givenArgIndex], out var _, out var _))
                {
                    //This argument was passed by name, so we now require all subsequent arguments to also be passed by name.
                    break;
                }

                var type = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
                if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                {
                    Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), ConvertArgumentValue(param, args[givenArgIndex])));
                    prototypeArgIndex++;
                    givenArgIndex++;
                }
                else if (type.IsArray)
                {
                    //Swallow up all remaining arguments and pass them as an array.
                    var arrayValues = new List<object?>();
                    for (; givenArgIndex < prototype.Parameters.Count;)
                    {
                        arrayValues.Add(ConvertArgumentValue(param, args[givenArgIndex]));
                        givenArgIndex++;
                    }
                    Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), arrayValues));
                    prototypeArgIndex++;
                }
            }

            if (givenArgIndex < prototype.Parameters.Count && givenArgIndex < args.Count)
            {
                //We still have arguments left to process, this can only mean that they were passed by name, so we need to process them as such.
                for (; prototypeArgIndex < prototype.Parameters.Count && givenArgIndex < args.Count;)
                {
                    var param = prototype.Parameters[prototypeArgIndex];

                    if (TryGetArgPassedByName(args[givenArgIndex], out var name, out var value))
                    {
                        if (string.Equals(name, param.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), args[givenArgIndex]));
                            givenArgIndex++;
                            prototypeArgIndex++;
                            continue;
                        }
                        else
                        {
                            if (param.HasDefaultValue)
                            {
                                Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), param.DefaultValue));
                                prototypeArgIndex++;
                                continue;
                            }
                            else
                            {
                                throw new Exception($"Function [{Name}], the argument [{name}] does not match any parameter of the function.");
                            }
                        }

                        //This argument was passed by name, so we now require all subsequent arguments to also be passed by name.
                    }
                    else if (param.ParameterType.IsArray)
                    {
                        //Swallow up all remaining arguments and pass them as an array.
                        var arrayValues = new List<object?>();
                        for (; givenArgIndex < prototype.Parameters.Count; givenArgIndex++)
                        {
                            arrayValues.Add(ConvertArgumentValue(param, args[givenArgIndex]));
                        }
                        Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), arrayValues));
                    }
                }
            }

            //Add the remaining parameters that were not supplied by the caller, if they have default values.
            for (; prototypeArgIndex < prototype.Parameters.Count;)
            {
                var param = prototype.Parameters[prototypeArgIndex];

                if (param.HasDefaultValue)
                {
                    Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), param.DefaultValue));
                    prototypeArgIndex++;
                    continue;
                }
                else
                {
                    throw new Exception($"Function [{Name}], the argument [{param.Name}] does not match any parameter of the function.");
                }
            }

            if (Parameters.Count != prototype.Parameters.Count)
            {
                throw new Exception($"Function [{Name}] was called with an incorrect number of arguments. Expected [{prototype.Parameters.Count}] but received [{Parameters.Count}].");
            }
        }

        private bool TryGetArgPassedByName(string arg, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out string? value)
        {
            if (arg.StartsWith(':') && arg.Contains('='))
            {
                var parsed = arg.Substring(1); //Skip the colon.
                int index = parsed.IndexOf('=');
                name = parsed.Substring(0, index).Trim().ToLowerInvariant();
                value = parsed.Substring(index + 1).Trim();
                return true;
            }

            name = null;
            value = null;
            return false;
        }

        public async Task<HandlerResult> Execute(ITightEngineState state)
        {
            var parameters = Parameters.Select(o => o.Value).ToArray();
            var result = ((Task<HandlerResult>?)Prototype.Method.Invoke(Prototype.EngineModule.Instance, parameters)).EnsureNotNull();
            return await result;
        }

        /// <summary>
        /// Checks the passed value against the function prototype to ensure that the variable is the correct type, value, etc.
        /// </summary>
        private object? ConvertArgumentValue(ParameterInfo param, string value)
        {
            var type = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
            if (type == typeof(string))
            {
                return value;
            }
            else if (type.IsPrimitive)
            {
                return ConvertPrimitave(param, value);
            }
            else if (type.IsEnum)
            {
                if (Enum.TryParse(param.ParameterType, value, ignoreCase: true, out var result))
                {
                    return result;
                }
                var allowedValues = Enum.GetNames(param.ParameterType);
                throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] is not allowed. Allowed values are [{string.Join(",", allowedValues)}].");
            }

            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to type [{param.ParameterType.Name}].");
        }

        private object ConvertPrimitave(ParameterInfo param, string value)
        {
            var type = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;

            switch (type)
            {
                #region Type value parsers.

                case Type t when t == typeof(bool):
                    {
                        if (bool.TryParse(value, out bool result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to boolean.");
                        }
                        return result;
                    }
                case Type t when t == typeof(int):
                    {
                        if (int.TryParse(value, out int result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        return result;
                    }
                case Type t when t == typeof(long):
                    {
                        if (long.TryParse(value, out long result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        return result;
                    }
                case Type t when t == typeof(short):
                    {
                        if (short.TryParse(value, out short result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        return result;
                    }

                case Type t when t == typeof(byte):
                    {
                        if (byte.TryParse(value, out byte result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        return result;
                    }
                case Type t when t == typeof(ulong):
                    {
                        if (ulong.TryParse(value, out ulong result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        return result;
                    }
                case Type t when t == typeof(ushort):
                    {
                        if (ushort.TryParse(value, out ushort result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        return result;
                    }
                case Type t when t == typeof(sbyte):
                    {
                        if (sbyte.TryParse(value, out sbyte result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        return result;
                    }
                case Type t when t == typeof(uint):
                    {
                        if (uint.TryParse(value, out uint result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to integer.");
                        }
                        return result;
                    }
                case Type t when t == typeof(double):
                    {
                        if (double.TryParse(value, out double result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to float.");
                        }
                        return result;
                    }
                case Type t when t == typeof(decimal):
                    {
                        if (decimal.TryParse(value, out decimal result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to float.");
                        }
                        return result;
                    }
                case Type t when t == typeof(float):
                    {
                        if (float.TryParse(value, out float result) == false)
                        {
                            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to float.");
                        }
                        return result;
                    }

                #endregion
            }

            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted [{param.ParameterType.Name}].");
        }
    }
}
