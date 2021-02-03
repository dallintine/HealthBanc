using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class added_corporateusertable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "InsuranceService",
                table: "ScheduledPayments",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceService",
                table: "ScheduledEnrollments",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceService",
                table: "PaymentOnReactivations",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "CompanyProfileId",
                table: "InsuranceUserProfiles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceService",
                table: "EnrollmentOnReactivations",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceService",
                table: "EnrollmentOnOnboardings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OTPCode",
                table: "AspNetUsers",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OTPCreated",
                table: "AspNetUsers",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.CreateTable(
                name: "CompanyProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyName = table.Column<string>(nullable: true),
                    CompanyEmail = table.Column<string>(nullable: true),
                    Industry = table.Column<string>(nullable: true),
                    CompanySize = table.Column<string>(nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceUserProfiles_CompanyProfileId",
                table: "InsuranceUserProfiles",
                column: "CompanyProfileId");

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

            migrationBuilder.DropTable(
                name: "CompanyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceUserProfiles_CompanyProfileId",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "InsuranceService",
                table: "ScheduledPayments");

            migrationBuilder.DropColumn(
                name: "InsuranceService",
                table: "ScheduledEnrollments");

            migrationBuilder.DropColumn(
                name: "InsuranceService",
                table: "PaymentOnReactivations");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "InsuranceService",
                table: "EnrollmentOnReactivations");

            migrationBuilder.DropColumn(
                name: "InsuranceService",
                table: "EnrollmentOnOnboardings");

            migrationBuilder.DropColumn(
                name: "OTPCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OTPCreated",
                table: "AspNetUsers");
        }
    }
}
