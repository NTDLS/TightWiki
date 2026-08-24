using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="Role"/> (Users.Role).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^002^Users^Role.sql. Unlike the other tables with
    /// a real CREATE TABLE script in the Users schema, this one only adds a column (IsBuiltIn) to a table that
    /// already existed before 2.26.0 - the base Id/Name/Description columns predate the scripted history (see
    /// Database-Providers-Plan.md chapter 2.1a) and were verified against the live database instead.
    /// </remarks>
    public class RoleConfiguration : IEntityTypeConfiguration<Role>
    {
        public void Configure(EntityTypeBuilder<Role> builder)
        {
            builder.ToTable("Role", schema: "Users");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.Name)
                .IsRequired()
                .UseCollation("NOCASE");

            builder.Property(e => e.Description)
                .UseCollation("NOCASE");

            builder.Property(e => e.IsBuiltIn)
                .HasDefaultValue(true);

            builder.HasIndex(e => e.Name, "IX_Role_Name")
                .IsUnique();

            //Static lookup, seeded via HasData per Database-Providers-Plan.md chapter 4.6a ("Statické číselníky
            //-> HasData v migraci"), values/order taken verbatim from the INSERT in
            //Scripts/Initialization/Versions/2.26.0/^002^Users^Role.sql (a plain multi-row VALUES insert, so
            //literal order is preserved: Administrator, Member, Contributor, Moderator, Anonymous).
            //
            //Explicit Id decision: the script has no explicit Id column (relies on AUTOINCREMENT), and on the
            //live Data/users.db the resulting Ids are 1, 2, 3, 4, 9 - not contiguous, because "Anonymous" was
            //added by this same script well after the original 4 built-in roles and the gap reflects that one
            //particular installation's history (rows deleted/reused in between), not a deterministic invariant.
            //Every reference to a role elsewhere in the codebase resolves it by Name, never by a hardcoded Id
            //(see e.g. Scripts/Initialization/Versions/2.26.0/^007^Users^CreatePermissionDefaults.sql, which
            //looks up "SELECT R.Id FROM Role AS R WHERE R.Name = 'Administrator'" etc.), so the exact numeric Id
            //carries no semantic meaning as long as it is stable and unique. HasData requires an explicit,
            //reproducible Id, so this uses contiguous 1..5 in script order rather than reproducing the
            //install-specific gap.
            builder.HasData(
                new Role { Id = 1, Name = "Administrator", Description = "Administrators can do anything. Add, edit, delete, etc.", IsBuiltIn = true },
                new Role { Id = 2, Name = "Member", Description = "Read-only user with a profile.", IsBuiltIn = true },
                new Role { Id = 3, Name = "Contributor", Description = "Contributor can add and edit unprotected pages.", IsBuiltIn = true },
                new Role { Id = 4, Name = "Moderator", Description = "Moderators can add, edit, and delete pages - including protected pages.", IsBuiltIn = true },
                new Role { Id = 5, Name = "Anonymous", Description = "Role applied to users who are not logged in.", IsBuiltIn = true });
        }
    }
}
