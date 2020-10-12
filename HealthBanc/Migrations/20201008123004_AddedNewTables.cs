using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class AddedNewTables : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs");

            migrationBuilder.RenameTable(
                name: "AuditLogs",
                newName: "UserAuditLogs");

            migrationBuilder.AlterColumn<bool>(
                name: "LoginOutHours",
                table: "AuditLogin_LogoutLogs",
                nullable: true,
                oldClrType: typeof(bool),
                oldType: "bit");

            migrationBuilder.AddColumn<string>(
                name: "Device",
                table: "UserAuditLogs",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_UserAuditLogs",
                table: "UserAuditLogs",
                column: "Id");

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserId = table.Column<int>(nullable: false),
                    TransactionId = table.Column<string>(nullable: false),
                    BeforeEventContent = table.Column<string>(nullable: false),
                    ActionApplied = table.Column<string>(nullable: true),
                    AfterEventContent = table.Column<string>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false),
                    IPAddress = table.Column<string>(nullable: false),
                    MACAddress = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordChangeHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserid = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    ChangePassword = table.Column<bool>(nullable: false),
                    ResetPassword = table.Column<bool>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordChangeHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLogin_LogoutLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserid = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    Signin = table.Column<bool>(nullable: false),
                    SignOut = table.Column<bool>(nullable: false),
                    FailedSigninAttempt = table.Column<bool>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogin_LogoutLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "PasswordChangeHistories");

            migrationBuilder.DropTable(
                name: "UserLogin_LogoutLogs");

            migrationBuilder.DropPrimaryKey(
                name: "PK_UserAuditLogs",
                table: "UserAuditLogs");

            migrationBuilder.DropColumn(
                name: "Device",
                table: "UserAuditLogs");

            migrationBuilder.RenameTable(
                name: "UserAuditLogs",
                newName: "AuditLogs");

            migrationBuilder.AlterColumn<bool>(
                name: "LoginOutHours",
                table: "AuditLogin_LogoutLogs",
                type: "bit",
                nullable: false,
                oldClrType: typeof(bool),
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_AuditLogs",
                table: "AuditLogs",
                column: "Id");
        }
    }
}
