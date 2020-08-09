using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class AddedNotificationTable2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_Services_ServiceId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_ServiceId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ServiceId",
                table: "Services");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "ServiceId",
                table: "Services",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ServiceId",
                table: "Services",
                column: "ServiceId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_Services_ServiceId",
                table: "Services",
                column: "ServiceId",
                principalTable: "Services",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
