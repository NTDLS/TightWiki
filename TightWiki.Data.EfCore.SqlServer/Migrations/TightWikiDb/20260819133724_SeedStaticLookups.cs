using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TightWiki.Data.EfCore.SqlServer.Migrations.TightWikiDb
{
    /// <inheritdoc />
    public partial class SeedStaticLookups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                schema: "Config",
                table: "DataType",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "Config",
                table: "DataType",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "Config",
                table: "DataType",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "Config",
                table: "DataType",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "Config",
                table: "DataType",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Permission",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Permission",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Permission",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Permission",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Permission",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "PermissionDisposition",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "PermissionDisposition",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Role",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Role",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Role",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Role",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "Users",
                table: "Role",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "Logging",
                table: "Severity",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                schema: "Logging",
                table: "Severity",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                schema: "Logging",
                table: "Severity",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                schema: "Logging",
                table: "Severity",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                schema: "Logging",
                table: "Severity",
                keyColumn: "Id",
                keyValue: 5);

            migrationBuilder.DeleteData(
                schema: "Logging",
                table: "Severity",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                schema: "Logging",
                table: "Severity",
                keyColumn: "Id",
                keyValue: 7);
        }
    }
}
