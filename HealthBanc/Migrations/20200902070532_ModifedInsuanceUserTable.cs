using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class ModifedInsuanceUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SuperAdminId",
                table: "AxaMansardUserProfile",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuperAdminId",
                table: "AxaMansardUserProfile");
        }
    }
}
