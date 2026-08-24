using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PageFileRevision
{
    public int PageFileId { get; set; }

    public string ContentType { get; set; } = null!;

    public int Size { get; set; }

    public string CreatedByUserId { get; set; } = null!;

    public DateTime CreatedDate { get; set; }

    public byte[] Data { get; set; } = null!;

    public int Revision { get; set; }

    public int DataHash { get; set; }

    public virtual PageFile PageFile { get; set; } = null!;
}
