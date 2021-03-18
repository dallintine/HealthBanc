using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class ModifiedInsuranceProfileTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UserId",
                table: "ScheduledEnrollments");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EnrollmentOnReactivations");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EnrollmentOnOnboardings");

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "InsuranceUserProfiles",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "ScheduledEnrollments",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AlterColumn<int>(
                name: "UserId",
                table: "InsuranceUserProfiles",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldNullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "EnrollmentOnReactivations",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "EnrollmentOnOnboardings",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
