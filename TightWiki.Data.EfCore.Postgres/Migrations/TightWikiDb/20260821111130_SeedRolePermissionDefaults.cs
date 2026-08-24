using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TightWiki.Data.EfCore.Postgres.Migrations.TightWikiDb
{
    /// <inheritdoc />
    public partial class SeedRolePermissionDefaults : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                schema: "Users",
                table: "RolePermission",
                columns: new[] { "Id", "Namespace", "PageId", "PermissionDispositionId", "PermissionId", "RoleId" },
                values: new object[,]
                {
                    { 1, null, "*", 1, 1, 1 },
                    { 2, "*", null, 1, 1, 1 },
                    { 3, null, "*", 1, 2, 1 },
                    { 4, "*", null, 1, 2, 1 },
                    { 5, null, "*", 1, 3, 1 },
                    { 6, "*", null, 1, 3, 1 },
                    { 7, null, "*", 1, 4, 1 },
                    { 8, "*", null, 1, 4, 1 },
                    { 9, null, "*", 1, 5, 1 },
                    { 10, "*", null, 1, 5, 1 },
                    { 11, null, "*", 1, 5, 5 },
                    { 12, "*", null, 1, 5, 5 },
                    { 13, null, "*", 1, 5, 2 },
                    { 14, "*", null, 1, 5, 2 },
                    { 15, null, "*", 1, 2, 4 },
                    { 16, "*", null, 1, 2, 4 },
                    { 17, null, "*", 1, 3, 4 },
                    { 18, "*", null, 1, 3, 4 },
                    { 19, null, "*", 1, 4, 4 },
                    { 20, "*", null, 1, 4, 4 },
                    { 21, null, "*", 1, 5, 4 },
                    { 22, "*", null, 1, 5, 4 },
                    { 23, null, "*", 1, 3, 3 },
                    { 24, "*", null, 1, 3, 3 },
                    { 25, null, "*", 1, 5, 3 },
                    { 26, "*", null, 1, 5, 3 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 10);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 11);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 12);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 13);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 14);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 15);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 16);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 17);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 18);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 19);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 20);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 21);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 22);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 23);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 24);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 25);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "RolePermission",
                keyColumn: "Id",
                keyValue: 26);
        }
    }
}
