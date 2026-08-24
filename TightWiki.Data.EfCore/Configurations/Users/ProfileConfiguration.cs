using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="Profile"/> (Users.Profile).
    /// </summary>
    /// <remarks>
    /// No CREATE TABLE script exists for this table (see Database-Providers-Plan.md chapter 2.1a) - verified
    /// directly against the live database (sqlite_master / PRAGMA table_info / index_list).
    /// </remarks>
    public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
    {
        public void Configure(EntityTypeBuilder<Profile> builder)
        {
            builder.ToTable("Profile", schema: "Users");

            builder.HasKey(e => e.UserId);

            //UserId matches AspNetUsers.Id and is always supplied by the caller when a profile is created - it is
            //not database-generated.
            builder.Property(e => e.UserId)
                .ValueGeneratedNever()
                .UseCollation("NOCASE");

            builder.Property(e => e.Navigation)
                .UseCollation("NOCASE");

            //AccountName carries no COLLATE NOCASE in the real schema, unlike Navigation - verified directly
            //against the live CREATE TABLE statement, not scaffold-inferred.

            builder.Property(e => e.CreatedDate).IsRequired();
            builder.Property(e => e.ModifiedDate).IsRequired();

            builder.HasIndex(e => e.Navigation).IsUnique();
            builder.HasIndex(e => e.AccountName).IsUnique();

            //Non-unique composite index on (UserId, AccountName). Since UserId is already the primary key (and
            //therefore unique on its own), this index adds no lookup capability beyond what the primary key
            //already provides. Reviewed as a possible consolidation candidate (same spirit as the Emoji/PageFile
            //redundant-index findings from the previous task), but it is not an exact duplicate of another index
            //- it covers a different column set - so it is kept as-is rather than dropped.
            builder.HasIndex(e => new { e.UserId, e.AccountName }, "idx_Profile_UserId_AccountName");
        }
    }
}
