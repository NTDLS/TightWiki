using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Users;

public partial class AccountPermission
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public int PermissionId { get; set; }

    public string? Namespace { get; set; }

    public string? PageId { get; set; }

    public int PermissionDispositionId { get; set; }

    public virtual Permission Permission { get; set; } = null!;

    public virtual PermissionDisposition PermissionDisposition { get; set; } = null!;

    public virtual Profile User { get; set; } = null!;
}
