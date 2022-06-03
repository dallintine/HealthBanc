using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class addedWalletsTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Wallets",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    InsuranceUserProfileId = table.Column<int>(nullable: true),
                    CompanyProfileId = table.Column<int>(nullable: true),
                    FamilyProfileId = table.Column<int>(nullable: true),
                    Mobile = table.Column<string>(nullable: true),
                    WalletId = table.Column<string>(nullable: true),
                    VirtualAccount = table.Column<string>(nullable: true),
                    AccountTier = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Wallets", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Wallets");
        }
    }
}
