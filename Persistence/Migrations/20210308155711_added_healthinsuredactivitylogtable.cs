using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class added_healthinsuredactivitylogtable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "HealthInsuredActivityLog",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceUserProfileId = table.Column<int>(nullable: true),
                    CompanyProfileId = table.Column<int>(nullable: true),
                    ActionApplied = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HealthInsuredActivityLog", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HealthInsuredActivityLog_CompanyProfiles_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HealthInsuredActivityLog_InsuranceUserProfiles_InsuranceUserProfileId",
                        column: x => x.InsuranceUserProfileId,
                        principalTable: "InsuranceUserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReferences_CompanyProfileId",
                table: "PaymentReferences",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthInsuredActivityLog_CompanyProfileId",
                table: "HealthInsuredActivityLog",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthInsuredActivityLog_InsuranceUserProfileId",
                table: "HealthInsuredActivityLog",
                column: "InsuranceUserProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentReferences_CompanyProfiles_CompanyProfileId",
                table: "PaymentReferences",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PaymentReferences_CompanyProfiles_CompanyProfileId",
                table: "PaymentReferences");

            migrationBuilder.DropTable(
                name: "HealthInsuredActivityLog");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReferences_CompanyProfileId",
                table: "PaymentReferences");
        }
    }
}
