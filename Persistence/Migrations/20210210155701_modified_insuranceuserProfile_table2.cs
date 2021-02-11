using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class modified_insuranceuserProfile_table2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceUserProfiles_CompanyProfiles_CompanyProfileId",
                table: "InsuranceUserProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyProfileId",
                table: "InsuranceUserProfiles",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceUserProfiles_CompanyProfiles_CompanyProfileId",
                table: "InsuranceUserProfiles",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceUserProfiles_CompanyProfiles_CompanyProfileId",
                table: "InsuranceUserProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "CompanyProfileId",
                table: "InsuranceUserProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceUserProfiles_CompanyProfiles_CompanyProfileId",
                table: "InsuranceUserProfiles",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
