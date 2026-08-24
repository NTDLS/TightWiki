using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore._Scaffold.Emoji;

public partial class EmojiContext : DbContext
{
    public EmojiContext(DbContextOptions<EmojiContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Emoji> Emojis { get; set; }

    public virtual DbSet<EmojiCategory> EmojiCategories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Emoji>(entity =>
        {
            entity.ToTable("Emoji");

            entity.HasIndex(e => e.Name, "IX_Emoji_Name").IsUnique();

            entity.HasIndex(e => e.Name, "IX_Emoji").IsUnique();

            entity.Property(e => e.MimeType).UseCollation("NOCASE");
            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        modelBuilder.Entity<EmojiCategory>(entity =>
        {
            entity.ToTable("EmojiCategory");

            entity.HasIndex(e => new { e.EmojiId, e.Category }, "IX_EmojiCategory_EmojiId_Category").IsUnique();

            entity.HasIndex(e => new { e.EmojiId, e.Category }, "IX_EmojiCategory").IsUnique();

            entity.Property(e => e.Category).UseCollation("NOCASE");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
