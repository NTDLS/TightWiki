using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Users;

namespace TightWiki.Data.EfCore.Configurations.Users
{
    /// <summary>
    /// Fluent configuration for <see cref="AdminPwCheck"/> (Users.AdminPwCheck).
    /// </summary>
    public class AdminPwCheckConfiguration : IEntityTypeConfiguration<AdminPwCheck>
    {
        public void Configure(EntityTypeBuilder<AdminPwCheck> builder)
        {
            builder.ToTable("AdminPwCheck", schema: "Users");

            //No primary key in the real schema - a bare, unconstrained single-column table used as a status
            //flag (see the remarks on the entity itself).
            builder.HasNoKey();
        }
    }
}
