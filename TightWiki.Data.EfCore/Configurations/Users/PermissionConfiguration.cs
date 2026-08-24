using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="Permission"/> (Users.Permission).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^003^Users^Permission.sql (this table has a real
    /// CREATE TABLE script, unlike most of the Users schema).
    /// </remarks>
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("Permission", schema: "Users");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            //Description carries no COLLATE NOCASE in the real schema, unlike Name.

            //Same redundant Id-unique-autoindex idiom documented in PermissionDispositionConfiguration - not
            //reproduced here.
            builder.HasIndex(e => e.Name)
                .IsUnique();

            //Static lookup, seeded via HasData per Database-Providers-Plan.md chapter 4.6a ("Statické číselníky
            //-> HasData v migraci"). The seed script inserts via "INSERT ... SELECT ... UNION SELECT ...", which
            //SQLite deduplicates/sorts rather than preserving literal insert order (script order is Read, Edit,
            //Delete, Moderate, Create) - the Ids/order below were cross-checked against the live Data/users.db,
            //which comes out alphabetically sorted (Create=1 .. Read=5). Descriptions copied verbatim from the
            //script.
            builder.HasData(
                new Permission { Id = 1, Name = "Create", Description = "User or role can create pages." },
                new Permission { Id = 2, Name = "Delete", Description = "User or role can delete page or within namespace." },
                new Permission { Id = 3, Name = "Edit", Description = "User or role can edit page or within namespace." },
                new Permission { Id = 4, Name = "Moderate", Description = "User or role can moderate page or within namespace, such as editing protected pages and reverting changes." },
                new Permission { Id = 5, Name = "Read", Description = "User or role can read page or within namespace." });
        }
    }
}
