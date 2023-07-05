using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Persistence.Migrations
{
    public partial class InitalCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "HealthBanc");

            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UniqueUsername = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OAuthSubject = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    LastLoginDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshTokenExpiryTime = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefreshToken = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    UserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SecurityStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumber = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(type: "bit", nullable: false),
                    TwoFactorEnabled = table.Column<bool>(type: "bit", nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    LockoutEnabled = table.Column<bool>(type: "bit", nullable: false),
                    AccessFailedCount = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ErrorLogs",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErrorCode = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    StackTrace = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Path = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OneTimePasswords",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<long>(type: "bigint", nullable: false),
                    Otp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OneTimePasswords", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceDetailURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageUrl = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    BackgroundColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ActiveColor = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserSessions",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ExpiryDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Browser = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    DeviceIp = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserSessions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "HealthBanc",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    ClaimType = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ClaimValue = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "HealthBanc",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                schema: "HealthBanc",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderKey = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    UserId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "HealthBanc",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                schema: "HealthBanc",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalSchema: "HealthBanc",
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "HealthBanc",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                schema: "HealthBanc",
                columns: table => new
                {
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    LoginProvider = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Value = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalSchema: "HealthBanc",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Transactions",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Reference = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ApplicationUserId = table.Column<long>(type: "bigint", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Transactions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "HealthBanc",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Vendors",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    SettlementAccount = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Vendors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Vendors_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "HealthBanc",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Discount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    MarkUpRate = table.Column<double>(type: "float", nullable: false),
                    VendorId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    ImageURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Tag = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalLinkName = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExternalLinkURL = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    OptionalFee = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Plans_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalSchema: "HealthBanc",
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Plans_Vendors_VendorId",
                        column: x => x.VendorId,
                        principalSchema: "HealthBanc",
                        principalTable: "Vendors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "PlanDescriptions",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    PlanId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PlanDescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PlanDescriptions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "HealthBanc",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Subscriptions",
                schema: "HealthBanc",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ApplicationUserId = table.Column<long>(type: "bigint", nullable: false),
                    PlanId = table.Column<long>(type: "bigint", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    VendorId = table.Column<long>(type: "bigint", nullable: false),
                    ServiceId = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    OptionalFee = table.Column<bool>(type: "bit", nullable: false),
                    TransactionId = table.Column<long>(type: "bigint", nullable: false),
                    IsSuccessful = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_AspNetUsers_ApplicationUserId",
                        column: x => x.ApplicationUserId,
                        principalSchema: "HealthBanc",
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "HealthBanc",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Transactions_TransactionId",
                        column: x => x.TransactionId,
                        principalSchema: "HealthBanc",
                        principalTable: "Transactions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                schema: "HealthBanc",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                schema: "HealthBanc",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                schema: "HealthBanc",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                schema: "HealthBanc",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                schema: "HealthBanc",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                schema: "HealthBanc",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                schema: "HealthBanc",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_PlanDescriptions_PlanId",
                schema: "HealthBanc",
                table: "PlanDescriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_ServiceId",
                schema: "HealthBanc",
                table: "Plans",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_Plans_VendorId",
                schema: "HealthBanc",
                table: "Plans",
                column: "VendorId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_ApplicationUserId",
                schema: "HealthBanc",
                table: "Subscriptions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_PlanId",
                schema: "HealthBanc",
                table: "Subscriptions",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TransactionId",
                schema: "HealthBanc",
                table: "Subscriptions",
                column: "TransactionId");

            migrationBuilder.CreateIndex(
                name: "IX_Transactions_ApplicationUserId",
                schema: "HealthBanc",
                table: "Transactions",
                column: "ApplicationUserId");

            migrationBuilder.CreateIndex(
                name: "IX_Vendors_ServiceId",
                schema: "HealthBanc",
                table: "Vendors",
                column: "ServiceId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AspNetRoleClaims",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "ErrorLogs",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "OneTimePasswords",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "PlanDescriptions",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "Subscriptions",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "UserSessions",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "AspNetRoles",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "Transactions",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "Vendors",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "AspNetUsers",
                schema: "HealthBanc");

            migrationBuilder.DropTable(
                name: "Services",
                schema: "HealthBanc");
        }
    }
}
