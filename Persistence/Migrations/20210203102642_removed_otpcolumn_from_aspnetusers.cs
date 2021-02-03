using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class removed_otpcolumn_from_aspnetusers : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OTPCode",
                table: "AspNetUsers");

            migrationBuilder.DropColumn(
                name: "OTPCreated",
                table: "AspNetUsers");

            migrationBuilder.AddColumn<string>(
                name: "OTPCode",
                table: "CompanyProfiles",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "OTPJobId",
                table: "CompanyProfiles",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "CompanyProfiles",
                nullable: false,
                defaultValue: 0);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "OTPCode",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "OTPJobId",
                table: "CompanyProfiles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CompanyProfiles");

            migrationBuilder.AddColumn<string>(
                name: "OTPCode",
                table: "AspNetUsers",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "OTPCreated",
                table: "AspNetUsers",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));
        }
    }
}
