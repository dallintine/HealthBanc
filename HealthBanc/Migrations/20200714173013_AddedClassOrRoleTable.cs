using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class AddedClassOrRoleTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClassOrRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassOrRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IdentityRole",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: true),
                    NormalizedName = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IdentityRole", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "ClassOrRoles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "SuperAdmin" },
                    { 2, "Initiator" },
                    { 3, "Reviewer" },
                    { 4, "Authorizer" }
                });

            migrationBuilder.InsertData(
                table: "IdentityRole",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { "1", null, "SuperAdmin", "SUPERADMIN" },
                    { "2", null, "RepSuperAdmin", "REPSUPERADMIN" },
                    { "3", null, "Initiator", "INITIATOR" },
                    { "4", null, "Reviewer", "REVIEWER" },
                    { "5", null, "Authorizer", "Authorizer" }
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ClassOrRoles");

            migrationBuilder.DropTable(
                name: "IdentityRole");
        }
    }
}
