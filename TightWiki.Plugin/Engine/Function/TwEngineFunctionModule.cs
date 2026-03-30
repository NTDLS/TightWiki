using NTDLS.Helpers;
using TightWiki.Plugin.Attributes;
using TightWiki.Plugin.Interfaces;

namespace TightWiki.Plugin.Engine.Function
{
    public class TwEngineFunctionModule
    {
        public Type DeclaringType { get; private set; }
        public TwFunctionModuleAttribute Attribute { get; private set; }
        public ITwFunctionModule Instance { get; private set; }

        public TwEngineFunctionModule(Type declaringType, TwFunctionModuleAttribute attribute)
        {
            DeclaringType = declaringType;
            Attribute = attribute;
            Instance = ((ITwFunctionModule?)Activator.CreateInstance(declaringType)).EnsureNotNull();
        }
    }
}
