using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class modified_paymentreference : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Action",
                table: "OtpValidations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Email",
                table: "OtpValidations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "InsuranceUserProfiles",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_CompanyProfileId",
                table: "Wallets",
                column: "CompanyProfileId",
                unique: true,
                filter: "[CompanyProfileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_FamilyProfileId",
                table: "Wallets",
                column: "FamilyProfileId",
                unique: true,
                filter: "[FamilyProfileId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_Wallets_InsuranceUserProfileId",
                table: "Wallets",
                column: "InsuranceUserProfileId",
                unique: true,
                filter: "[InsuranceUserProfileId] IS NOT NULL");

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_CompanyProfiles_CompanyProfileId",
                table: "Wallets",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_FamilyProfiles_FamilyProfileId",
                table: "Wallets",
                column: "FamilyProfileId",
                principalTable: "FamilyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Wallets_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "Wallets",
                column: "InsuranceUserProfileId",
                principalTable: "InsuranceUserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_CompanyProfiles_CompanyProfileId",
                table: "Wallets");

            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_FamilyProfiles_FamilyProfileId",
                table: "Wallets");

            migrationBuilder.DropForeignKey(
                name: "FK_Wallets_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_CompanyProfileId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_FamilyProfileId",
                table: "Wallets");

            migrationBuilder.DropIndex(
                name: "IX_Wallets_InsuranceUserProfileId",
                table: "Wallets");

            migrationBuilder.DropColumn(
                name: "Action",
                table: "OtpValidations");

            migrationBuilder.DropColumn(
                name: "Email",
                table: "OtpValidations");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "InsuranceUserProfiles");
        }
    }
}
