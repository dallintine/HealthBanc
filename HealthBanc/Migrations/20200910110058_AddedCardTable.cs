using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class AddedCardTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Authorization_Code",
                table: "TokenizationReferences",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    LastFourDigit = table.Column<string>(nullable: true),
                    TokenizationReferenceId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cards_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Cards_TokenizationReferences_TokenizationReferenceId",
                        column: x => x.TokenizationReferenceId,
                        principalTable: "TokenizationReferences",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Cards_AxaMansardUserProfileId",
                table: "Cards",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_TokenizationReferenceId",
                table: "Cards",
                column: "TokenizationReferenceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropColumn(
                name: "Authorization_Code",
                table: "TokenizationReferences");
        }
    }
}
