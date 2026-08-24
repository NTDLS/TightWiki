using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Users;

public partial class AspNetRole
{
    public string Id { get; set; } = null!;

    public string? ConcurrencyStamp { get; set; }

    public string? Name { get; set; }

    public string? NormalizedName { get; set; }

    public virtual ICollection<AspNetRoleClaim> AspNetRoleClaims { get; set; } = new List<AspNetRoleClaim>();

    public virtual ICollection<AspNetUser> Users { get; set; } = new List<AspNetUser>();
}
