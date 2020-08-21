using Microsoft.EntityFrameworkCore.Migrations;

namespace HealthBanc.Migrations
{
    public partial class AddedAcaMAnsardTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AxaMansardUserProfile",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    TransId = table.Column<string>(nullable: true),
                    Gender = table.Column<string>(nullable: true),
                    CustomerNo = table.Column<string>(nullable: true),
                    Surname = table.Column<string>(nullable: true),
                    Othernames = table.Column<string>(nullable: true),
                    MaidenName = table.Column<string>(nullable: true),
                    DateOfBirth = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    ContactAddress = table.Column<string>(nullable: true),
                    Occupation = table.Column<string>(nullable: true),
                    MaritalStatus = table.Column<string>(nullable: true),
                    BankName = table.Column<string>(nullable: true),
                    AccountNo = table.Column<string>(nullable: true),
                    BVN = table.Column<string>(nullable: true),
                    Weight = table.Column<string>(nullable: true),
                    Height = table.Column<string>(nullable: true),
                    BloodGroup = table.Column<string>(nullable: true),
                    Genotype = table.Column<string>(nullable: true),
                    Identification = table.Column<string>(nullable: true),
                    Religion = table.Column<string>(nullable: true),
                    Hobbies = table.Column<string>(nullable: true),
                    CareProviderName = table.Column<string>(nullable: true),
                    CPPhone = table.Column<string>(nullable: true),
                    CPAddress = table.Column<string>(nullable: true),
                    CPCity = table.Column<string>(nullable: true),
                    CPEmail = table.Column<string>(nullable: true),
                    AlternateHospital = table.Column<string>(nullable: true),
                    MedicalCondition = table.Column<string>(nullable: true),
                    PlanCode = table.Column<string>(nullable: true),
                    Premium = table.Column<string>(nullable: true),
                    CustomerPhoto = table.Column<string>(nullable: true),
                    IdentityPhoto = table.Column<string>(nullable: true),
                    StateOfResidence = table.Column<string>(nullable: true),
                    TownOfResidence = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaMansardUserProfile", x => x.Id);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AxaMansardUserProfile");
        }
    }
}
