using System.Reflection;
using TightWiki.Engine.Library.Attributes;

namespace TightWiki.Engine.Library
{
    public class TightEngineFunctionEnvelope
    {
        public MethodInfo Method { get; }
        public ITightWikiFunctionPrototypeAttribute Attribute { get; }
        public List<ParameterInfo> Parameters { get; }
        public TightEngineFunctionModule EngineModule { get; }

        public TightEngineFunctionEnvelope(TightEngineFunctionModule engineModule, MethodInfo method, ITightWikiFunctionPrototypeAttribute attribute)
        {
            EngineModule = engineModule;
            Method = method;
            Attribute = attribute;
            Parameters = method.GetParameters().ToList();
        }
    }
}
