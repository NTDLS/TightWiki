using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TightWiki.Data.EfCore.Postgres.Migrations.TightWikiDb
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "Users");

            migrationBuilder.EnsureSchema(
                name: "Config");

            migrationBuilder.EnsureSchema(
                name: "Pages");

            migrationBuilder.EnsureSchema(
                name: "DeletedPageRevisions");

            migrationBuilder.EnsureSchema(
                name: "DeletedPages");

            migrationBuilder.EnsureSchema(
                name: "Emoji");

            migrationBuilder.EnsureSchema(
                name: "Logging");

            migrationBuilder.EnsureSchema(
                name: "Statistics");

            migrationBuilder.AlterDatabase()
                .Annotation("Npgsql:PostgresExtension:citext", ",,");

            migrationBuilder.CreateTable(
                name: "AdminPwCheck",
                schema: "Users",
                columns: table => new
                {
                    Value = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationEntry",
                schema: "Config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    ConfigurationGroupId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true),
                    DataTypeId = table.Column<int>(type: "integer", nullable: false),
                    Description = table.Column<string>(type: "citext", nullable: true),
                    IsEncrypted = table.Column<bool>(type: "boolean", nullable: false),
                    IsRequired = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationEntry", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ConfigurationGroup",
                schema: "Config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Description = table.Column<string>(type: "citext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ConfigurationGroup", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CryptoCheck",
                schema: "Config",
                columns: table => new
                {
                    Content = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                });

            migrationBuilder.CreateTable(
                name: "DataType",
                schema: "Config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DataType", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Emoji",
                schema: "Emoji",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    ImageData = table.Column<byte[]>(type: "bytea", nullable: true),
                    MimeType = table.Column<string>(type: "citext", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Emoji", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "EmojiCategory",
                schema: "Emoji",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    EmojiId = table.Column<int>(type: "integer", nullable: false),
                    Category = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EmojiCategory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MenuItem",
                schema: "Config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Link = table.Column<string>(type: "citext", nullable: false),
                    Ordinal = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MenuItem", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageFile",
                schema: "DeletedPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Navigation = table.Column<string>(type: "citext", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageFile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PageProcessingInstruction",
                schema: "DeletedPages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Instruction = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageProcessingInstruction", x => new { x.PageId, x.Instruction });
                });

            migrationBuilder.CreateTable(
                name: "PageRevisionAttachment",
                schema: "DeletedPageRevisions",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    PageFileId = table.Column<int>(type: "integer", nullable: false),
                    FileRevision = table.Column<int>(type: "integer", nullable: false),
                    PageRevision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageRevisionAttachment", x => new { x.PageId, x.PageFileId, x.FileRevision, x.PageRevision });
                });

            migrationBuilder.CreateTable(
                name: "PageRevisionAttachment",
                schema: "DeletedPages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    PageFileId = table.Column<int>(type: "integer", nullable: false),
                    FileRevision = table.Column<int>(type: "integer", nullable: false),
                    PageRevision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageRevisionAttachment1", x => new { x.PageId, x.PageFileId, x.FileRevision, x.PageRevision });
                });

            migrationBuilder.CreateTable(
                name: "PageTag",
                schema: "DeletedPages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Tag = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageTag", x => new { x.PageId, x.Tag });
                });

            migrationBuilder.CreateTable(
                name: "PageToken",
                schema: "DeletedPages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "citext", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    DoubleMetaphone = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageToken", x => new { x.PageId, x.Token });
                });

            migrationBuilder.CreateTable(
                name: "Permission",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Permission", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PermissionDisposition",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PermissionDisposition", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Profile",
                schema: "Users",
                columns: table => new
                {
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Navigation = table.Column<string>(type: "citext", nullable: true),
                    AccountName = table.Column<string>(type: "text", nullable: true),
                    Biography = table.Column<string>(type: "text", nullable: true),
                    Avatar = table.Column<byte[]>(type: "bytea", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    AvatarContentType = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Profile", x => x.UserId);
                });

            migrationBuilder.CreateTable(
                name: "Role",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Description = table.Column<string>(type: "citext", nullable: true),
                    IsBuiltIn = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Role", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Severity",
                schema: "Logging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Severity", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Theme",
                schema: "Config",
                columns: table => new
                {
                    Name = table.Column<string>(type: "text", nullable: false),
                    DelimitedFiles = table.Column<string>(type: "text", nullable: false),
                    ClassNavBar = table.Column<string>(type: "text", nullable: false),
                    ClassNavLink = table.Column<string>(type: "text", nullable: false),
                    ClassDropdown = table.Column<string>(type: "text", nullable: false),
                    ClassBranding = table.Column<string>(type: "text", nullable: false),
                    EditorTheme = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Theme", x => x.Name);
                });

            migrationBuilder.CreateTable(
                name: "VersionState",
                schema: "Config",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_VersionState", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AccountPermission",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    PermissionId = table.Column<int>(type: "integer", nullable: false),
                    Namespace = table.Column<string>(type: "text", nullable: true),
                    PageId = table.Column<string>(type: "text", nullable: true),
                    PermissionDispositionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountPermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountPermission_PermissionDisposition_PermissionDispositi~",
                        column: x => x.PermissionDispositionId,
                        principalSchema: "Users",
                        principalTable: "PermissionDisposition",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccountPermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "Users",
                        principalTable: "Permission",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AccountPermission_Profile_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "CurrentPageEditors",
                schema: "Pages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountName = table.Column<string>(type: "text", nullable: false),
                    UTCDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CurrentPageEditors", x => new { x.PageId, x.UserId });
                    table.ForeignKey(
                        name: "FK_CurrentPageEditors_Profile_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "DeletionMeta",
                schema: "DeletedPageRevisions",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletionMeta", x => new { x.PageId, x.Revision });
                    table.ForeignKey(
                        name: "FK_DeletionMeta_Profile_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "DeletionMeta",
                schema: "DeletedPages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    DeletedByUserId = table.Column<Guid>(type: "uuid", nullable: true),
                    DeletedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DeletionMeta1", x => x.PageId);
                    table.ForeignKey(
                        name: "FK_DeletionMeta_Profile_DeletedByUserId",
                        column: x => x.DeletedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Page",
                schema: "DeletedPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Namespace = table.Column<string>(type: "citext", nullable: false),
                    Navigation = table.Column<string>(type: "citext", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Page", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Page_Profile_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Page_Profile_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "Page",
                schema: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Namespace = table.Column<string>(type: "citext", nullable: false),
                    Navigation = table.Column<string>(type: "citext", nullable: false),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Page1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Page_Profile_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_Page_Profile_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PageComment",
                schema: "DeletedPages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false),
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageComment", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageComment_Profile_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PageFileRevision",
                schema: "DeletedPages",
                columns: table => new
                {
                    PageFileId = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "citext", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    DataHash = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageFileRevision", x => new { x.PageFileId, x.Revision });
                    table.ForeignKey(
                        name: "FK_PageFileRevision_Profile_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PageRevision",
                schema: "DeletedPageRevisions",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Namespace = table.Column<string>(type: "citext", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ChangeSummary = table.Column<string>(type: "text", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataHash = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageRevision", x => new { x.PageId, x.Revision });
                    table.ForeignKey(
                        name: "FK_PageRevision_Profile_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PageRevision",
                schema: "DeletedPages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Namespace = table.Column<string>(type: "citext", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ChangeSummary = table.Column<string>(type: "text", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataHash = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageRevision1", x => new { x.PageId, x.Revision });
                    table.ForeignKey(
                        name: "FK_PageRevision_Profile_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PageRevision",
                schema: "Pages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Namespace = table.Column<string>(type: "citext", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false),
                    ChangeSummary = table.Column<string>(type: "text", nullable: true),
                    ModifiedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModifiedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DataHash = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageRevision2", x => new { x.PageId, x.Revision });
                    table.ForeignKey(
                        name: "FK_PageRevision_Profile_ModifiedByUserId",
                        column: x => x.ModifiedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "AccountRole",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    RoleId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AccountRole", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AccountRole_Profile_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                    table.ForeignKey(
                        name: "FK_AccountRole_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Users",
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "RolePermission",
                schema: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<int>(type: "integer", nullable: false),
                    PermissionId = table.Column<int>(type: "integer", nullable: false),
                    Namespace = table.Column<string>(type: "text", nullable: true),
                    PageId = table.Column<string>(type: "text", nullable: true),
                    PermissionDispositionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RolePermission", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RolePermission_PermissionDisposition_PermissionDispositionId",
                        column: x => x.PermissionDispositionId,
                        principalSchema: "Users",
                        principalTable: "PermissionDisposition",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermission_Permission_PermissionId",
                        column: x => x.PermissionId,
                        principalSchema: "Users",
                        principalTable: "Permission",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_RolePermission_Role_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "Users",
                        principalTable: "Role",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "Log",
                schema: "Logging",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    SeverityId = table.Column<int>(type: "integer", nullable: true),
                    Text = table.Column<string>(type: "text", nullable: true),
                    ExceptionText = table.Column<string>(type: "text", nullable: true),
                    StackTrace = table.Column<string>(type: "text", nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Log", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Log_Severity_SeverityId",
                        column: x => x.SeverityId,
                        principalSchema: "Logging",
                        principalTable: "Severity",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "FeatureTemplate",
                schema: "Pages",
                columns: table => new
                {
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Type = table.Column<string>(type: "citext", nullable: false),
                    PageId = table.Column<int>(type: "integer", nullable: true),
                    Description = table.Column<string>(type: "text", nullable: true),
                    TemplateText = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FeatureTemplate", x => new { x.Name, x.Type });
                    table.ForeignKey(
                        name: "FK_FeatureTemplate_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PageComment",
                schema: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    Body = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageComment1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageComment_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PageComment_Profile_UserId",
                        column: x => x.UserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PageFile",
                schema: "Pages",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Name = table.Column<string>(type: "citext", nullable: false),
                    Navigation = table.Column<string>(type: "citext", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageFile1", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageFile_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PageProcessingInstruction",
                schema: "Pages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Instruction = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageProcessingInstruction1", x => new { x.PageId, x.Instruction });
                    table.ForeignKey(
                        name: "FK_PageProcessingInstruction_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PageReference",
                schema: "Pages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    ReferencesPageNavigation = table.Column<string>(type: "citext", nullable: false),
                    ReferencesPageName = table.Column<string>(type: "citext", nullable: false),
                    ReferencesPageId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageReference", x => new { x.PageId, x.ReferencesPageNavigation });
                    table.ForeignKey(
                        name: "FK_PageReference_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PageReference_Page_ReferencesPageId",
                        column: x => x.ReferencesPageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PageStatistics",
                schema: "Statistics",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    LastCompileDateTime = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    TotalCompilationCount = table.Column<int>(type: "integer", nullable: false),
                    LastWikifyTimeMs = table.Column<double>(type: "double precision", nullable: true),
                    TotalWikifyTimeMs = table.Column<double>(type: "double precision", nullable: true),
                    LastMatchCount = table.Column<int>(type: "integer", nullable: true),
                    LastErrorCount = table.Column<int>(type: "integer", nullable: true),
                    LastOutgoingLinkCount = table.Column<int>(type: "integer", nullable: true),
                    LastTagCount = table.Column<int>(type: "integer", nullable: true),
                    LastProcessedBodySize = table.Column<int>(type: "integer", nullable: true),
                    LastBodySize = table.Column<int>(type: "integer", nullable: true),
                    TotalViewCount = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageStatistics", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PageStatistics_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PageTag",
                schema: "Pages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Tag = table.Column<string>(type: "citext", nullable: false),
                    Navigation = table.Column<string>(type: "citext", nullable: false, defaultValue: "")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageTag1", x => new { x.PageId, x.Tag });
                    table.ForeignKey(
                        name: "FK_PageTag_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PageToken",
                schema: "Pages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    Token = table.Column<string>(type: "citext", nullable: false),
                    Weight = table.Column<double>(type: "double precision", nullable: false),
                    DoubleMetaphone = table.Column<string>(type: "citext", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageToken1", x => new { x.PageId, x.Token });
                    table.ForeignKey(
                        name: "FK_PageToken_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "PageFileRevision",
                schema: "Pages",
                columns: table => new
                {
                    PageFileId = table.Column<int>(type: "integer", nullable: false),
                    Revision = table.Column<int>(type: "integer", nullable: false),
                    ContentType = table.Column<string>(type: "citext", nullable: false),
                    Size = table.Column<long>(type: "bigint", nullable: false),
                    CreatedByUserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    Data = table.Column<byte[]>(type: "bytea", nullable: false),
                    DataHash = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageFileRevision1", x => new { x.PageFileId, x.Revision });
                    table.ForeignKey(
                        name: "FK_PageFileRevision_PageFile_PageFileId",
                        column: x => x.PageFileId,
                        principalSchema: "Pages",
                        principalTable: "PageFile",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PageFileRevision_Profile_CreatedByUserId",
                        column: x => x.CreatedByUserId,
                        principalSchema: "Users",
                        principalTable: "Profile",
                        principalColumn: "UserId");
                });

            migrationBuilder.CreateTable(
                name: "PageRevisionAttachment",
                schema: "Pages",
                columns: table => new
                {
                    PageId = table.Column<int>(type: "integer", nullable: false),
                    PageFileId = table.Column<int>(type: "integer", nullable: false),
                    FileRevision = table.Column<int>(type: "integer", nullable: false),
                    PageRevision = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PageRevisionAttachment2", x => new { x.PageId, x.PageFileId, x.FileRevision, x.PageRevision });
                    table.ForeignKey(
                        name: "FK_PageRevisionAttachment_PageFile_PageFileId",
                        column: x => x.PageFileId,
                        principalSchema: "Pages",
                        principalTable: "PageFile",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_PageRevisionAttachment_Page_PageId",
                        column: x => x.PageId,
                        principalSchema: "Pages",
                        principalTable: "Page",
                        principalColumn: "Id");
                });

            migrationBuilder.InsertData(
                schema: "Config",
                table: "DataType",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Integer" },
                    { 2, "String" },
                    { 3, "Boolean" },
                    { 4, "Decimal" },
                    { 5, "Text" }
                });

            migrationBuilder.InsertData(
                schema: "Users",
                table: "Permission",
                columns: new[] { "Id", "Description", "Name" },
                values: new object[,]
                {
                    { 1, "User or role can create pages.", "Create" },
                    { 2, "User or role can delete page or within namespace.", "Delete" },
                    { 3, "User or role can edit page or within namespace.", "Edit" },
                    { 4, "User or role can moderate page or within namespace, such as editing protected pages and reverting changes.", "Moderate" },
                    { 5, "User or role can read page or within namespace.", "Read" }
                });

            migrationBuilder.InsertData(
                schema: "Users",
                table: "PermissionDisposition",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Allow" },
                    { 2, "Deny" }
                });

            migrationBuilder.InsertData(
                schema: "Users",
                table: "Role",
                columns: new[] { "Id", "Description", "IsBuiltIn", "Name" },
                values: new object[,]
                {
                    { 1, "Administrators can do anything. Add, edit, delete, etc.", true, "Administrator" },
                    { 2, "Read-only user with a profile.", true, "Member" },
                    { 3, "Contributor can add and edit unprotected pages.", true, "Contributor" },
                    { 4, "Moderators can add, edit, and delete pages - including protected pages.", true, "Moderator" },
                    { 5, "Role applied to users who are not logged in.", true, "Anonymous" }
                });

            migrationBuilder.InsertData(
                schema: "Logging",
                table: "Severity",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "Trace" },
                    { 2, "Debug" },
                    { 3, "Information" },
                    { 4, "Warning" },
                    { 5, "Error" },
                    { 6, "Critical" },
                    { 7, "None" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AccountPermission_PermissionDispositionId",
                schema: "Users",
                table: "AccountPermission",
                column: "PermissionDispositionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPermission_PermissionId",
                schema: "Users",
                table: "AccountPermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountPermission_UserId",
                schema: "Users",
                table: "AccountPermission",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRole_RoleId",
                schema: "Users",
                table: "AccountRole",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_AccountRole_UserId_RoleId",
                schema: "Users",
                table: "AccountRole",
                columns: new[] { "UserId", "RoleId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationEntry_ConfigurationGroupId_Name",
                schema: "Config",
                table: "ConfigurationEntry",
                columns: new[] { "ConfigurationGroupId", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ConfigurationGroup_Name",
                schema: "Config",
                table: "ConfigurationGroup",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CurrentPageEditors_UserId",
                schema: "Pages",
                table: "CurrentPageEditors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_DataType_Name",
                schema: "Config",
                table: "DataType",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DeletionMeta_DeletedByUserId",
                schema: "DeletedPageRevisions",
                table: "DeletionMeta",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_DeletionMeta_DeletedByUserId1",
                schema: "DeletedPages",
                table: "DeletionMeta",
                column: "DeletedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Emoji",
                schema: "Emoji",
                table: "Emoji",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_EmojiCategory",
                schema: "Emoji",
                table: "EmojiCategory",
                columns: new[] { "EmojiId", "Category" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FeatureTemplate_PageId",
                schema: "Pages",
                table: "FeatureTemplate",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_Log_SeverityId",
                schema: "Logging",
                table: "Log",
                column: "SeverityId");

            migrationBuilder.CreateIndex(
                name: "IX_Page_CreatedByUserId",
                schema: "DeletedPages",
                table: "Page",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Page_ModifiedByUserId",
                schema: "DeletedPages",
                table: "Page",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Page_CreatedByUserId1",
                schema: "Pages",
                table: "Page",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Page_ModifiedByUserId1",
                schema: "Pages",
                table: "Page",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Page_Name",
                schema: "Pages",
                table: "Page",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Page_Navigation",
                schema: "Pages",
                table: "Page",
                column: "Navigation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageComment_UserId",
                schema: "DeletedPages",
                table: "PageComment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageComment_PageId",
                schema: "Pages",
                table: "PageComment",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "IX_PageComment_UserId1",
                schema: "Pages",
                table: "PageComment",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageFile_PageId_Name_Revision",
                schema: "Pages",
                table: "PageFile",
                columns: new[] { "PageId", "Name", "Revision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PageFileRevision_CreatedByUserId",
                schema: "DeletedPages",
                table: "PageFileRevision",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageFileRevision_CreatedByUserId1",
                schema: "Pages",
                table: "PageFileRevision",
                column: "CreatedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageReference_ReferencesPageId",
                schema: "Pages",
                table: "PageReference",
                column: "ReferencesPageId");

            migrationBuilder.CreateIndex(
                name: "IX_PageRevision_ModifiedByUserId",
                schema: "DeletedPageRevisions",
                table: "PageRevision",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageRevision_ModifiedByUserId1",
                schema: "DeletedPages",
                table: "PageRevision",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageRevision_ModifiedByUserId2",
                schema: "Pages",
                table: "PageRevision",
                column: "ModifiedByUserId");

            migrationBuilder.CreateIndex(
                name: "IX_PageRevisionAttachment_PageFileId",
                schema: "Pages",
                table: "PageRevisionAttachment",
                column: "PageFileId");

            migrationBuilder.CreateIndex(
                name: "IX_PageRevisionAttachment_PageId_PageFileId_PageRevision",
                schema: "Pages",
                table: "PageRevisionAttachment",
                columns: new[] { "PageId", "PageFileId", "PageRevision" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CompilationStatistics_PageId",
                schema: "Statistics",
                table: "PageStatistics",
                column: "PageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_PageToken_DoubleMetaphone_PageId_Weight",
                schema: "Pages",
                table: "PageToken",
                columns: new[] { "DoubleMetaphone", "PageId", "Weight" });

            migrationBuilder.CreateIndex(
                name: "idx_PageToken_PageId",
                schema: "Pages",
                table: "PageToken",
                column: "PageId");

            migrationBuilder.CreateIndex(
                name: "idx_PageToken_Token_PageId_Weight",
                schema: "Pages",
                table: "PageToken",
                columns: new[] { "Token", "PageId", "Weight" });

            migrationBuilder.CreateIndex(
                name: "IX_Permission_Name",
                schema: "Users",
                table: "Permission",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PermissionDisposition_Name",
                schema: "Users",
                table: "PermissionDisposition",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_Profile_UserId_AccountName",
                schema: "Users",
                table: "Profile",
                columns: new[] { "UserId", "AccountName" });

            migrationBuilder.CreateIndex(
                name: "IX_Profile_AccountName",
                schema: "Users",
                table: "Profile",
                column: "AccountName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Profile_Navigation",
                schema: "Users",
                table: "Profile",
                column: "Navigation",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Role_Name",
                schema: "Users",
                table: "Role",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionDispositionId",
                schema: "Users",
                table: "RolePermission",
                column: "PermissionDispositionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_PermissionId",
                schema: "Users",
                table: "RolePermission",
                column: "PermissionId");

            migrationBuilder.CreateIndex(
                name: "IX_RolePermission_RoleId_PermissionId_Namespace_PageId_Permiss~",
                schema: "Users",
                table: "RolePermission",
                columns: new[] { "RoleId", "PermissionId", "Namespace", "PageId", "PermissionDispositionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Severity_Name",
                schema: "Logging",
                table: "Severity",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_VersionState_Name",
                schema: "Config",
                table: "VersionState",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AccountPermission",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "AccountRole",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "AdminPwCheck",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "ConfigurationEntry",
                schema: "Config");

            migrationBuilder.DropTable(
                name: "ConfigurationGroup",
                schema: "Config");

            migrationBuilder.DropTable(
                name: "CryptoCheck",
                schema: "Config");

            migrationBuilder.DropTable(
                name: "CurrentPageEditors",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "DataType",
                schema: "Config");

            migrationBuilder.DropTable(
                name: "DeletionMeta",
                schema: "DeletedPageRevisions");

            migrationBuilder.DropTable(
                name: "DeletionMeta",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "Emoji",
                schema: "Emoji");

            migrationBuilder.DropTable(
                name: "EmojiCategory",
                schema: "Emoji");

            migrationBuilder.DropTable(
                name: "FeatureTemplate",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "Log",
                schema: "Logging");

            migrationBuilder.DropTable(
                name: "MenuItem",
                schema: "Config");

            migrationBuilder.DropTable(
                name: "Page",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageComment",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageComment",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "PageFile",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageFileRevision",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageFileRevision",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "PageProcessingInstruction",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageProcessingInstruction",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "PageReference",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "PageRevision",
                schema: "DeletedPageRevisions");

            migrationBuilder.DropTable(
                name: "PageRevision",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageRevision",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "PageRevisionAttachment",
                schema: "DeletedPageRevisions");

            migrationBuilder.DropTable(
                name: "PageRevisionAttachment",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageRevisionAttachment",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "PageStatistics",
                schema: "Statistics");

            migrationBuilder.DropTable(
                name: "PageTag",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageTag",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "PageToken",
                schema: "DeletedPages");

            migrationBuilder.DropTable(
                name: "PageToken",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "RolePermission",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "Theme",
                schema: "Config");

            migrationBuilder.DropTable(
                name: "VersionState",
                schema: "Config");

            migrationBuilder.DropTable(
                name: "Severity",
                schema: "Logging");

            migrationBuilder.DropTable(
                name: "PageFile",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "PermissionDisposition",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "Permission",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "Role",
                schema: "Users");

            migrationBuilder.DropTable(
                name: "Page",
                schema: "Pages");

            migrationBuilder.DropTable(
                name: "Profile",
                schema: "Users");
        }
    }
}
