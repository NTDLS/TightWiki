using System.Text;

namespace TightWiki.Plugin.Engine
{
    /// <summary>
    /// A string builder class that provides additional functionalities for string manipulation.
    /// </summary>
    public class TwString
    {
        private readonly StringBuilder _builder = new();

        /// <summary>
        /// A string builder class that provides additional functionalities for string manipulation.
        /// </summary>
        public TwString()
        {
        }

        /// <summary>
        /// A string builder class that provides additional functionalities for string manipulation.
        /// </summary>
        public TwString(string value)
            => _builder = new StringBuilder(value);

        /// <summary>
        /// Gets the number of characters contained in the current instance.
        /// </summary>
        public int Length
            => _builder.Length;

        /// <summary>
        /// Replaces all occurrences of a specified string in the current content with another specified string.
        /// </summary>
        /// <param name="oldValue">The string to be replaced. Cannot be null or empty.</param>
        /// <param name="newValue">The string to replace all occurrences of oldValue. Can be null to remove all occurrences of oldValue.</param>
        public void Replace(string oldValue, string newValue)
            => _builder.Replace(oldValue, newValue);

        /// <summary>
        /// Inserts the specified string at the given position within the current content.
        /// </summary>
        /// <param name="position">The zero-based index at which to insert the specified string. Must be greater than or equal to 0 and less
        /// than or equal to the current length.</param>
        /// <param name="value">The string to insert at the specified position. Cannot be null.</param>
        public void Insert(int position, string value)
            => _builder.Insert(position, value);

        /// <summary>
        /// Removes all characters from the current content, effectively resetting it to an empty state.
        /// </summary>
        public void Clear()
            => _builder.Clear();

        /// <summary>
        /// Appends the specified string to the end of the current instance.
        /// </summary>
        /// <param name="value">The string to append to the current instance. Can be null, in which case no changes are made.</param>
        public void Append(string value)
            => _builder.Append(value);

        /// <summary>
        /// Returns a string that represents the current object.
        /// </summary>
        /// <returns>A string representation of the current object.</returns>
        public new string ToString()
            => _builder.ToString();
    }
}
