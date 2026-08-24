using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TightWiki.Data.EfCore.Entities.Config;

namespace TightWiki.Data.EfCore.Configurations.Config
{
    /// <summary>
    /// Fluent configuration for <see cref="Theme"/> (Config.Theme).
    /// </summary>
    public class ThemeConfiguration : IEntityTypeConfiguration<Theme>
    {
        public void Configure(EntityTypeBuilder<Theme> builder)
        {
            builder.ToTable("Theme", schema: "Config");

            //Name is the real primary key - there is no surrogate integer identifier.
            builder.HasKey(e => e.Name);

            builder.Property(e => e.DelimitedFiles).IsRequired();
            builder.Property(e => e.ClassNavBar).IsRequired();
            builder.Property(e => e.ClassNavLink).IsRequired();
            builder.Property(e => e.ClassDropdown).IsRequired();
            builder.Property(e => e.ClassBranding).IsRequired();
            builder.Property(e => e.EditorTheme).IsRequired();
        }
    }
}
