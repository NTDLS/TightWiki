using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class PageComment
{
    public int Id { get; set; }

    public int PageId { get; set; }

    public string CreatedDate { get; set; } = null!;

    public string UserId { get; set; } = null!;

    public string Body { get; set; } = null!;
}
