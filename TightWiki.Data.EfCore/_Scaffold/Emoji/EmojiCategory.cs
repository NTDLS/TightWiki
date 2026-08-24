using System;
using System.Collections.Generic;

namespace TightWiki.Data.EfCore._Scaffold.Emoji;

public partial class EmojiCategory
{
    public int Id { get; set; }

    public int EmojiId { get; set; }

    public string Category { get; set; } = null!;
}
