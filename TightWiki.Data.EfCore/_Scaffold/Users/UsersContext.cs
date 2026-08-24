using System;
using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace TightWiki.Data.EfCore._Scaffold.Users;

public partial class UsersContext : DbContext
{
    public UsersContext(DbContextOptions<UsersContext> options)
        : base(options)
    {
    }

    public virtual DbSet<AccountPermission> AccountPermissions { get; set; }

    public virtual DbSet<AccountRole> AccountRoles { get; set; }

    public virtual DbSet<AdminPwCheck> AdminPwChecks { get; set; }

    public virtual DbSet<AspNetRole> AspNetRoles { get; set; }

    public virtual DbSet<AspNetRoleClaim> AspNetRoleClaims { get; set; }

    public virtual DbSet<AspNetUser> AspNetUsers { get; set; }

    public virtual DbSet<AspNetUserClaim> AspNetUserClaims { get; set; }

    public virtual DbSet<AspNetUserLogin> AspNetUserLogins { get; set; }

    public virtual DbSet<AspNetUserToken> AspNetUserTokens { get; set; }

    public virtual DbSet<Permission> Permissions { get; set; }

    public virtual DbSet<PermissionDisposition> PermissionDispositions { get; set; }

    public virtual DbSet<Profile> Profiles { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<RolePermission> RolePermissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AccountPermission>(entity =>
        {
            entity.ToTable("AccountPermission");

            entity.HasIndex(e => e.Id, "IX_AccountPermission_Id").IsUnique();

            entity.HasOne(d => d.PermissionDisposition).WithMany(p => p.AccountPermissions)
                .HasForeignKey(d => d.PermissionDispositionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Permission).WithMany(p => p.AccountPermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.AccountPermissions)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AccountRole>(entity =>
        {
            entity.ToTable("AccountRole");

            entity.HasIndex(e => e.Id, "IX_AccountRole_Id").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.RoleId }, "IX_AccountRole_UserId_RoleId").IsUnique();

            entity.Property(e => e.UserId).UseCollation("NOCASE");

            entity.HasOne(d => d.Role).WithMany(p => p.AccountRoles)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.User).WithMany(p => p.AccountRoles)
                .HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        modelBuilder.Entity<AdminPwCheck>(entity =>
        {
            entity
                .HasNoKey()
                .ToTable("AdminPwCheck");

            entity.Property(e => e.Value).HasColumnType("INT");
        });

        modelBuilder.Entity<AspNetRole>(entity =>
        {
            entity.HasIndex(e => e.NormalizedName, "RoleNameIndex").IsUnique();
        });

        modelBuilder.Entity<AspNetRoleClaim>(entity =>
        {
            entity.HasIndex(e => e.RoleId, "IX_AspNetRoleClaims_RoleId");

            entity.HasOne(d => d.Role).WithMany(p => p.AspNetRoleClaims).HasForeignKey(d => d.RoleId);
        });

        modelBuilder.Entity<AspNetUser>(entity =>
        {
            entity.HasIndex(e => e.NormalizedEmail, "EmailIndex");

            entity.HasIndex(e => e.NormalizedUserName, "UserNameIndex").IsUnique();

            entity.HasIndex(e => e.Email, "idx_AspNetUsers_Email");

            entity.Property(e => e.Id).UseCollation("NOCASE");
            entity.Property(e => e.Email).UseCollation("NOCASE");
            entity.Property(e => e.NormalizedUserName).UseCollation("NOCASE");
            entity.Property(e => e.UserName).UseCollation("NOCASE");

            entity.HasMany(d => d.Roles).WithMany(p => p.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "AspNetUserRole",
                    r => r.HasOne<AspNetRole>().WithMany().HasForeignKey("RoleId"),
                    l => l.HasOne<AspNetUser>().WithMany().HasForeignKey("UserId"),
                    j =>
                    {
                        j.HasKey("UserId", "RoleId");
                        j.ToTable("AspNetUserRoles");
                        j.HasIndex(new[] { "RoleId" }, "IX_AspNetUserRoles_RoleId");
                        j.IndexerProperty<string>("UserId").UseCollation("NOCASE");
                    });
        });

        modelBuilder.Entity<AspNetUserClaim>(entity =>
        {
            entity.HasIndex(e => e.UserId, "IX_AspNetUserClaims_UserId");

            entity.HasIndex(e => e.ClaimValue, "idx_AspNetUserClaims_ClaimValue");

            entity.HasIndex(e => new { e.UserId, e.ClaimType }, "idx_AspNetUserClaims_UserId_ClaimType");

            entity.Property(e => e.UserId).UseCollation("NOCASE");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserClaims).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserLogin>(entity =>
        {
            entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });

            entity.HasIndex(e => e.UserId, "IX_AspNetUserLogins_UserId");

            entity.Property(e => e.UserId).UseCollation("NOCASE");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserLogins).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<AspNetUserToken>(entity =>
        {
            entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });

            entity.Property(e => e.UserId).UseCollation("NOCASE");

            entity.HasOne(d => d.User).WithMany(p => p.AspNetUserTokens).HasForeignKey(d => d.UserId);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.ToTable("Permission");

            entity.HasIndex(e => e.Id, "IX_Permission_Id").IsUnique();

            entity.HasIndex(e => e.Name, "IX_Permission_Name").IsUnique();

            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        modelBuilder.Entity<PermissionDisposition>(entity =>
        {
            entity.ToTable("PermissionDisposition");

            entity.HasIndex(e => e.Id, "IX_PermissionDisposition_Id").IsUnique();

            entity.HasIndex(e => e.Name, "IX_PermissionDisposition_Name").IsUnique();

            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        modelBuilder.Entity<Profile>(entity =>
        {
            entity.HasKey(e => e.UserId);

            entity.ToTable("Profile");

            entity.HasIndex(e => e.AccountName, "IX_Profile_AccountName").IsUnique();

            entity.HasIndex(e => e.Navigation, "IX_Profile_Navigation").IsUnique();

            entity.HasIndex(e => new { e.UserId, e.AccountName }, "idx_Profile_UserId_AccountName");

            entity.Property(e => e.UserId)
                .ValueGeneratedNever()
                .UseCollation("NOCASE");
            entity.Property(e => e.Navigation).UseCollation("NOCASE");
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.ToTable("Role");

            entity.HasIndex(e => e.Name, "IX_Role_Name").IsUnique();

            entity.Property(e => e.Description).UseCollation("NOCASE");
            entity.Property(e => e.IsBuiltIn).HasDefaultValue(1);
            entity.Property(e => e.Name).UseCollation("NOCASE");
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.ToTable("RolePermission");

            entity.HasIndex(e => e.Id, "IX_RolePermission_Id").IsUnique();

            entity.HasIndex(e => new { e.RoleId, e.PermissionId, e.Namespace, e.PageId, e.PermissionDispositionId }, "IX_RolePermission_RoleId_PermissionId_Namespace_PageId_PermissionDispositionId").IsUnique();

            entity.HasOne(d => d.PermissionDisposition).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionDispositionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Permission).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.PermissionId)
                .OnDelete(DeleteBehavior.ClientSetNull);

            entity.HasOne(d => d.Role).WithMany(p => p.RolePermissions)
                .HasForeignKey(d => d.RoleId)
                .OnDelete(DeleteBehavior.ClientSetNull);
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}
