using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Users;

public partial class PermissionDisposition
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public virtual ICollection<AccountPermission> AccountPermissions { get; set; } = new List<AccountPermission>();

    public virtual ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
