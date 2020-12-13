using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class AddedBackendAdminIDTo_AdminAuditLogTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "BackendAdminUserId",
                table: "AdminAuditLogs",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_BackendAdminUserId",
                table: "AdminAuditLogs",
                column: "BackendAdminUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminAuditLogs_BackendAdminUsers_BackendAdminUserId",
                table: "AdminAuditLogs",
                column: "BackendAdminUserId",
                principalTable: "BackendAdminUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminAuditLogs_BackendAdminUsers_BackendAdminUserId",
                table: "AdminAuditLogs");

            migrationBuilder.DropIndex(
                name: "IX_AdminAuditLogs_BackendAdminUserId",
                table: "AdminAuditLogs");

            migrationBuilder.DropColumn(
                name: "BackendAdminUserId",
                table: "AdminAuditLogs");
        }
    }
}
