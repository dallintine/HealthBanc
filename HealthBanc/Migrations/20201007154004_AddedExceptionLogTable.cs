using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class AddedExceptionLogTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AuditLogs",
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
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExceptionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErrorCode = table.Column<string>(nullable: true),
                    ErrorMessage = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    Link = table.Column<string>(nullable: true),
                    ErrorDate = table.Column<DateTime>(nullable: false),
                    StackTrace = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionLogs", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "ExceptionLogs");
        }
    }
}
