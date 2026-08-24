using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore._Scaffold.DeletedPages;

public partial class DeletedPagesContext : DbContext
{
    public DeletedPagesContext(DbContextOptions<DeletedPagesContext> options)
        : base(options)
    {
    }

    public virtual DbSet<DeletionMetum> DeletionMeta { get; set; }

    public virtual DbSet<Page> Pages { get; set; }

    public virtual DbSet<PageComment> PageComments { get; set; }

    public virtual DbSet<PageFile> PageFiles { get; set; }

    public virtual DbSet<PageFileRevision> PageFileRevisions { get; set; }

    public virtual DbSet<PageProcessingInstruction> PageProcessingInstructions { get; set; }

    public virtual DbSet<PageRevision> PageRevisions { get; set; }

    public virtual DbSet<PageRevisionAttachment> PageRevisionAttachments { get; set; }

    public virtual DbSet<PageTag> PageTags { get; set; }

    public virtual DbSet<PageToken> PageTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DeletionMetum>(entity =>
        {
            entity.HasKey(e => e.PageId);

            entity.Property(e => e.PageId).ValueGeneratedNever();
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.ToTable("Page");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).UseCollation("NOCASE");
            entity.Property(e => e.Namespace).UseCollation("NOCASE");
            entity.Property(e => e.Navigation).UseCollation("NOCASE");
        });

        modelBuilder.Entity<PageComment>(entity =>
        {
            entity.ToTable("PageComment");

            entity.Property(e => e.Id).ValueGeneratedNever();
        });

        modelBuilder.Entity<PageFile>(entity =>
        {
            entity.ToTable("PageFile");

            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.Name).UseCollation("NOCASE");
            entity.Property(e => e.Navigation).UseCollation("NOCASE");
        });

        modelBuilder.Entity<PageFileRevision>(entity =>
        {
            entity.HasKey(e => new { e.PageFileId, e.Revision });

            entity.ToTable("PageFileRevision");

            entity.Property(e => e.ContentType).UseCollation("NOCASE");
            entity.Property(e => e.CreatedByUserId).UseCollation("NOCASE");
        });

        modelBuilder.Entity<PageProcessingInstruction>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Instruction });

            entity.ToTable("PageProcessingInstruction");

            entity.Property(e => e.Instruction).UseCollation("NOCASE");
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

        modelBuilder.Entity<PageTag>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Tag });

            entity.ToTable("PageTag");

            entity.Property(e => e.Tag).UseCollation("NOCASE");
        });

        modelBuilder.Entity<PageToken>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Token });

            entity.ToTable("PageToken");

            entity.Property(e => e.Token).UseCollation("NOCASE");
            entity.Property(e => e.DoubleMetaphone).UseCollation("NOCASE");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
