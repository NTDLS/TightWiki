using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore._Scaffold.Statistics;

public partial class StatisticsContext : DbContext
{
    public StatisticsContext(DbContextOptions<StatisticsContext> options)
        : base(options)
    {
    }

    public virtual DbSet<PageStatistic> PageStatistics { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PageStatistic>(entity =>
        {
            entity.HasIndex(e => e.PageId, "IX_CompilationStatistics_PageId").IsUnique();
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
