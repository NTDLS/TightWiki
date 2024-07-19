using System.Data;

namespace TightWiki.Engine
{
    public static class ExtensionMethods
    {
        public static string RemoveWhitespace(this string input)
            => new(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
    }
}