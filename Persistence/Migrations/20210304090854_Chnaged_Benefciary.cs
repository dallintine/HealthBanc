using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class Chnaged_Benefciary : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Restore",
                table: "BeneficiaryReviewUsers");

            migrationBuilder.AddColumn<bool>(
                name: "IsRemove",
                table: "BeneficiaryReviewUsers",
                nullable: false,
                defaultValue: false);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsRemove",
                table: "BeneficiaryReviewUsers");

            migrationBuilder.AddColumn<bool>(
                name: "Restore",
                table: "BeneficiaryReviewUsers",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
