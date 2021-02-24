using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class Modified_CompanyProfiletable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PresentCyclePremiumFee",
                table: "CompanyProfiles");

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "CompanyProfiles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "CompanyProfiles");

            migrationBuilder.AddColumn<decimal>(
                name: "PresentCyclePremiumFee",
                table: "CompanyProfiles",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
