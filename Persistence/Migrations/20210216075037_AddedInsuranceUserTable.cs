using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class AddedInsuranceUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentReferences_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "PaymentReferences");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledPayments_ScheduledEnrollments_ScheduledEnrollmentId",
                table: "ScheduledPayments");

            migrationBuilder.DropColumn(
                name: "AccountNo",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "BVN",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "BankName",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "BloodGroup",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "CPEmail",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "CPPhone",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "Genotype",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "Height",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "Hobbies",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "MedicalCondition",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "Religion",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "Weight",
                table: "InsuranceUserProfiles");

            migrationBuilder.AlterColumn<Guid>(
                name: "ScheduledEnrollmentId",
                table: "ScheduledPayments",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AlterColumn<int>(
                name: "InsuranceUserProfileId",
                table: "ScheduledPayments",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CompanyProfileId",
                table: "ScheduledPayments",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InsuranceUserProfileId",
                table: "PaymentReferences",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CompanyProfileId",
                table: "PaymentReferences",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CompanySubscribedStatus",
                table: "InsuranceUserProfiles",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "NextCyclePremiumFee",
                table: "CompanyProfiles",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "NextPaymentDate",
                table: "CompanyProfiles",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PendingJobId",
                table: "CompanyProfiles",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "PresentCyclePremiumFee",
                table: "CompanyProfiles",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ProfileCompleted",
                table: "CompanyProfiles",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "TokenizationCompleted",
                table: "CompanyProfiles",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AlterColumn<int>(
                name: "InsuranceUserProfileId",
                table: "Cards",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<int>(
                name: "CompanyProfileId",
                table: "Cards",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "CompanyInsuranceUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CompanyProfileId = table.Column<int>(nullable: false),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Gender = table.Column<string>(nullable: true),
                    DateOfBirth = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CompanyInsuranceUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CompanyInsuranceUsers_CompanyProfiles_CompanyProfileId",
                        column: x => x.CompanyProfileId,
                        principalTable: "CompanyProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_CompanyProfileId",
                table: "Cards",
                column: "CompanyProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_CompanyInsuranceUsers_CompanyProfileId",
                table: "CompanyInsuranceUsers",
                column: "CompanyProfileId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_CompanyProfiles_CompanyProfileId",
                table: "Cards",
                column: "CompanyProfileId",
                principalTable: "CompanyProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "Cards",
                column: "InsuranceUserProfileId",
                principalTable: "InsuranceUserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentReferences_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "PaymentReferences",
                column: "InsuranceUserProfileId",
                principalTable: "InsuranceUserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledPayments_ScheduledEnrollments_ScheduledEnrollmentId",
                table: "ScheduledPayments",
                column: "ScheduledEnrollmentId",
                principalTable: "ScheduledEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_CompanyProfiles_CompanyProfileId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_Cards_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "Cards");

            migrationBuilder.DropForeignKey(
                name: "FK_PaymentReferences_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "PaymentReferences");

            migrationBuilder.DropForeignKey(
                name: "FK_ScheduledPayments_ScheduledEnrollments_ScheduledEnrollmentId",
                table: "ScheduledPayments");

            migrationBuilder.DropTable(
                name: "CompanyInsuranceUsers");

            migrationBuilder.DropIndex(
                name: "IX_Cards_CompanyProfileId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                table: "ScheduledPayments");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                table: "PaymentReferences");

            migrationBuilder.DropColumn(
                name: "CompanySubscribedStatus",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "NextCyclePremiumFee",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "NextPaymentDate",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "PendingJobId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "PresentCyclePremiumFee",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "ProfileCompleted",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "TokenizationCompleted",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "CompanyProfileId",
                table: "Cards");

            migrationBuilder.AlterColumn<Guid>(
                name: "ScheduledEnrollmentId",
                table: "ScheduledPayments",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InsuranceUserProfileId",
                table: "ScheduledPayments",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InsuranceUserProfileId",
                table: "PaymentReferences",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AccountNo",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BVN",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BankName",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "BloodGroup",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CPEmail",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CPPhone",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Genotype",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Height",
                table: "InsuranceUserProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Hobbies",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "MedicalCondition",
                table: "InsuranceUserProfiles",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "Religion",
                table: "InsuranceUserProfiles",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Weight",
                table: "InsuranceUserProfiles",
                type: "decimal(18,2)",
                nullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "InsuranceUserProfileId",
                table: "Cards",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "Cards",
                column: "InsuranceUserProfileId",
                principalTable: "InsuranceUserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_PaymentReferences_InsuranceUserProfiles_InsuranceUserProfileId",
                table: "PaymentReferences",
                column: "InsuranceUserProfileId",
                principalTable: "InsuranceUserProfiles",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_ScheduledPayments_ScheduledEnrollments_ScheduledEnrollmentId",
                table: "ScheduledPayments",
                column: "ScheduledEnrollmentId",
                principalTable: "ScheduledEnrollments",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
