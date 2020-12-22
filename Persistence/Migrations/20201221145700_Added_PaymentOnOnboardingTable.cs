using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class Added_PaymentOnOnboardingTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AxaEnrollmentOnOnboardings",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    JobId = table.Column<string>(nullable: true),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaEnrollmentOnOnboardings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AxaEnrollmentOnOnboardings_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AxaEnrollmentOnOnboardings_AxaMansardUserProfileId",
                table: "AxaEnrollmentOnOnboardings",
                column: "AxaMansardUserProfileId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AxaEnrollmentOnOnboardings");
        }
    }
}
