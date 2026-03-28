using System.Reflection;

namespace TightWiki.Engine.Library
{
    public class TightEngineFunctionEnvelope
    {
        public MethodInfo Method { get; }
        public Attribute Attribute { get; }
        public List<ParameterInfo> Parameters { get; }
        public TightEngineFunctionModule EngineModule { get; }

        public TightEngineFunctionEnvelope(TightEngineFunctionModule engineModule, MethodInfo method, Attribute attribute)
        {
            EngineModule = engineModule;
            Method = method;
            Attribute = attribute;
            Parameters = method.GetParameters().ToList();
        }
    }
}
