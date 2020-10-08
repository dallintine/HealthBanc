using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class ModifiedAxaMAnsardUserProfile : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AlternateHospitalAddress",
                table: "AxaMansardUserProfile",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AuditLogin_LogoutLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserid = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    Signin = table.Column<bool>(nullable: false),
                    SignOut = table.Column<bool>(nullable: false),
                    LoginFailure = table.Column<bool>(nullable: false),
                    LogOutFailure = table.Column<bool>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false),
                    LoginOutHours = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogin_LogoutLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogin_LogoutLogs");

            migrationBuilder.DropColumn(
                name: "AlternateHospitalAddress",
                table: "AxaMansardUserProfile");
        }
    }
}
