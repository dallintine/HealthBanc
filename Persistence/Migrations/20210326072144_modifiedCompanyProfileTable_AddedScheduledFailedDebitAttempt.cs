using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class modifiedCompanyProfileTable_AddedScheduledFailedDebitAttempt : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "FailedScheduledPaymentRetry",
                table: "CompanyProfiles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FailedScheduledPaymentRetry",
                table: "CompanyProfiles");
        }
    }
}
