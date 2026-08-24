using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="RolePermission"/> (Users.RolePermission).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^006^Users^RolePermission.sql (this table has a
    /// real CREATE TABLE script, unlike most of the Users schema).
    /// </remarks>
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            builder.ToTable("RolePermission", schema: "Users");

            builder.HasKey(e => e.Id);

            //Same "named in SQL but not honored by SQLite" situation as AccountRole above - the live database
            //resolves this composite constraint to an unnamed autoindex (sqlite_autoindex_RolePermission_2), not
            //the "IX_Unique" name given in the CREATE TABLE script.
            builder.HasIndex(e => new { e.RoleId, e.PermissionId, e.Namespace, e.PageId, e.PermissionDispositionId })
                .IsUnique();

            //Same redundant Id-unique-autoindex idiom documented in PermissionDispositionConfiguration - not
            //reproduced here.

            builder.HasOne(e => e.Role)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.Permission)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.PermissionDisposition)
                .WithMany(e => e.RolePermissions)
                .HasForeignKey(e => e.PermissionDispositionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            //Static lookup, seeded via HasData per Database-Providers-Plan.md chapter 4.6a ("Statické číselníky ->
            //HasData v migraci") - the same bucket as Role/Permission/PermissionDisposition above, even though the
            //plan's own enumeration of that bucket (chapter 4.6a) omits RolePermission by name. Unlike AccountRole
            //(genuinely per-installation - it needs a dynamically created admin UserId), every one of these rows is
            //a fixed grant from a built-in role to a built-in permission with no installation-specific value
            //anywhere in it, so there is no reason for it to be anything other than static, migration-baked data
            //like its three sibling lookups.
            //
            //Found missing during the phase 3.8 Postgres smoke test against a freshly migrated, empty database:
            //without these rows RolePermission is empty after seeding, so no role - not even Administrator - has
            //any grant, and even the very first anonymous request to the home page fails with "You do not have
            //permission to perform the action: Read" (PageController's permission check has nothing to allow
            //against). Confirmed the same missing-data class of bug reproduces identically on SQL Server (same
            //shared TightWiki.Data.EfCore model, same absent HasData), not something Postgres-specific.
            //
            //Values/order taken verbatim from the INSERT ... SELECT ... UNION SELECT statements in
            //Scripts/Initialization/Versions/2.26.0/^007^Users^CreatePermissionDefaults.sql: Administrator gets an
            //Allow for every permission, granted twice over (once scoped by PageId="*", once scoped by
            //Namespace="*" - the SQL's own two mirrored SELECTs, preserved here rather than collapsed, since that
            //is what every other role's grant below also does and what the live application's permission-lookup
            //queries expect to find two rows for). Anonymous/Member get Read only; Moderator gets
            //Read/Edit/Delete/Moderate; Contributor gets Read/Edit. Permission/Role/PermissionDisposition ids are
            //the ones assigned by this same class's sibling HasData blocks (PermissionConfiguration: Create=1,
            //Delete=2, Edit=3, Moderate=4, Read=5; RoleConfiguration: Administrator=1, Member=2, Contributor=3,
            //Moderator=4, Anonymous=5; PermissionDispositionConfiguration: Allow=1).
            builder.HasData(
                //Administrator - Allow, every permission, both scoping variants.
                new RolePermission { Id = 1, RoleId = 1, PermissionId = 1, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 2, RoleId = 1, PermissionId = 1, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                new RolePermission { Id = 3, RoleId = 1, PermissionId = 2, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 4, RoleId = 1, PermissionId = 2, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                new RolePermission { Id = 5, RoleId = 1, PermissionId = 3, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 6, RoleId = 1, PermissionId = 3, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                new RolePermission { Id = 7, RoleId = 1, PermissionId = 4, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 8, RoleId = 1, PermissionId = 4, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                new RolePermission { Id = 9, RoleId = 1, PermissionId = 5, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 10, RoleId = 1, PermissionId = 5, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                //Anonymous - Allow, Read only, both scoping variants.
                new RolePermission { Id = 11, RoleId = 5, PermissionId = 5, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 12, RoleId = 5, PermissionId = 5, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                //Member - Allow, Read only, both scoping variants.
                new RolePermission { Id = 13, RoleId = 2, PermissionId = 5, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 14, RoleId = 2, PermissionId = 5, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                //Moderator - Allow, Read/Edit/Delete/Moderate, both scoping variants.
                new RolePermission { Id = 15, RoleId = 4, PermissionId = 2, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 16, RoleId = 4, PermissionId = 2, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                new RolePermission { Id = 17, RoleId = 4, PermissionId = 3, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 18, RoleId = 4, PermissionId = 3, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                new RolePermission { Id = 19, RoleId = 4, PermissionId = 4, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 20, RoleId = 4, PermissionId = 4, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                new RolePermission { Id = 21, RoleId = 4, PermissionId = 5, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 22, RoleId = 4, PermissionId = 5, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                //Contributor - Allow, Read/Edit, both scoping variants.
                new RolePermission { Id = 23, RoleId = 3, PermissionId = 3, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 24, RoleId = 3, PermissionId = 3, Namespace = "*", PageId = null, PermissionDispositionId = 1 },
                new RolePermission { Id = 25, RoleId = 3, PermissionId = 5, Namespace = null, PageId = "*", PermissionDispositionId = 1 },
                new RolePermission { Id = 26, RoleId = 3, PermissionId = 5, Namespace = "*", PageId = null, PermissionDispositionId = 1 });
        }
    }
}
