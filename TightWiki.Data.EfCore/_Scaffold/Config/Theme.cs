using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Config;

public partial class Theme
{
    public string Name { get; set; } = null!;

    public string DelimitedFiles { get; set; } = null!;

    public string ClassNavBar { get; set; } = null!;

    public string ClassNavLink { get; set; } = null!;

    public string ClassDropdown { get; set; } = null!;

    public string ClassBranding { get; set; } = null!;

    public string EditorTheme { get; set; } = null!;
}
