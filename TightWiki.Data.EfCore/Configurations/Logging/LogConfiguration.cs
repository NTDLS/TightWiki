using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Logging;

namespace TightWiki.Data.EfCore.Configurations.Logging
{
    /// <summary>
    /// Fluent configuration for <see cref="Log"/> (Logging.Log).
    /// </summary>
    public class LogConfiguration : IEntityTypeConfiguration<Log>
    {
        public void Configure(EntityTypeBuilder<Log> builder)
        {
            builder.ToTable("Log", schema: "Logging");

            builder.HasKey(e => e.Id);

            //Intra-schema relationship (both Log and Severity live in the Logging schema), backed by a real
            //FOREIGN KEY constraint in the SQLite schema - not the cross-schema kind that is out of scope here.
            builder.HasOne(e => e.Severity)
                .WithMany(e => e.Logs)
                .HasForeignKey(e => e.SeverityId);
        }
    }
}
