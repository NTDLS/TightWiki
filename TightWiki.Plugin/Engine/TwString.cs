using System.Text;

namespace TightWiki.Plugin.Engine
{
    public class TwString
    {
        private readonly StringBuilder _builder = new();
        
        public TwString()
        {
        }

        public TwString(string value)
            => _builder = new StringBuilder(value);

        public int Length
            => _builder.Length;

        public void Replace(string oldValue, string newValue)
            => _builder.Replace(oldValue, newValue);

        public void Insert(int position, string value)
            => _builder.Insert(position, value);

        public void Clear()
            => _builder.Clear();

        public void Append(string value)
            => _builder.Append(value);

        public new string ToString()
            => _builder.ToString();
    }
}
