using System;
using System.Linq;

namespace TightWiki.Shared.Models.Data
{
    public class NameNav
    {
        public string Name { get; set; }
        public string Namespace { get; set; }
        public string Navigation { get; set; }

        public NameNav()
        {
        }

        public NameNav(string name, string navigation)
        {
            var parts = name.Split("::");

            if (parts.Count() == 1)
            {
                Name = parts[0]?.Trim();
            }
            else if (parts.Count() == 2)
            {
                Namespace = parts[0]?.Trim();
                Name = parts[1]?.Trim();
            }
            else
            {
                throw new Exception($"Invalid page name {name}");
            }

            Navigation = navigation;
        }
    }
}
