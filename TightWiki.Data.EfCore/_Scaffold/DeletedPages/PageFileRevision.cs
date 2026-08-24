using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class PageFileRevision
{
    public int PageFileId { get; set; }

    public string ContentType { get; set; } = null!;

    public int Size { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public string CreatedDate { get; set; } = null!;

    public byte[] Data { get; set; } = null!;

    public int Revision { get; set; }

    public int DataHash { get; set; }
}
