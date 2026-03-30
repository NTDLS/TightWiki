using NTDLS.Helpers;
using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Engine.Function
{
    /// <summary>
    /// Contains information about an actual function call, its supplied parameters, and is matched with a defined function.
    /// </summary>
    public class TwPreparedFunction
    {
        /// <summary>
        /// The name of the function being called.
        /// </summary>
        public string Name { get; private set; }

        public TwEngineFunctionDescriptor Descriptor { get; set; }

        /// <summary>
        /// The arguments supplied by the caller.
        /// </summary>
        public List<TwNamedParameter> Parameters { get; private set; } = new();

        public TwPreparedFunction(TwEngineFunctionDescriptor descriptor, TwParsedFunction parsedFunction)
        {
            Descriptor = descriptor;
            Name = descriptor.Method.Name;
        }

        public async Task<TwHandlerResult> Execute()
        {
            var parameters = Parameters.Select(o => o.Value).ToArray();
            var result = ((Task<TwHandlerResult>?)Descriptor.Method.Invoke(Descriptor.EngineModule.Instance, parameters)).EnsureNotNull();
            return await result;
        }

        /// <summary>
        /// Parsed a function call, its parameters and matches it to a defined function and its descriptor.
        /// </summary>
        public static TwPreparedFunction Create(ITwEngineState state,
            List<TwEngineFunctionDescriptor> descriptors, TwParsedFunction parsedFunction)
        {
            var descriptor = descriptors.SingleOrDefault(o =>
                o.Method.Name.Equals(parsedFunction.Name, StringComparison.InvariantCultureIgnoreCase)
                && o.Attribute is ITwFunctionDescriptorAttribute attr
                && attr.Demarcation == parsedFunction.Demarcation)
                ?? throw new Exception($"Function ({parsedFunction.Name}) does not have a defined descriptor.");

            int givenArgIndex = 0;
            int descriptorArgIndex = 0;

            var preparedFunction = new TwPreparedFunction(descriptor, parsedFunction);

            if (descriptor.Parameters.Count == 0)
            {
                throw new Exception($"Function [{descriptor.Method.Name}] was called with arguments, but the function does not accept any parameters.");
            }

            //The first parameter must always be the state, so we add it to the parameters list before processing the caller supplied arguments.
            if (descriptor.Parameters[descriptorArgIndex].ParameterType != typeof(ITwEngineState))
            {
                throw new Exception($"Function [{descriptor.Method.Name}] must have the first parameter of type ITightEngineState.");
            }
            preparedFunction.Parameters.Add(new TwNamedParameter(descriptor.Parameters[descriptorArgIndex].Name.EnsureNotNull(), state));
            descriptorArgIndex++;

            if (descriptor.Attribute is TwScopeFunctionAttribute)
            {
                //For scope functions, the second parameter must be the bodyText.
                preparedFunction.Parameters.Add(new TwNamedParameter(descriptor.Parameters[descriptorArgIndex].Name.EnsureNotNull(), parsedFunction.BodyText ?? string.Empty));
                descriptorArgIndex++;
            }

            for (; descriptorArgIndex < descriptor.Parameters.Count && givenArgIndex < parsedFunction.Arguments.Count;)
            {
                var param = descriptor.Parameters[descriptorArgIndex];

                if (TryGetArgPassedByName(parsedFunction.Arguments[givenArgIndex], out var _, out var _))
                {
                    //This argument was passed by name, so we now require all subsequent arguments to also be passed by name.
                    break;
                }

                var type = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
                if (type.IsPrimitive || type.IsEnum || type == typeof(string))
                {
                    preparedFunction.Parameters.Add(new TwNamedParameter(param.Name.EnsureNotNull(), preparedFunction.ConvertArgumentValue(param, parsedFunction.Arguments[givenArgIndex])));
                    descriptorArgIndex++;
                    givenArgIndex++;
                }
                else if (type.IsArray)
                {
                    //Swallow up all remaining arguments and pass them as an array.

                    var elementType = param.ParameterType.GetElementType().EnsureNotNull();
                    var listType = typeof(List<>).MakeGenericType(elementType);
                    var arrayValues = (IList)Activator.CreateInstance(listType)!;

                    for (; givenArgIndex < parsedFunction.Arguments.Count; givenArgIndex++)
                    {
                        arrayValues.Add(preparedFunction.ConvertArgumentValue(param, parsedFunction.Arguments[givenArgIndex]));
                    }

                    var typedArray = Array.CreateInstance(elementType, arrayValues.Count);
                    for (int i = 0; i < arrayValues.Count; i++)
                    {
                        typedArray.SetValue(arrayValues[i], i);
                    }

                    preparedFunction.Parameters.Add(new TwNamedParameter(param.Name.EnsureNotNull(), typedArray));
                    descriptorArgIndex++;
                }
            }

            if (givenArgIndex < descriptor.Parameters.Count && givenArgIndex < parsedFunction.Arguments.Count)
            {
                //We still have arguments left to process, this can only mean that they were passed by name, so we need to process them as such.
                for (; descriptorArgIndex < descriptor.Parameters.Count && givenArgIndex < parsedFunction.Arguments.Count;)
                {
                    var param = descriptor.Parameters[descriptorArgIndex];

                    if (TryGetArgPassedByName(parsedFunction.Arguments[givenArgIndex], out var name, out var value))
                    {
                        if (!descriptor.Parameters.Any(o => string.Equals(o.Name, name, StringComparison.InvariantCultureIgnoreCase)))
                            throw new Exception($"Function [{descriptor.Method.Name}] does not have a parameter named [{name}].");

                        if (string.Equals(name, param.Name, StringComparison.InvariantCultureIgnoreCase))
                        {
                            //We have a named parameter and we are currently processing the correct
                            //  parameter in the function, so we can add it to the parameters list.
                            preparedFunction.Parameters.Add(new TwNamedParameter(param.Name.EnsureNotNull(), preparedFunction.ConvertArgumentValue(param, value)));
                            givenArgIndex++;
                            descriptorArgIndex++;
                            continue;
                        }
                        else
                        {
                            //We have a named parameter but it does not match the current parameter for the function,
                            //so we need to check if the current function's parameter has a default value. If it does, we can
                            //add it to the parameters list and move on to the next parameter for the function.
                            //If it does not, we need to throw an error because we cannot skip required parameters for the function.

                            if (param.HasDefaultValue)
                            {
                                preparedFunction.Parameters.Add(new TwNamedParameter(param.Name.EnsureNotNull(), param.DefaultValue));
                                descriptorArgIndex++;
                                continue;
                            }
                            else
                            {
                                throw new Exception($"Function [{descriptor.Method.Name}], the required argument [{param.Name}] was not supplied.");
                            }
                        }

                        //This argument was passed by name, so we now require all subsequent arguments to also be passed by name.
                    }
                    else if (param.ParameterType.IsArray)
                    {
                        //Swallow up all remaining arguments and pass them as an array.
                        var elementType = param.ParameterType.GetElementType().EnsureNotNull();
                        var listType = typeof(List<>).MakeGenericType(elementType);
                        var arrayValues = (IList)Activator.CreateInstance(listType)!;

                        for (; givenArgIndex < parsedFunction.Arguments.Count; givenArgIndex++)
                        {
                            arrayValues.Add(preparedFunction.ConvertArgumentValue(param, parsedFunction.Arguments[givenArgIndex]));
                        }

                        var typedArray = Array.CreateInstance(elementType, arrayValues.Count);
                        for (int i = 0; i < arrayValues.Count; i++)
                        {
                            typedArray.SetValue(arrayValues[i], i);
                        }

                        preparedFunction.Parameters.Add(new TwNamedParameter(param.Name.EnsureNotNull(), typedArray));
                    }
                }
            }

            //Add the remaining parameters that were not supplied by the caller, if they have default values.
            for (; descriptorArgIndex < descriptor.Parameters.Count;)
            {
                var param = descriptor.Parameters[descriptorArgIndex];

                if (param.HasDefaultValue)
                {
                    preparedFunction.Parameters.Add(new TwNamedParameter(param.Name.EnsureNotNull(), param.DefaultValue));
                    descriptorArgIndex++;
                    continue;
                }
                else
                {
                    throw new Exception($"Function [{descriptor.Method.Name}], the required argument [{param.Name}] was not supplied.");
                }
            }

            if (preparedFunction.Parameters.Count != descriptor.Parameters.Count)
            {
                throw new Exception($"Function [{descriptor.Method.Name}] was called with an incorrect number of arguments. Expected [{descriptor.Parameters.Count}] but received [{descriptor.Parameters.Count}].");
            }

            return preparedFunction;
        }

        private static bool TryGetArgPassedByName(string arg, [NotNullWhen(true)] out string? name, [NotNullWhen(true)] out string? value)
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

        /// <summary>
        /// Checks the passed value against the function descriptor to ensure that the variable is the correct type, value, etc.
        /// </summary>
        private object? ConvertArgumentValue(ParameterInfo param, string value)
        {
            var type = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
            if (type.IsPrimitive || type.IsArray || type == typeof(string))
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
            Type? type;

            if (param.ParameterType.IsArray)
            {
                type = Nullable.GetUnderlyingType(param.ParameterType.GetElementType().EnsureNotNull()) ?? param.ParameterType.GetElementType();
            }
            else
            {
                type = Nullable.GetUnderlyingType(param.ParameterType) ?? param.ParameterType;
            }

            switch (type)
            {
                #region Type value parsers.

                case Type t when t == typeof(string):
                    {
                        return value;
                    }
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
