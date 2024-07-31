namespace TightWiki.Engine.Library
{
    public class PageReference
    {
        /// <summary>
        /// The name of the page. Such as "Sand Box" or "Some Namespace : SandBox". 
        /// </summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// The namespace part of the Name.
        /// </summary>
        public string Namespace { get; set; } = string.Empty;

        /// The cleaned up version of the name, safe for passing in URLs.
        public string Navigation { get; set; } = string.Empty;

        public PageReference()
        {
        }

        public override bool Equals(object? obj)
        {
            return obj is PageReference other
                && string.Equals(Navigation, other.Navigation, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Navigation.GetHashCode();
        }

        public PageReference(string name, string navigation)
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
