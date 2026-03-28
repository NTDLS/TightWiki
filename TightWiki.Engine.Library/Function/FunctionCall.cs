using NTDLS.Helpers;
using System.Diagnostics.CodeAnalysis;
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
        public List<NamedParameter> Parameters { get; private set; } = new();

        public FunctionCall(TightEngineFunctionEnvelope prototype, List<string> args)
        {
            Prototype = prototype;
            Name = prototype.Method.Name;

            int argIndex = 0;

            for (; argIndex < prototype.Parameters.Count && argIndex < args.Count; argIndex++)
            {
                var param = prototype.Parameters[argIndex];

                if (TryGetArgPassedByName(args[argIndex], out var _, out var _))
                {
                    //This argument was passed by name, so we now require all subsequent arguments to also be passed by name.
                    break;
                }

                if (param.ParameterType.IsPrimitive || param.ParameterType.IsEnum)
                {
                    ConvertArgumentValue(param, args[argIndex]);
                    Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), args[argIndex]));
                }
                else if (param.ParameterType.IsArray)
                {
                    //Swallow up all remaining arguments and pass them as an array.
                    var arrayValues = new List<object?>();
                    for (; argIndex < prototype.Parameters.Count; argIndex++)
                    {
                        arrayValues.Add(ConvertArgumentValue(param, args[argIndex]));
                    }
                    Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), arrayValues));
                }
            }

            if (argIndex < prototype.Parameters.Count && argIndex < args.Count)
            {
                int paramIndex = argIndex;

                //We still have arguments left to process, this can only mean that they were passed by name, so we need to process them as such.
                for (; argIndex < prototype.Parameters.Count && argIndex < args.Count;)
                {
                    var param = prototype.Parameters[paramIndex];

                    if (TryGetArgPassedByName(args[argIndex], out var name, out var value))
                    {
                        if (string.Equals(name, param.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), args[argIndex]));
                            argIndex++;
                            paramIndex++;
                            continue;
                        }
                        else
                        {
                            if (param.HasDefaultValue)
                            {
                                Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), param.DefaultValue));
                                paramIndex++;
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
                        for (; argIndex < prototype.Parameters.Count; argIndex++)
                        {
                            arrayValues.Add(ConvertArgumentValue(param, args[argIndex]));
                        }
                        Parameters.Add(new NamedParameter(param.Name.EnsureNotNull(), arrayValues));
                    }
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
            var parameters = new List<object>() { state };

            //foreach (var arg in Parameters.Named)
            //{
            //}

            var result = ((Task<HandlerResult>?)Prototype.Method.Invoke(Prototype.EngineModule.Instance, parameters.ToArray())).EnsureNotNull();
            return await result;
        }

        /// <summary>
        /// Checks the passed value against the function prototype to ensure that the variable is the correct type, value, etc.
        /// </summary>
        private object? ConvertArgumentValue(ParameterInfo param, string value)
        {
            var parameterType = param.ParameterType;

            if (parameterType.IsPrimitive)
            {
                return ConvertPrimitave(param, value);
            }
            else if (parameterType.IsEnum)
            {
                if (Enum.TryParse(parameterType, value, ignoreCase: true, out var result))
                {
                    return result;
                }
                var allowedValues = Enum.GetNames(parameterType);
                throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] is not allowed. Allowed values are [{string.Join(",", allowedValues)}].");
            }

            throw new Exception($"Function [{Name}], the value [{value}] passed to parameter [{param.Name}] could not be converted to type [{parameterType.Name}].");
        }

        private object ConvertPrimitave(ParameterInfo param, string value)
        {
            switch (param.ParameterType)
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
