using System.Reflection;
using TightWiki.Plugin.Attributes;

namespace TightWiki.Plugin
{
    public class TwEngineFunctionDescriptor
    {
        /// <summary>
        /// Reference to the function that will be called when this function is invoked.
        /// </summary>
        public MethodInfo Method { get; }

        /// <summary>
        /// Attributes of the function, containing information such as the demarcation and description.
        /// This is used to match a function call to its descriptor and for documentation purposes.
        /// </summary>
        public ITwFunctionDescriptorAttribute Attribute { get; }

        /// <summary>
        /// List of functiont that the function accepts, containing information such as the parameter type and name.
        /// Same as method.GetParameters().ToList(), but done here to avoid having to call GetParameters() multiple times, which can be expensive.
        /// </summary>
        public List<ParameterInfo> Parameters { get; }

        /// <summary>
        /// The class that contains the function method.
        /// This is used to invoke the method when the function is called, and can also be used to access any properties
        /// or fields of the class that may be needed for the function's execution.
        /// </summary>
        public TwEngineFunctionModule EngineModule { get; }

        public TwEngineFunctionDescriptor(TwEngineFunctionModule engineModule, MethodInfo method, ITwFunctionDescriptorAttribute attribute)
        {
            EngineModule = engineModule;
            Method = method;
            Attribute = attribute;
            Parameters = method.GetParameters().ToList();
        }
    }
}
