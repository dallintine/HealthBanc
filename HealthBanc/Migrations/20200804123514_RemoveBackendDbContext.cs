using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class RemoveBackendDbContext : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UniqueUsername",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { 6, null, "Super-Administrator", "SUPER-ADMINISTRATOR" },
                    { 7, null, "Administrator", "ADMINISTRATOR" },
                    { 8, null, "Technical-Support", "TECHNICAL-SUPPORT" },
                    { 9, null, "Analyst", "ANALYST" }
                });

            migrationBuilder.InsertData(
                table: "ClassOrRoles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 6, "Super-Administrator" },
                    { 7, "Administrator" },
                    { 8, "Technical-Support" },
                    { 9, "Analyst" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "AspNetRoles",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DeleteData(
                table: "ClassOrRoles",
                keyColumn: "Id",
                keyValue: 6);

            migrationBuilder.DeleteData(
                table: "ClassOrRoles",
                keyColumn: "Id",
                keyValue: 7);

            migrationBuilder.DeleteData(
                table: "ClassOrRoles",
                keyColumn: "Id",
                keyValue: 8);

            migrationBuilder.DeleteData(
                table: "ClassOrRoles",
                keyColumn: "Id",
                keyValue: 9);

            migrationBuilder.DropColumn(
                name: "UniqueUsername",
                table: "AspNetUsers");
        }
    }
}
