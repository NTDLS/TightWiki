using System.Reflection;

namespace TightWiki.Engine.Library
{
    public class TightEnginFunctionEnvelope
    {
        public MethodInfo Method { get; set; }
        public Attribute Attribute { get; set; }

        public TightEnginFunctionEnvelope(MethodInfo method, Attribute attribute)
        {
            Method = method;
            Attribute = attribute;
        }
    }
}
