using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class modified_beneficiaries_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "CareProviderName",
                table: "BeneficiaryReviewUsers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "StateOfResidence",
                table: "BeneficiaryReviewUsers",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TownOfResidence",
                table: "BeneficiaryReviewUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CareProviderName",
                table: "BeneficiaryReviewUsers");

            migrationBuilder.DropColumn(
                name: "StateOfResidence",
                table: "BeneficiaryReviewUsers");

            migrationBuilder.DropColumn(
                name: "TownOfResidence",
                table: "BeneficiaryReviewUsers");
        }
    }
}
