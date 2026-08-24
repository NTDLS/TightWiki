using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore._Scaffold.Config;

public partial class ConfigContext : DbContext
{
    public ConfigContext(DbContextOptions<ConfigContext> options)
        : base(options)
    {
    }

    public virtual DbSet<ConfigurationEntry> ConfigurationEntries { get; set; }

    public virtual DbSet<ConfigurationGroup> ConfigurationGroups { get; set; }

    public virtual DbSet<CryptoCheck> CryptoChecks { get; set; }

    public virtual DbSet<DataType> DataTypes { get; set; }

    public virtual DbSet<MenuItem> MenuItems { get; set; }

    public virtual DbSet<Theme> Themes { get; set; }

    public virtual DbSet<VersionState> VersionStates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ConfigurationEntry>(entity =>
        {
            entity.ToTable("ConfigurationEntry");

            entity.HasIndex(e => new { e.ConfigurationGroupId, e.Name }, "IX_ConfigurationEntry_ConfigurationGroupId_Name").IsUnique();

            entity.Property(e => e.DataTypeId).HasColumnType("INT");
            entity.Property(e => e.Description).UseCollation("NOCASE");
            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        modelBuilder.Entity<ConfigurationGroup>(entity =>
        {
            entity.ToTable("ConfigurationGroup");

            entity.HasIndex(e => e.Name, "IX_ConfigurationGroup_Name").IsUnique();

            entity.Property(e => e.Description).UseCollation("NOCASE");
            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        modelBuilder.Entity<CryptoCheck>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("CryptoCheck");
        });

        modelBuilder.Entity<DataType>(entity =>
        {
            entity.ToTable("DataType");

            entity.HasIndex(e => e.Name, "IX_DataType_Name").IsUnique();

            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        modelBuilder.Entity<MenuItem>(entity =>
        {
            entity.ToTable("MenuItem");

            entity.Property(e => e.Link).UseCollation("NOCASE");
            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        modelBuilder.Entity<Theme>(entity =>
        {
            entity.HasKey(e => e.Name);

            entity.ToTable("Theme");
        });

        modelBuilder.Entity<VersionState>(entity =>
        {
            entity.ToTable("VersionState");

            entity.HasIndex(e => e.Name, "IX_VersionState_Name").IsUnique();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
