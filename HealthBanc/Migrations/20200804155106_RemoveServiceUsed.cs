using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class RemoveServiceUsed : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Services_AspNetUsers_ApplicationUserId",
                table: "Services");

            migrationBuilder.DropIndex(
                name: "IX_Services_ApplicationUserId",
                table: "Services");

            migrationBuilder.DropColumn(
                name: "ApplicationUserId",
                table: "Services");

            migrationBuilder.AddColumn<string>(
                name: "ServiceUsed",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ServiceUsed",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<int>(
                name: "ApplicationUserId",
                table: "Services",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Services_ApplicationUserId",
                table: "Services",
                column: "ApplicationUserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Services_AspNetUsers_ApplicationUserId",
                table: "Services",
                column: "ApplicationUserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
