using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class RemovedInsufficientfundtable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsufficientChargeTransactions");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsufficientChargeTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    AxaMansardUserProfileId = table.Column<int>(type: "int", nullable: false),
                    DateScheduled = table.Column<DateTime>(type: "datetime2", nullable: false),
                    JobId = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    MatureDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UserId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsufficientChargeTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InsufficientChargeTransactions_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_InsufficientChargeTransactions_AxaMansardUserProfileId",
                table: "InsufficientChargeTransactions",
                column: "AxaMansardUserProfileId");
        }
    }
}
