using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class addedOTPValidations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PaymentMethod",
                table: "PaymentReferences",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "OtpValidations",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PhoneNumber = table.Column<string>(nullable: true),
                    OTP = table.Column<string>(nullable: true),
                    GeneratedDate = table.Column<DateTimeOffset>(nullable: false),
                    ExpiredDate = table.Column<DateTimeOffset>(nullable: false),
                    Status = table.Column<bool>(nullable: false),
                    ApplicationUserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OtpValidations", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "OtpValidations");

            migrationBuilder.DropColumn(
                name: "PaymentMethod",
                table: "PaymentReferences");
        }
    }
}
