namespace TightWiki.Engine.Library
{
    /// <summary>
    /// TODO: This is used by some curiously diverse portions of code, that needs to be fixed.
    /// </summary>
    public class NameNav
    {
        public string Name { get; set; } = string.Empty;
        public string Namespace { get; set; } = string.Empty;
        public string Navigation { get; set; } = string.Empty;

        public NameNav()
        {
        }

        public override bool Equals(object? obj)
        {
            return obj is NameNav other
                && string.Equals(Navigation, other.Navigation, StringComparison.OrdinalIgnoreCase);
        }

        public override int GetHashCode()
        {
            return Navigation.GetHashCode();
        }

        public NameNav(string name, string navigation)
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
