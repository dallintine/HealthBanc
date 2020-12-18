using System;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Persistence.Migrations
{
    public partial class ModifyDatabase : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserName = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedUserName = table.Column<string>(maxLength: 256, nullable: true),
                    Email = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedEmail = table.Column<string>(maxLength: 256, nullable: true),
                    EmailConfirmed = table.Column<bool>(nullable: false),
                    PasswordHash = table.Column<string>(nullable: true),
                    SecurityStamp = table.Column<string>(nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true),
                    PhoneNumber = table.Column<string>(nullable: true),
                    PhoneNumberConfirmed = table.Column<bool>(nullable: false),
                    TwoFactorEnabled = table.Column<bool>(nullable: false),
                    LockoutEnd = table.Column<DateTimeOffset>(nullable: true),
                    LockoutEnabled = table.Column<bool>(nullable: false),
                    AccessFailedCount = table.Column<int>(nullable: false),
                    FirstName = table.Column<string>(nullable: false),
                    LastName = table.Column<string>(nullable: false),
                    HashedPasswordHistory = table.Column<string>(nullable: true),
                    DateOfRegistration = table.Column<DateTime>(nullable: false),
                    IsDeleted = table.Column<bool>(nullable: false),
                    SuperAdminId = table.Column<int>(nullable: true),
                    AdminId = table.Column<int>(nullable: true),
                    LastLoginDate = table.Column<DateTime>(nullable: false),
                    ServiceUsed = table.Column<string>(nullable: true),
                    UniqueUsername = table.Column<string>(nullable: true),
                    RefreshToken = table.Column<string>(nullable: true),
                    RefreshTokenExpiryTime = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUsers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogin_LogoutLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserid = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    Signin = table.Column<bool>(nullable: false),
                    SignOut = table.Column<bool>(nullable: false),
                    LoginFailure = table.Column<bool>(nullable: false),
                    LogOutFailure = table.Column<bool>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false),
                    LoginOutHours = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogin_LogoutLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AxaMansardCompletionProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    ProfileCompleted = table.Column<bool>(nullable: false),
                    TokenizationCompleted = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaMansardCompletionProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AxaMansardHospitalLists",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    State = table.Column<string>(nullable: true),
                    City = table.Column<string>(nullable: true),
                    HospitalName = table.Column<string>(nullable: true),
                    Address = table.Column<string>(nullable: true),
                    Specialisation = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaMansardHospitalLists", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AxaMansardUserProfile",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    TransId = table.Column<string>(nullable: true),
                    Gender = table.Column<string>(nullable: true),
                    PendingJobId = table.Column<string>(nullable: true),
                    Surname = table.Column<string>(nullable: true),
                    Othernames = table.Column<string>(nullable: true),
                    MaidenName = table.Column<string>(nullable: true),
                    DateOfBirth = table.Column<DateTime>(nullable: false),
                    PhoneNumber = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    ContactAddress = table.Column<string>(nullable: true),
                    Occupation = table.Column<string>(nullable: true),
                    MaritalStatus = table.Column<string>(nullable: true),
                    BankName = table.Column<string>(nullable: true),
                    AccountNo = table.Column<string>(nullable: true),
                    BVN = table.Column<string>(nullable: true),
                    Weight = table.Column<decimal>(nullable: true),
                    Height = table.Column<decimal>(nullable: true),
                    BloodGroup = table.Column<string>(nullable: true),
                    Genotype = table.Column<string>(nullable: true),
                    Identification = table.Column<string>(nullable: true),
                    Religion = table.Column<int>(nullable: true),
                    Hobbies = table.Column<string>(nullable: true),
                    CareProviderName = table.Column<string>(nullable: true),
                    CPPhone = table.Column<string>(nullable: true),
                    CPAddress = table.Column<string>(nullable: true),
                    CPCity = table.Column<string>(nullable: true),
                    CPEmail = table.Column<string>(nullable: true),
                    AlternateHospital = table.Column<string>(nullable: true),
                    AlternateHospitalAddress = table.Column<string>(nullable: true),
                    MedicalCondition = table.Column<string>(nullable: true),
                    PlanCode = table.Column<string>(nullable: true),
                    Premium = table.Column<decimal>(nullable: false),
                    CustomerPhoto = table.Column<string>(nullable: true),
                    IdentityPhoto = table.Column<string>(nullable: true),
                    StateOfResidence = table.Column<string>(nullable: true),
                    TownOfResidence = table.Column<string>(nullable: true),
                    ActiveStatus = table.Column<bool>(nullable: true),
                    EndActiveStatusDate = table.Column<DateTime>(nullable: false),
                    StartActiveStatusDate = table.Column<DateTime>(nullable: false),
                    SubscriptionStatus = table.Column<bool>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaMansardUserProfile", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ClassOrRoles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClassOrRoles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ExceptionLogs",
                columns: table => new
                {
                    Id = table.Column<long>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ErrorCode = table.Column<string>(nullable: true),
                    ErrorMessage = table.Column<string>(nullable: true),
                    Source = table.Column<string>(nullable: true),
                    Link = table.Column<string>(nullable: true),
                    ErrorDate = table.Column<DateTime>(nullable: false),
                    StackTrace = table.Column<string>(nullable: true),
                    Path = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExceptionLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "PasswordChangeHistories",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserid = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    ChangePassword = table.Column<bool>(nullable: false),
                    ResetPassword = table.Column<bool>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PasswordChangeHistories", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Services",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Services", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserId = table.Column<int>(nullable: false),
                    TransactionId = table.Column<string>(nullable: true),
                    BeforeEventContent = table.Column<string>(nullable: true),
                    ActionApplied = table.Column<string>(nullable: false),
                    AfterEventContent = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    IPAddress = table.Column<string>(nullable: true),
                    Device = table.Column<string>(nullable: true),
                    MACAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserAuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "UserLogin_LogoutLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserid = table.Column<int>(nullable: false),
                    Email = table.Column<string>(nullable: true),
                    Signin = table.Column<bool>(nullable: false),
                    SignOut = table.Column<bool>(nullable: false),
                    FailedSigninAttempt = table.Column<bool>(nullable: false),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserLogin_LogoutLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<int>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AspNetUserClaims_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_AspNetUserLogins_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    RoleId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetRoles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "AspNetRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AspNetUserRoles_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<int>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_AspNetUserTokens_AspNetUsers_UserId",
                        column: x => x.UserId,
                        principalTable: "AspNetUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AxaEnrollmentReactivations",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    ExecutionDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AxaEnrollmentReactivations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AxaEnrollmentReactivations_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Cards",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    Status = table.Column<int>(nullable: false),
                    LastFourDigit = table.Column<string>(nullable: true),
                    Type = table.Column<string>(nullable: true),
                    CardReference = table.Column<string>(nullable: true),
                    Authorization_Code = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Cards", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Cards_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InsufficientChargeTransactions",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    MatureDate = table.Column<DateTime>(nullable: false),
                    JobId = table.Column<string>(nullable: true)
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
                    Amount = table.Column<decimal>(nullable: false),
                    Active = table.Column<bool>(nullable: false)
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

            migrationBuilder.CreateTable(
                name: "ScheduledAxaEnrollments",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    ExecutionDate = table.Column<DateTime>(nullable: false),
                    JobId = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledAxaEnrollments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledAxaEnrollments_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BackendAdminUsers",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FirstName = table.Column<string>(nullable: true),
                    LastName = table.Column<string>(nullable: true),
                    Email = table.Column<string>(nullable: true),
                    ClassOrRoleId = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BackendAdminUsers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BackendAdminUsers_ClassOrRoles_ClassOrRoleId",
                        column: x => x.ClassOrRoleId,
                        principalTable: "ClassOrRoles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Subject = table.Column<string>(nullable: false),
                    Message = table.Column<string>(nullable: false),
                    ScheduleDate = table.Column<DateTime>(nullable: false),
                    ImageURl = table.Column<string>(nullable: true),
                    ServiceId = table.Column<int>(nullable: false),
                    RestoreServiceId = table.Column<int>(nullable: true),
                    Status = table.Column<int>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Notifications_Services_ServiceId",
                        column: x => x.ServiceId,
                        principalTable: "Services",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PaymentOnReactivations",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    AxaEnrollmentOnReactivationId = table.Column<Guid>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    ExecutionDate = table.Column<DateTime>(nullable: false),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true),
                    JobId = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PaymentOnReactivations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PaymentOnReactivations_AxaEnrollmentReactivations_AxaEnrollmentOnReactivationId",
                        column: x => x.AxaEnrollmentOnReactivationId,
                        principalTable: "AxaEnrollmentReactivations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PaymentOnReactivations_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                });

            migrationBuilder.CreateTable(
                name: "ScheduledPayments",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<int>(nullable: false),
                    AxaMansardUserProfileId = table.Column<int>(nullable: false),
                    ScheduledAxaEnrollmentId = table.Column<Guid>(nullable: false),
                    DateScheduled = table.Column<DateTime>(nullable: false),
                    ExecutionDate = table.Column<DateTime>(nullable: false),
                    JobId = table.Column<string>(nullable: true),
                    Status = table.Column<string>(nullable: true),
                    Message = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduledPayments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduledPayments_AxaMansardUserProfile_AxaMansardUserProfileId",
                        column: x => x.AxaMansardUserProfileId,
                        principalTable: "AxaMansardUserProfile",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.NoAction);
                    table.ForeignKey(
                        name: "FK_ScheduledPayments_ScheduledAxaEnrollments_ScheduledAxaEnrollmentId",
                        column: x => x.ScheduledAxaEnrollmentId,
                        principalTable: "ScheduledAxaEnrollments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AdminAuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(nullable: false),
                    ApplicationUserId = table.Column<int>(nullable: false),
                    BackendAdminUserId = table.Column<int>(nullable: false),
                    TransactionId = table.Column<string>(nullable: true),
                    BeforeEventContent = table.Column<string>(nullable: true),
                    ActionApplied = table.Column<string>(nullable: false),
                    AfterEventContent = table.Column<string>(nullable: true),
                    Date = table.Column<DateTime>(nullable: false),
                    IPAddress = table.Column<string>(nullable: false),
                    MACAddress = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminAuditLogs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminAuditLogs_BackendAdminUsers_BackendAdminUserId",
                        column: x => x.BackendAdminUserId,
                        principalTable: "BackendAdminUsers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.InsertData(
                table: "AspNetRoles",
                columns: new[] { "Id", "ConcurrencyStamp", "Name", "NormalizedName" },
                values: new object[,]
                {
                    { 1, null, "SuperAdmin", "SUPERADMIN" },
                    { 6, null, "Super-Administrator", "SUPER-ADMINISTRATOR" },
                    { 7, null, "Administrator", "ADMINISTRATOR" },
                    { 8, null, "Technical-Support", "TECHNICAL-SUPPORT" },
                    { 9, null, "Analyst", "ANALYST" }
                });

            migrationBuilder.InsertData(
                table: "ClassOrRoles",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "SuperAdmin" },
                    { 6, "Super-Administrator" },
                    { 7, "Administrator" },
                    { 8, "Technical-Support" },
                    { 9, "Analyst" }
                });

            migrationBuilder.InsertData(
                table: "Services",
                columns: new[] { "Id", "Name" },
                values: new object[,]
                {
                    { 1, "HealthMall" },
                    { 2, "HealthInsured" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminAuditLogs_BackendAdminUserId",
                table: "AdminAuditLogs",
                column: "BackendAdminUserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetRoleClaims_RoleId",
                table: "AspNetRoleClaims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "AspNetRoles",
                column: "NormalizedName",
                unique: true,
                filter: "[NormalizedName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserClaims_UserId",
                table: "AspNetUserClaims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserLogins_UserId",
                table: "AspNetUserLogins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_AspNetUserRoles_RoleId",
                table: "AspNetUserRoles",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "AspNetUsers",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "AspNetUsers",
                column: "NormalizedUserName",
                unique: true,
                filter: "[NormalizedUserName] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AxaEnrollmentReactivations_AxaMansardUserProfileId",
                table: "AxaEnrollmentReactivations",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_BackendAdminUsers_ClassOrRoleId",
                table: "BackendAdminUsers",
                column: "ClassOrRoleId");

            migrationBuilder.CreateIndex(
                name: "IX_Cards_AxaMansardUserProfileId",
                table: "Cards",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_InsufficientChargeTransactions_AxaMansardUserProfileId",
                table: "InsufficientChargeTransactions",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_Notifications_ServiceId",
                table: "Notifications",
                column: "ServiceId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOnReactivations_AxaEnrollmentOnReactivationId",
                table: "PaymentOnReactivations",
                column: "AxaEnrollmentOnReactivationId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentOnReactivations_AxaMansardUserProfileId",
                table: "PaymentOnReactivations",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_PaymentReferences_AxaMansardUserProfileId",
                table: "PaymentReferences",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledAxaEnrollments_AxaMansardUserProfileId",
                table: "ScheduledAxaEnrollments",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPayments_AxaMansardUserProfileId",
                table: "ScheduledPayments",
                column: "AxaMansardUserProfileId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduledPayments_ScheduledAxaEnrollmentId",
                table: "ScheduledPayments",
                column: "ScheduledAxaEnrollmentId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminAuditLogs");

            migrationBuilder.DropTable(
                name: "AspNetRoleClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserClaims");

            migrationBuilder.DropTable(
                name: "AspNetUserLogins");

            migrationBuilder.DropTable(
                name: "AspNetUserRoles");

            migrationBuilder.DropTable(
                name: "AspNetUserTokens");

            migrationBuilder.DropTable(
                name: "AuditLogin_LogoutLogs");

            migrationBuilder.DropTable(
                name: "AxaMansardCompletionProfiles");

            migrationBuilder.DropTable(
                name: "AxaMansardHospitalLists");

            migrationBuilder.DropTable(
                name: "Cards");

            migrationBuilder.DropTable(
                name: "ExceptionLogs");

            migrationBuilder.DropTable(
                name: "InsufficientChargeTransactions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PasswordChangeHistories");

            migrationBuilder.DropTable(
                name: "PaymentOnReactivations");

            migrationBuilder.DropTable(
                name: "PaymentReferences");

            migrationBuilder.DropTable(
                name: "ScheduledPayments");

            migrationBuilder.DropTable(
                name: "UserAuditLogs");

            migrationBuilder.DropTable(
                name: "UserLogin_LogoutLogs");

            migrationBuilder.DropTable(
                name: "BackendAdminUsers");

            migrationBuilder.DropTable(
                name: "AspNetRoles");

            migrationBuilder.DropTable(
                name: "AspNetUsers");

            migrationBuilder.DropTable(
                name: "Services");

            migrationBuilder.DropTable(
                name: "AxaEnrollmentReactivations");

            migrationBuilder.DropTable(
                name: "ScheduledAxaEnrollments");

            migrationBuilder.DropTable(
                name: "ClassOrRoles");

            migrationBuilder.DropTable(
                name: "AxaMansardUserProfile");
        }
    }
}
