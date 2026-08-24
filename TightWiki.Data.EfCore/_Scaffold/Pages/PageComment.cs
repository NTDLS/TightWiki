using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PageComment
{
    public int Id { get; set; }

    public int PageId { get; set; }

    public DateTime CreatedDate { get; set; }

    public Guid UserId { get; set; }

    public string Body { get; set; } = null!;

    public virtual Page Page { get; set; } = null!;
}
