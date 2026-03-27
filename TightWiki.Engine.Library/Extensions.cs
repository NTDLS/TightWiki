using System.Reflection;

namespace TightWiki.Engine.Library
{
    internal static class Extensions
    {
        public static bool IsInfinite(this ParameterInfo param)
        {
            return param.ParameterType.IsArray //It the parameter an array or List<>?
                    || (param.ParameterType.IsGenericType && param.ParameterType.GetGenericTypeDefinition() == typeof(List<>));
        }
    }
}
