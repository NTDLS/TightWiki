using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Config;

namespace TightWiki.Data.EfCore.Configurations.Config
{
    /// <summary>
    /// Fluent configuration for <see cref="CryptoCheck"/> (Config.CryptoCheck).
    /// </summary>
    public class CryptoCheckConfiguration : IEntityTypeConfiguration<CryptoCheck>
    {
        public void Configure(EntityTypeBuilder<CryptoCheck> builder)
        {
            //The real table has no primary key - it holds a single row that is deleted and re-inserted wholesale.
            builder.HasNoKey();

            builder.ToTable("CryptoCheck", schema: "Config");
        }
    }
}
