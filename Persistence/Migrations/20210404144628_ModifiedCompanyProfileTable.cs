using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class ModifiedCompanyProfileTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NextCyclePremiumFee",
                table: "CompanyProfiles");

            migrationBuilder.AddColumn<string>(
                name: "CompanyName",
                table: "InsuranceUserProfiles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CompanyName",
                table: "InsuranceUserProfiles");

            migrationBuilder.AddColumn<decimal>(
                name: "NextCyclePremiumFee",
                table: "CompanyProfiles",
                type: "decimal(18,2)",
                nullable: true);
        }
    }
}
