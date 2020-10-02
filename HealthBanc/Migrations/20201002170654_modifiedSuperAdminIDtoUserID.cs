using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class modifiedSuperAdminIDtoUserID : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SuperAdminId",
                table: "AxaMansardUserProfile");

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "AxaMansardUserProfile",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PaymentReferences",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Date = table.Column<DateTime>(nullable: false),
                    Channel = table.Column<string>(nullable: true),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    RequestId = table.Column<string>(nullable: true),
                    Amount = table.Column<decimal>(nullable: false),
                    Active = table.Column<bool>(nullable: false),
                    DateCanceled = table.Column<DateTime>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentReferences", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentReferences_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReferences_AxaMansardUserProfileId",
                table: "PaymentReferences",
                column: "AxaMansardUserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PaymentReferences");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "AxaMansardUserProfile");

            migrationBuilder.AddColumn<int>(
                name: "SuperAdminId",
                table: "AxaMansardUserProfile",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
