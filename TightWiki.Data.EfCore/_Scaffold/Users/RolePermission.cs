using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Users;

public partial class RolePermission
{
    public int Id { get; set; }

    public int RoleId { get; set; }

    public int PermissionId { get; set; }

    public string? Namespace { get; set; }

    public string? PageId { get; set; }

    public int PermissionDispositionId { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual PermissionDisposition PermissionDisposition { get; set; } = null!;

    public virtual Role Role { get; set; } = null!;
}
