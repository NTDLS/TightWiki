using NTDLS.Helpers;
using TightWiki.Engine.Library.Attributes;

namespace TightWiki.Engine.Library
{
    public class TightEngineFunctionModule
    {
        public Type DeclaringType { get; private set; }
        public TightWikiFunctionModuleAttribute Attribute { get; private set; }
        public ITightWikiFunctionModule Instance { get; private set; }

        public TightEngineFunctionModule(Type declaringType, TightWikiFunctionModuleAttribute attribute)
        {
            DeclaringType = declaringType;
            Attribute = attribute;
            Instance = ((ITightWikiFunctionModule?)Activator.CreateInstance(declaringType)).EnsureNotNull();
        }
    }
}
