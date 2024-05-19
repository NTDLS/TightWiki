namespace TightWiki.Library.Library
{
    public class WikiString
    {
        public WikiString()
        {
        }

        public WikiString(string value)
        {
            Value = value;
        }

        public string Value { get; set; } = string.Empty;

        public int Length => Value.Length;

        public void Replace(string oldValue, string newValue)
        {
            Value = Value.Replace(oldValue, newValue);
        }

        public void Insert(int position, string value)
        {
            Value = Value.Insert(position, value);
        }

        public void Clear()
        {
            Value = string.Empty;
        }

        public void Append(string value)
        {
            Value += value;
        }

        public new string ToString()
        {
            return Value;
        }
    }
}
