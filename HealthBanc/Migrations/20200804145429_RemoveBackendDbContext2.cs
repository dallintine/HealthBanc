using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class RemoveBackendDbContext2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "ApplicationUserId", "Name" },
                values: new object[] { 1, null, "Pharmmall" });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "ApplicationUserId", "Name" },
                values: new object[] { 2, null, "Insurance" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Services",
                keyColumn: "Id",
                keyValue: 2);
        }
    }
}
