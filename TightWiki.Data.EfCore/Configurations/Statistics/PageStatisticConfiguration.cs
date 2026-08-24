using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Statistics;

namespace TightWiki.Data.EfCore.Configurations.Statistics
{
    /// <summary>
    /// Fluent configuration for <see cref="PageStatistic"/> (Statistics.PageStatistics).
    /// </summary>
    public class PageStatisticConfiguration : IEntityTypeConfiguration<PageStatistic>
    {
        public void Configure(EntityTypeBuilder<PageStatistic> builder)
        {
            builder.ToTable("PageStatistics", schema: "Statistics");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.TotalViewCount)
                .HasDefaultValue(0);

            //Unique index name preserved from the real schema, which still carries the pre-2.31.1 table name
            //("CompilationStatistics") even though the table itself was renamed to "PageStatistics".
            builder.HasIndex(e => e.PageId, "IX_CompilationStatistics_PageId")
                .IsUnique();

            //PageId references Pages.Page - real, one-to-one, cross-schema relationship (both schemas now live
            //in the same TightWikiDbContext, see Database-Providers-Plan.md chapter 4.3). Configured from this
            //(dependent) side; PageId already carries a UNIQUE index above, which is what makes this one-to-one
            //rather than one-to-many.
            builder.HasOne(e => e.Page)
                .WithOne(e => e.PageStatistic)
                .HasForeignKey<PageStatistic>(e => e.PageId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
