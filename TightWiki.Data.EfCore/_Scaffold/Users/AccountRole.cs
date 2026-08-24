using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Users;

public partial class AccountRole
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    public int RoleId { get; set; }

    public virtual Role Role { get; set; } = null!;

    public virtual Profile User { get; set; } = null!;
}
