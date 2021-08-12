using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class createfamilytables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FamilyProfileId",
                table: "PaymentReferences",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "InsuranceUserProfiles",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "FamilyProfileId",
                table: "InsuranceUserProfiles",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FamilyProfileId",
                table: "Cards",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "FamilyProfileId",
                table: "ActivityLogs",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "FamilyProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    PendingJobId = table.Column<string>(nullable: true),
                    PendingEmailJobId = table.Column<string>(nullable: true),
                    FullName = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    TokenizationCompleted = table.Column<bool>(nullable: false),
                    PaymentProcessed = table.Column<bool>(nullable: false),
                    ProfileCompleted = table.Column<bool>(nullable: false),
                    InsuranceService = table.Column<string>(nullable: true),
                    DateCreated = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FamilyProfiles", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReferences_FamilyProfileId",
                table: "PaymentReferences",
                column: "FamilyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceUserProfiles_FamilyProfileId",
                table: "InsuranceUserProfiles",
                column: "FamilyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_FamilyProfileId",
                table: "Cards",
                column: "FamilyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ActivityLogs_FamilyProfileId",
                table: "ActivityLogs",
                column: "FamilyProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_ActivityLogs_FamilyProfiles_FamilyProfileId",
                table: "ActivityLogs",
                column: "FamilyProfileId",
                principalTable: "FamilyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_FamilyProfiles_FamilyProfileId",
                table: "Cards",
                column: "FamilyProfileId",
                principalTable: "FamilyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_InsuranceUserProfiles_FamilyProfiles_FamilyProfileId",
                table: "InsuranceUserProfiles",
                column: "FamilyProfileId",
                principalTable: "FamilyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentReferences_FamilyProfiles_FamilyProfileId",
                table: "PaymentReferences",
                column: "FamilyProfileId",
                principalTable: "FamilyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ActivityLogs_FamilyProfiles_FamilyProfileId",
                table: "ActivityLogs");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_FamilyProfiles_FamilyProfileId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_InsuranceUserProfiles_FamilyProfiles_FamilyProfileId",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentReferences_FamilyProfiles_FamilyProfileId",
                table: "PaymentReferences");

            migrationBuilder.DropTable(
                name: "FamilyProfiles");

            migrationBuilder.DropIndex(
                name: "IX_PaymentReferences_FamilyProfileId",
                table: "PaymentReferences");

            migrationBuilder.DropIndex(
                name: "IX_InsuranceUserProfiles_FamilyProfileId",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropIndex(
                name: "IX_Cards_FamilyProfileId",
                table: "Cards");

            migrationBuilder.DropIndex(
                name: "IX_ActivityLogs_FamilyProfileId",
                table: "ActivityLogs");

            migrationBuilder.DropColumn(
                name: "FamilyProfileId",
                table: "PaymentReferences");

            migrationBuilder.DropColumn(
                name: "FamilyProfileId",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "FamilyProfileId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "FamilyProfileId",
                table: "ActivityLogs");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "InsuranceUserProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);
        }
    }
}
