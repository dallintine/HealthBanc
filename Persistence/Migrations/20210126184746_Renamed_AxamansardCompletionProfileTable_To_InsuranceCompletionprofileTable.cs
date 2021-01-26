using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class Renamed_AxamansardCompletionProfileTable_To_InsuranceCompletionprofileTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AxaMansardCompletionProfiles");

            migrationBuilder.CreateTable(
                name: "InsuranceCompletionProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    ProfileCompleted = table.Column<bool>(nullable: false),
                    TokenizationCompleted = table.Column<bool>(nullable: false),
                    ServiceUsed = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceCompletionProfiles", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceCompletionProfiles");

            migrationBuilder.CreateTable(
                name: "AxaMansardCompletionProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfileCompleted = table.Column<bool>(type: "bit", nullable: false),
                    TokenizationCompleted = table.Column<bool>(type: "bit", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaMansardCompletionProfiles", x => x.Id);
                });
        }
    }
}
