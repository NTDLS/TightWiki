using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Users;

public partial class Profile
{
    public Guid UserId { get; set; }

    public string? Navigation { get; set; }

    public string? AccountName { get; set; }

    public string? Biography { get; set; }

    public byte[]? Avatar { get; set; }

    public DateTime CreatedDate { get; set; }

    public DateTime ModifiedDate { get; set; }

    public string? AvatarContentType { get; set; }

    public virtual ICollection<AccountPermission> AccountPermissions { get; set; } = new List<AccountPermission>();

    public virtual ICollection<AccountRole> AccountRoles { get; set; } = new List<AccountRole>();
}
