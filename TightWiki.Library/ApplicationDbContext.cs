using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Library
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<IdentityUser>(options)
    {
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            //Database-Providers-Plan.md chapter 4.1.1: "Identity tabulky jdou do schématu Users (přes
            //builder.HasDefaultSchema("Users") v OnModelCreating); pro SQLite provider se schéma ignoruje a
            //chování zůstává dnešní." EF Core's SQLite provider has no notion of schemas and silently ignores
            //this call, so the SQLite users.db table layout (AspNetUsers, AspNetRoles, ...) is unaffected. For
            //SQL Server/Postgres it places the Identity tables alongside the rest of TightWiki.Data.EfCore's
            //"Users" schema (chapter 4.3), which is required for the cross-schema joins TightWiki's SQL already
            //performs against AspNetUsers (chapter 2.1c).
            builder.HasDefaultSchema("Users");
        }
    }
}
