namespace TightWiki.Plugin.Models
{
    /// <summary>
    /// Represents a reference to a wiki page, storing its display name, namespace,
    /// and URL-safe navigation path. Used to track outbound links between pages.
    /// </summary>
    public class TwPageReference
    {
        /// <summary>
        /// The display name of the referenced page, such as "Sand Box" or "Some Namespace::SandBox".
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The namespace portion of the page name, or an empty string if no namespace is present.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// <summary>
        /// The URL-safe navigation path of the referenced page, safe for use in URLs and links.
        /// </summary>
        public string Navigation { get; set; } = string.Empty;

        /// <summary>
        /// Initializes a new empty page reference.
        /// </summary>
        public TwPageReference()
        {
        }

        /// <summary>
        /// Returns true if the specified object is a <see cref="TwPageReference"/> with the same navigation path,
        /// using a case-insensitive comparison.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return obj is TwPageReference other
                && string.Equals(Navigation, other.Navigation, StringComparison.InvariantCultureIgnoreCase);
        }

        /// <summary>
        /// Returns a hash code based on the navigation path of this page reference.
        /// </summary>
        public override int GetHashCode()
        {
            return Navigation.GetHashCode();
        }

        /// <summary>
        /// Initializes a new page reference by parsing the given name into its namespace and title components,
        /// and storing the provided URL-safe navigation path.
        /// </summary>
        /// <param name="name">The full page name, optionally including a namespace prefix separated by "::".</param>
        /// <param name="navigation">The URL-safe navigation path for this page reference.</param>
        /// <exception cref="Exception">Thrown if the page name contains more than one "::" separator.</exception>
        public TwPageReference(string name, string navigation)
        {
            var parts = name.Split("::");

            if (parts.Length == 1)
            {
                Name = parts[0].Trim();
            }
            else if (parts.Length == 2)
            {
                Namespace = parts[0].Trim();
                Name = parts[1].Trim();
            }
            else
            {
                throw new Exception($"Invalid page name {name}");
            }

            Navigation = navigation;
        }
    }
}