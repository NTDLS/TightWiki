using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore._Scaffold.Pages;

public partial class PagesContext : DbContext
{
    public PagesContext(DbContextOptions<PagesContext> options)
        : base(options)
    {
    }

    public virtual DbSet<CurrentPageEditor> CurrentPageEditors { get; set; }

    public virtual DbSet<FeatureTemplate> FeatureTemplates { get; set; }

    public virtual DbSet<Page> Pages { get; set; }

    public virtual DbSet<PageComment> PageComments { get; set; }

    public virtual DbSet<PageFile> PageFiles { get; set; }

    public virtual DbSet<PageFileRevision> PageFileRevisions { get; set; }

    public virtual DbSet<PageProcessingInstruction> PageProcessingInstructions { get; set; }

    public virtual DbSet<PageReference> PageReferences { get; set; }

    public virtual DbSet<PageRevision> PageRevisions { get; set; }

    public virtual DbSet<PageRevisionAttachment> PageRevisionAttachments { get; set; }

    public virtual DbSet<PageTag> PageTags { get; set; }

    public virtual DbSet<PageToken> PageTokens { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<CurrentPageEditor>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.UserId });

            entity.Property(e => e.AccountName).UseCollation("NOCASE");
            entity.Property(e => e.Utcdate).HasColumnName("UTCDate");
        });

        modelBuilder.Entity<FeatureTemplate>(entity =>
        {
            entity.HasKey(e => new { e.Name, e.Type });

            entity.ToTable("FeatureTemplate");

            entity.Property(e => e.Name).UseCollation("NOCASE");
            entity.Property(e => e.Type).UseCollation("NOCASE");

            entity.HasOne(d => d.Page).WithMany(p => p.FeatureTemplates).HasForeignKey(d => d.PageId);
        });

        modelBuilder.Entity<Page>(entity =>
        {
            entity.ToTable("Page");

            entity.HasIndex(e => new { e.Namespace, e.Name }, "IX_Page_Namespace_Name").IsUnique();

            entity.HasIndex(e => e.Name, "IX_Page_Name").IsUnique();

            entity.HasIndex(e => e.Navigation, "IX_Page_Navigation").IsUnique();

            entity.Property(e => e.Name).UseCollation("NOCASE");
            entity.Property(e => e.Namespace).UseCollation("NOCASE");
            entity.Property(e => e.Navigation).UseCollation("NOCASE");
        });

        modelBuilder.Entity<PageComment>(entity =>
        {
            entity.ToTable("PageComment");

            entity.HasIndex(e => e.PageId, "IX_PageComment_PageId");

            entity.HasOne(d => d.Page).WithMany(p => p.PageComments)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PageFile>(entity =>
        {
            entity.ToTable("PageFile");

            entity.HasIndex(e => new { e.PageId, e.Name, e.Revision }, "IX_PageFile_PageId_Name_Revision").IsUnique();

            entity.HasIndex(e => new { e.Name, e.PageId, e.Revision }, "IX_PageFile_Id_Navigation_Revision").IsUnique();

            entity.Property(e => e.Name).UseCollation("NOCASE");
            entity.Property(e => e.Navigation).UseCollation("NOCASE");

            entity.HasOne(d => d.Page).WithMany(p => p.PageFiles)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PageFileRevision>(entity =>
        {
            entity.HasKey(e => new { e.PageFileId, e.Revision });

            entity.ToTable("PageFileRevision");

            entity.Property(e => e.ContentType).UseCollation("NOCASE");
            entity.Property(e => e.CreatedByUserId).UseCollation("NOCASE");

            entity.HasOne(d => d.PageFile).WithMany(p => p.PageFileRevisions)
                .HasForeignKey(d => d.PageFileId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PageProcessingInstruction>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Instruction });

            entity.ToTable("PageProcessingInstruction");

            entity.Property(e => e.Instruction).UseCollation("NOCASE");

            entity.HasOne(d => d.Page).WithMany(p => p.PageProcessingInstructions)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PageReference>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.ReferencesPageNavigation });

            entity.ToTable("PageReference");

            entity.Property(e => e.ReferencesPageNavigation).UseCollation("NOCASE");
            entity.Property(e => e.ReferencesPageName).UseCollation("NOCASE");

            entity.HasOne(d => d.Page).WithMany(p => p.PageReferencePages)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.ReferencesPage).WithMany(p => p.PageReferenceReferencesPages).HasForeignKey(d => d.ReferencesPageId);
        });

        modelBuilder.Entity<PageRevision>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Revision });

            entity.ToTable("PageRevision");

            entity.HasIndex(e => new { e.PageId, e.Revision }, "IX_PageRevision_PageId_Revision").IsUnique();

            entity.Property(e => e.ModifiedDate).HasColumnType("INTEGER");
            entity.Property(e => e.Name).UseCollation("NOCASE");
            entity.Property(e => e.Namespace).UseCollation("NOCASE");
        });

        modelBuilder.Entity<PageRevisionAttachment>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.PageFileId, e.FileRevision, e.PageRevision });

            entity.ToTable("PageRevisionAttachment");

            entity.HasIndex(e => new { e.PageId, e.PageFileId, e.PageRevision }, "IX_PageRevisionAttachment_PageId_PageFileId_PageRevision").IsUnique();

            entity.HasOne(d => d.PageFile).WithMany(p => p.PageRevisionAttachments)
                .HasForeignKey(d => d.PageFileId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Page).WithMany(p => p.PageRevisionAttachments)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PageTag>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Tag });

            entity.ToTable("PageTag");

            entity.Property(e => e.Tag).UseCollation("NOCASE");
            entity.Property(e => e.Navigation)
                .HasDefaultValue("")
                .UseCollation("NOCASE");

            entity.HasOne(d => d.Page).WithMany(p => p.PageTags)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<PageToken>(entity =>
        {
            entity.HasKey(e => new { e.PageId, e.Token });

            entity.ToTable("PageToken");

            entity.HasIndex(e => new { e.DoubleMetaphone, e.PageId, e.Weight }, "idx_PageToken_DoubleMetaphone_PageId_Weight");

            entity.HasIndex(e => e.PageId, "idx_PageToken_PageId");

            entity.HasIndex(e => new { e.Token, e.PageId, e.Weight }, "idx_PageToken_Token_PageId_Weight");

            entity.Property(e => e.Token).UseCollation("NOCASE");
            entity.Property(e => e.DoubleMetaphone).UseCollation("NOCASE");

            entity.HasOne(d => d.Page).WithMany(p => p.PageTokens)
                .HasForeignKey(d => d.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
