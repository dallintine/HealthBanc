using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class TestUserProfileTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TestUserProfiles",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    TransId = table.Column<string>(nullable: true),
                    FirstName = table.Column<string>(nullable: true),
                    SurnName = table.Column<string>(nullable: true),
                    OtherName = table.Column<string>(nullable: true),
                    MaritalStatus = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    DateOfBirth = table.Column<string>(nullable: true),
                    NextofKin = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    Plan = table.Column<string>(nullable: true),
                    Sex = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TestUserProfiles", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TestUserProfiles");
        }
    }
}
