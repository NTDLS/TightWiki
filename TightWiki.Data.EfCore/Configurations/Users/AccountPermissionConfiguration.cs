using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="AccountPermission"/> (Users.AccountPermission).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^005^Users^AccountPermission.sql (this table has a
    /// real CREATE TABLE script, unlike most of the Users schema).
    /// </remarks>
    public class AccountPermissionConfiguration : IEntityTypeConfiguration<AccountPermission>
    {
        public void Configure(EntityTypeBuilder<AccountPermission> builder)
        {
            builder.ToTable("AccountPermission", schema: "Users");

            builder.HasKey(e => e.Id);

            //UserId carries no COLLATE NOCASE in the real schema, unlike AccountRole.UserId - verified directly
            //against the live CREATE TABLE statement, not scaffold-inferred.
            builder.Property(e => e.UserId).IsRequired();

            //Same redundant Id-unique-autoindex idiom documented in PermissionDispositionConfiguration - not
            //reproduced here.

            //All three FKs are real, intra-schema FOREIGN KEY constraints in the live database (UserId references
            //Profile.UserId, not AspNetUsers.Id directly - Profile is the TightWiki-owned proxy for the Identity
            //user, see the remarks on Profile.UserId).
            builder.HasOne(e => e.Profile)
                .WithMany(e => e.AccountPermissions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.Permission)
                .WithMany(e => e.AccountPermissions)
                .HasForeignKey(e => e.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.PermissionDisposition)
                .WithMany(e => e.AccountPermissions)
                .HasForeignKey(e => e.PermissionDispositionId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
