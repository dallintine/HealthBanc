using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class AddedPasswordHistorytoUserTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "HashedPasswordHistory",
                table: "AspNetUsers",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "HashedPasswordHistory",
                table: "AspNetUsers");
        }
    }
}
