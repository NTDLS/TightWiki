using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore._Scaffold.Logging;

public partial class LoggingContext : DbContext
{
    public LoggingContext(DbContextOptions<LoggingContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Log> Logs { get; set; }

    public virtual DbSet<Severity> Severities { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Log>(entity =>
        {
            entity.ToTable("Log");

            entity.HasOne(d => d.Severity).WithMany(p => p.Logs).HasForeignKey(d => d.SeverityId);
        });

        modelBuilder.Entity<Severity>(entity =>
        {
            entity.ToTable("Severity");

            entity.HasIndex(e => e.Name, "IX_Severity_Name").IsUnique();

            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
