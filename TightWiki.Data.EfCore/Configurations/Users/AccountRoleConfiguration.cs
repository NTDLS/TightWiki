using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="AccountRole"/> (Users.AccountRole).
    /// </summary>
    /// <remarks>
    /// Source of truth: Scripts/Initialization/Versions/2.26.0/^004^Users^AccountRole.sql (this table has a real
    /// CREATE TABLE script, unlike most of the Users schema).
    /// </remarks>
    public class AccountRoleConfiguration : IEntityTypeConfiguration<AccountRole>
    {
        public void Configure(EntityTypeBuilder<AccountRole> builder)
        {
            builder.ToTable("AccountRole", schema: "Users");

            builder.HasKey(e => e.Id);

            builder.Property(e => e.UserId)
                .IsRequired()
                .UseCollation("NOCASE");

            //The CREATE TABLE script names this constraint "IX_Unique", but SQLite does not actually honor a
            //CONSTRAINT name for an inline UNIQUE table constraint - the live database resolves it to an unnamed
            //autoindex (sqlite_autoindex_AccountRole_2), same as RolePermission's identically-named constraint
            //below. There is no real named index to reproduce, so this is left unnamed.
            builder.HasIndex(e => new { e.UserId, e.RoleId })
                .IsUnique();

            //Same redundant Id-unique-autoindex idiom documented in PermissionDispositionConfiguration - not
            //reproduced here.

            //Both FKs are real, intra-schema FOREIGN KEY constraints in the live database (UserId references
            //Profile.UserId, not AspNetUsers.Id directly - Profile is the TightWiki-owned proxy for the Identity
            //user, see the remarks on Profile.UserId).
            builder.HasOne(e => e.Profile)
                .WithMany(e => e.AccountRoles)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            builder.HasOne(e => e.Role)
                .WithMany(e => e.AccountRoles)
                .HasForeignKey(e => e.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        }
    }
}
