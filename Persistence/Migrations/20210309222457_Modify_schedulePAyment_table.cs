using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class Modify_schedulePAyment_table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "HealthInsuredActivityLog");

            migrationBuilder.DropColumn(
                name: "AlternateHospital",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "AlternateHospitalAddress",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "CustomerPhoto",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "Identification",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "IdentityPhoto",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "InsuranceService",
                table: "EnrollmentOnOnboardings");

            migrationBuilder.DropColumn(
                name: "JobId",
                table: "EnrollmentOnOnboardings");

            migrationBuilder.DropColumn(
                name: "AfterEventContent",
                table: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "BeforeEventContent",
                table: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "MACAddress",
                table: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "TransactionId",
                table: "AdminAuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "Channel",
                table: "AdminAuditLogs",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "ActivityLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    InsuranceUserProfileId = table.Column<int>(nullable: true),
                    CompanyProfileId = table.Column<int>(nullable: true),
                    ActionApplied = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    Service = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ActivityLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_CompanyProfiles_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ActivityLogs_InsuranceUserProfiles_InsuranceUserProfileId",
                        column: x => x.InsuranceUserProfileId,
                        principalTable: "InsuranceUserProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_CompanyProfileId",
                table: "ActivityLogs",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_InsuranceUserProfileId",
                table: "ActivityLogs",
                column: "InsuranceUserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "Channel",
                table: "AdminAuditLogs");

            migrationBuilder.AddColumn<string>(
                name: "AlternateHospital",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AlternateHospitalAddress",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CustomerPhoto",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Identification",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "IdentityPhoto",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceService",
                table: "EnrollmentOnOnboardings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "JobId",
                table: "EnrollmentOnOnboardings",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AfterEventContent",
                table: "AdminAuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BeforeEventContent",
                table: "AdminAuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MACAddress",
                table: "AdminAuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "TransactionId",
                table: "AdminAuditLogs",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "HealthInsuredActivityLog",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ActionApplied = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CompanyProfileId = table.Column<int>(type: "int", nullable: true),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    InsuranceUserProfileId = table.Column<int>(type: "int", nullable: true)
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
                name: "IX_HealthInsuredActivityLog_CompanyProfileId",
                table: "HealthInsuredActivityLog",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_HealthInsuredActivityLog_InsuranceUserProfileId",
                table: "HealthInsuredActivityLog",
                column: "InsuranceUserProfileId");
        }
    }
}
