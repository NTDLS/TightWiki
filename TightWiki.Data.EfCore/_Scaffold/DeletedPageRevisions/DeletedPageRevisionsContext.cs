using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPageRevisions;

public partial class DeletedPageRevisionsContext : DbContext
{
    public DeletedPageRevisionsContext(DbContextOptions<DeletedPageRevisionsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DeletionMetum> DeletionMeta { get; set; }

    public virtual DbSet<PageRevision> PageRevisions { get; set; }

    public virtual DbSet<PageRevisionAttachment> PageRevisionAttachments { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeletionMetum>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Revision });
        });

        modelBuilder.Entity<PageRevision>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Revision });

            entity.ToTable("PageRevision");

            entity.Property(e => e.Name).UseCollation("NOCASE");
            entity.Property(e => e.Namespace).UseCollation("NOCASE");
        });

        modelBuilder.Entity<PageRevisionAttachment>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.PageFileId, e.FileRevision, e.PageRevision });

            entity.ToTable("PageRevisionAttachment");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
