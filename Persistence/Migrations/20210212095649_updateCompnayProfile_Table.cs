using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class updateCompnayProfile_Table : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "DateCreated",
                table: "InsuranceUserProfiles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "InsuranceService",
                table: "CompanyProfiles",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DateCreated",
                table: "InsuranceUserProfiles");

            migrationBuilder.DropColumn(
                name: "InsuranceService",
                table: "CompanyProfiles");
        }
    }
}
