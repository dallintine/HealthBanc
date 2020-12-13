using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class ModifiedAxaMAnsardCompletionProfileTableProperty : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Cards_TokenizationReferences_TokenizationReferenceId",
                table: "Cards");

            migrationBuilder.DropTable(
                name: "TestUserProfiles");

            migrationBuilder.DropTable(
                name: "TokenizationReferences");

            migrationBuilder.DropIndex(
                name: "IX_Cards_TokenizationReferenceId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "DateCanceled",
                table: "PaymentReferences");

            migrationBuilder.DropColumn(
                name: "RequestId",
                table: "PaymentReferences");

            migrationBuilder.DropColumn(
                name: "TokenizationReferenceId",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CustomerNo",
                table: "AxaMansardUserProfile");

            migrationBuilder.DropColumn(
                name: "SuperAdminId",
                table: "AxaMansardCompletionProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "UserAuditLogs",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AlterColumn<string>(
                name: "Device",
                table: "UserAuditLogs",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<string>(
                name: "Authorization_Code",
                table: "Cards",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "CardReference",
                table: "Cards",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "ActiveStatus",
                table: "AxaMansardUserProfile",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "EndActiveStatusDate",
                table: "AxaMansardUserProfile",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<string>(
                name: "PendingJobId",
                table: "AxaMansardUserProfile",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "StartActiveStatusDate",
                table: "AxaMansardUserProfile",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "AxaMansardCompletionProfiles",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "AxaEnrollmentReactivations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    ExecutionDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaEnrollmentReactivations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AxaEnrollmentReactivations_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AxaMansardHospitalLists",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    State = table.Column<string>(nullable: true),
                    City = table.Column<string>(nullable: true),
                    HospitalName = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Specialisation = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaMansardHospitalLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "InsufficientChargeTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    MatureDate = table.Column<DateTime>(nullable: false),
                    JobId = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsufficientChargeTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsufficientChargeTransactions_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledAxaEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    ExecutionDate = table.Column<DateTime>(nullable: false),
                    JobId = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledAxaEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledAxaEnrollments_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentOnReactivations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    AxaEnrollmentOnReactivationId = table.Column<Guid>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    ExecutionDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true),
                    JobId = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOnReactivations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentOnReactivations_AxaEnrollmentReactivations_AxaEnrollmentOnReactivationId",
                        column: x => x.AxaEnrollmentOnReactivationId,
                        principalTable: "AxaEnrollmentReactivations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentOnReactivations_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledPayments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    ScheduledAxaEnrollmentId = table.Column<Guid>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    ExecutionDate = table.Column<DateTime>(nullable: false),
                    JobId = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledPayments_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ScheduledPayments_ScheduledAxaEnrollments_ScheduledAxaEnrollmentId",
                        column: x => x.ScheduledAxaEnrollmentId,
                        principalTable: "ScheduledAxaEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AxaEnrollmentReactivations_AxaMansardUserProfileId",
                table: "AxaEnrollmentReactivations",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_InsufficientChargeTransactions_AxaMansardUserProfileId",
                table: "InsufficientChargeTransactions",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOnReactivations_AxaEnrollmentOnReactivationId",
                table: "PaymentOnReactivations",
                column: "AxaEnrollmentOnReactivationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOnReactivations_AxaMansardUserProfileId",
                table: "PaymentOnReactivations",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledAxaEnrollments_AxaMansardUserProfileId",
                table: "ScheduledAxaEnrollments",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPayments_AxaMansardUserProfileId",
                table: "ScheduledPayments",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPayments_ScheduledAxaEnrollmentId",
                table: "ScheduledPayments",
                column: "ScheduledAxaEnrollmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AxaMansardHospitalLists");

            migrationBuilder.DropTable(
                name: "InsufficientChargeTransactions");

            migrationBuilder.DropTable(
                name: "PaymentOnReactivations");

            migrationBuilder.DropTable(
                name: "ScheduledPayments");

            migrationBuilder.DropTable(
                name: "AxaEnrollmentReactivations");

            migrationBuilder.DropTable(
                name: "ScheduledAxaEnrollments");

            migrationBuilder.DropColumn(
                name: "Authorization_Code",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "CardReference",
                table: "Cards");

            migrationBuilder.DropColumn(
                name: "ActiveStatus",
                table: "AxaMansardUserProfile");

            migrationBuilder.DropColumn(
                name: "EndActiveStatusDate",
                table: "AxaMansardUserProfile");

            migrationBuilder.DropColumn(
                name: "PendingJobId",
                table: "AxaMansardUserProfile");

            migrationBuilder.DropColumn(
                name: "StartActiveStatusDate",
                table: "AxaMansardUserProfile");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AxaMansardCompletionProfiles");

            migrationBuilder.AlterColumn<string>(
                name: "IPAddress",
                table: "UserAuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Device",
                table: "UserAuditLogs",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldNullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "DateCanceled",
                table: "PaymentReferences",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RequestId",
                table: "PaymentReferences",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TokenizationReferenceId",
                table: "Cards",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "CustomerNo",
                table: "AxaMansardUserProfile",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "SuperAdminId",
                table: "AxaMansardCompletionProfiles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "TestUserProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    DateOfBirth = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MaritalStatus = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    NextofKin = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OtherName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Plan = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Sex = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SurnName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    TransId = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestUserProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TokenizationReferences",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Authorization_Code = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SuperAdminId = table.Column<int>(type: "int", nullable: false),
                    TokenReference = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TokenizationReferences", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_TokenizationReferenceId",
                table: "Cards",
                column: "TokenizationReferenceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Cards_TokenizationReferences_TokenizationReferenceId",
                table: "Cards",
                column: "TokenizationReferenceId",
                principalTable: "TokenizationReferences",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
