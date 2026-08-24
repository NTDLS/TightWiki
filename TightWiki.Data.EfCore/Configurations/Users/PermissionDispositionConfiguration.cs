using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="PermissionDisposition"/> (Users.PermissionDisposition).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^001^Users^PermissionDisposition.sql (this table
    /// has a real CREATE TABLE script, unlike most of the Users schema).
    /// </remarks>
    public class PermissionDispositionConfiguration : IEntityTypeConfiguration<PermissionDisposition>
    {
        public void Configure(EntityTypeBuilder<PermissionDisposition> builder)
        {
            builder.ToTable("PermissionDisposition", schema: "Users");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            //The real schema declares "Id" as both NOT NULL UNIQUE and PRIMARY KEY(Id AUTOINCREMENT), which
            //produces a second, functionally-redundant autoindex on Id alongside the one already implied by the
            //primary key itself (same idiom repeated on Permission/AccountRole/AccountPermission/RolePermission
            //below). Not reproduced here - HasKey(e => e.Id) already enforces the same uniqueness.
            builder.HasIndex(e => e.Name)
                .IsUnique();

            //Static lookup, seeded via HasData per Database-Providers-Plan.md chapter 4.6a ("Statické číselníky
            //-> HasData v migraci"). The seed script inserts via "INSERT ... SELECT ... UNION SELECT ...", which
            //SQLite deduplicates/sorts rather than preserving literal insert order - the Ids below were
            //cross-checked against the live Data/users.db (Allow=1, Deny=2; here that happens to match both the
            //script's literal order and alphabetical order).
            builder.HasData(
                new PermissionDisposition { Id = 1, Name = "Allow" },
                new PermissionDisposition { Id = 2, Name = "Deny" });
        }
    }
}
