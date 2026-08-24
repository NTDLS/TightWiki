using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class DeletionMetum
{
    public int PageId { get; set; }

    public int? DeletedByUserId { get; set; }

    public int? DeletedDate { get; set; }
}
