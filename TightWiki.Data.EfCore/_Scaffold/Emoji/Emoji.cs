using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Emoji;

public partial class Emoji
{
    public int Id { get; set; }

    public string Name { get; set; } = null!;

    public byte[]? ImageData { get; set; }

    public string? MimeType { get; set; }
}
