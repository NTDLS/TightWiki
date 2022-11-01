using System;
using System.Collections.Generic;
using System.Text;

namespace TightWiki.Shared.Models.Data
{
    public class NameNav
    {
        public string Name { get; set; }
        public string Navigation { get; set; }

        public NameNav()
        {
        }

        public NameNav(string name, string navigation)
        {
            Name = name;
            Navigation = navigation;
        }
    }
}
