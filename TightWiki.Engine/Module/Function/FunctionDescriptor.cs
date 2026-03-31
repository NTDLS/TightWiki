using System.Reflection;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Attributes.Functions;
using TightWiki.Plugin.Interfaces.Module;
using TightWiki.Plugin.Interfaces.Module.Function;

namespace TightWiki.Engine.Module.Function
{
    public class FunctionDescriptor
        : ITwFunctionDescriptor
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
        /// The attribute of the module that contains the function, containing information
        /// such as the module name, description, and the order of execution of the module in relation to other modules.
        /// </summary>
        public TwPluginModuleAttribute ModuleAttribute { get; }

        /// <summary>
        /// List of parameters that the function accepts, containing information such as the parameter type and name.
        /// Same as method.GetParameters().ToList(), but done here to avoid having to call GetParameters() multiple times, which can be expensive.
        /// </summary>
        public List<ParameterInfo> Parameters { get; }

        /// <summary>
        /// The class that contains the function method.
        /// This is used to invoke the method when the function is called, and can also be used to access any properties
        /// or fields of the class that may be needed for the function's execution.
        /// </summary>
        public ITwPluginModule EngineModule { get; }

        public FunctionDescriptor(PluginModule engineModule, MethodInfo method,
            ITwFunctionDescriptorAttribute attribute, TwPluginModuleAttribute moduleAttribute)
        {
            EngineModule = engineModule;
            Method = method;
            Attribute = attribute;
            ModuleAttribute = moduleAttribute;
            Parameters = method.GetParameters().ToList();
        }
    }
}
