using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class modifiedPayementReferencTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Active",
                table: "PaymentReferences");

            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "PaymentReferences",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "PaymentReferences");

            migrationBuilder.AddColumn<bool>(
                name: "Active",
                table: "PaymentReferences",
                type: "bit",
                nullable: false,
                defaultValue: false);
        }
    }
}
