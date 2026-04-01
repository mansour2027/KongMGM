using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace KongPortal.Data.Migrations
{
    public partial class InitialCreate : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ASP.NET Identity tables
            migrationBuilder.CreateTable(
                name: "AspNetRoles",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    Name = table.Column<string>(maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AspNetRoles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AspNetUsers",
                columns: table => new
                {
                    Id = table.Column<string>(nullable: false),
                    FullName = table.Column<string>(nullable: false, defaultValue: ""),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    LastLoginAt = table.Column<DateTime>(nullable: true),
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
                    AccessFailedCount = table.Column<int>(nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_AspNetUsers", x => x.Id));

            migrationBuilder.CreateTable(name: "AspNetRoleClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    RoleId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetRoleClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetRoleClaims_AspNetRoles_RoleId",
                        x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(name: "AspNetUserClaims",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(nullable: false),
                    ClaimType = table.Column<string>(nullable: true),
                    ClaimValue = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserClaims", x => x.Id);
                    table.ForeignKey("FK_AspNetUserClaims_AspNetUsers_UserId",
                        x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(name: "AspNetUserLogins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(nullable: false),
                    ProviderKey = table.Column<string>(nullable: false),
                    ProviderDisplayName = table.Column<string>(nullable: true),
                    UserId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserLogins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey("FK_AspNetUserLogins_AspNetUsers_UserId",
                        x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(name: "AspNetUserRoles",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    RoleId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserRoles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey("FK_AspNetUserRoles_AspNetRoles_RoleId",
                        x => x.RoleId, "AspNetRoles", "Id", onDelete: ReferentialAction.Cascade);
                    table.ForeignKey("FK_AspNetUserRoles_AspNetUsers_UserId",
                        x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(name: "AspNetUserTokens",
                columns: table => new
                {
                    UserId = table.Column<string>(nullable: false),
                    LoginProvider = table.Column<string>(nullable: false),
                    Name = table.Column<string>(nullable: false),
                    Value = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AspNetUserTokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey("FK_AspNetUserTokens_AspNetUsers_UserId",
                        x => x.UserId, "AspNetUsers", "Id", onDelete: ReferentialAction.Cascade);
                });

            // App tables
            migrationBuilder.CreateTable(
                name: "ConsumerProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    KongConsumerUsername = table.Column<string>(maxLength: 200, nullable: false),
                    ServiceName = table.Column<string>(maxLength: 200, nullable: false, defaultValue: ""),
                    TeamName = table.Column<string>(maxLength: 200, nullable: false, defaultValue: ""),
                    Environment = table.Column<string>(nullable: false, defaultValue: "Production"),
                    AuthType = table.Column<string>(nullable: false, defaultValue: "key-auth"),
                    Notes = table.Column<string>(nullable: true),
                    ContactName = table.Column<string>(nullable: false, defaultValue: ""),
                    ContactEmail = table.Column<string>(maxLength: 200, nullable: false, defaultValue: ""),
                    ContactPhone = table.Column<string>(nullable: false, defaultValue: ""),
                    ContactSlack = table.Column<string>(nullable: false, defaultValue: ""),
                    LastRotatedAt = table.Column<DateTime>(nullable: true),
                    LastRotatedBy = table.Column<string>(nullable: true),
                    RotationIntervalDays = table.Column<int>(nullable: false, defaultValue: 90),
                    CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    UpdatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()")
                },
                constraints: table => table.PrimaryKey("PK_ConsumerProfiles", x => x.Id));

            migrationBuilder.CreateTable(
                name: "RotationRecords",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    KongConsumerUsername = table.Column<string>(maxLength: 200, nullable: false),
                    AuthType = table.Column<string>(nullable: false, defaultValue: ""),
                    ServiceName = table.Column<string>(nullable: false, defaultValue: ""),
                    ContactEmail = table.Column<string>(nullable: false, defaultValue: ""),
                    OldCredentialIds = table.Column<string>(nullable: false, defaultValue: ""),
                    RotatedBy = table.Column<string>(nullable: false, defaultValue: ""),
                    RotatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Confirmed = table.Column<bool>(nullable: false, defaultValue: false),
                    ConfirmedAt = table.Column<DateTime>(nullable: true),
                    OldCredentialsDeleted = table.Column<bool>(nullable: false, defaultValue: false),
                    DeletedAt = table.Column<DateTime>(nullable: true),
                    DeletedBy = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_RotationRecords", x => x.Id));

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<int>(nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    Action = table.Column<string>(maxLength: 100, nullable: false),
                    Resource = table.Column<string>(maxLength: 300, nullable: false, defaultValue: ""),
                    ResourceType = table.Column<string>(nullable: false, defaultValue: ""),
                    PerformedBy = table.Column<string>(maxLength: 200, nullable: false, defaultValue: ""),
                    IpAddress = table.Column<string>(nullable: false, defaultValue: ""),
                    At = table.Column<DateTime>(nullable: false, defaultValueSql: "GETUTCDATE()"),
                    Success = table.Column<bool>(nullable: false, defaultValue: true),
                    FailureReason = table.Column<string>(nullable: true),
                    Notes = table.Column<string>(nullable: true)
                },
                constraints: table => table.PrimaryKey("PK_AuditLogs", x => x.Id));

            // Indexes
            migrationBuilder.CreateIndex("IX_ConsumerProfiles_Username", "ConsumerProfiles", "KongConsumerUsername", unique: true);
            migrationBuilder.CreateIndex("IX_RotationRecords_Consumer_Date", "RotationRecords", new[] { "KongConsumerUsername", "RotatedAt" });
            migrationBuilder.CreateIndex("IX_AuditLogs_At", "AuditLogs", "At");
            migrationBuilder.CreateIndex("IX_AuditLogs_PerformedBy", "AuditLogs", "PerformedBy");
            migrationBuilder.CreateIndex("IX_AspNetRoleClaims_RoleId", "AspNetRoleClaims", "RoleId");
            migrationBuilder.CreateIndex("IX_AspNetUserClaims_UserId", "AspNetUserClaims", "UserId");
            migrationBuilder.CreateIndex("IX_AspNetUserLogins_UserId", "AspNetUserLogins", "UserId");
            migrationBuilder.CreateIndex("IX_AspNetUserRoles_RoleId", "AspNetUserRoles", "RoleId");
            migrationBuilder.CreateIndex("RoleNameIndex", "AspNetRoles", "NormalizedName", unique: true, filter: "[NormalizedName] IS NOT NULL");
            migrationBuilder.CreateIndex("EmailIndex", "AspNetUsers", "NormalizedEmail");
            migrationBuilder.CreateIndex("UserNameIndex", "AspNetUsers", "NormalizedUserName", unique: true, filter: "[NormalizedUserName] IS NOT NULL");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("AuditLogs");
            migrationBuilder.DropTable("ConsumerProfiles");
            migrationBuilder.DropTable("RotationRecords");
            migrationBuilder.DropTable("AspNetUserTokens");
            migrationBuilder.DropTable("AspNetUserRoles");
            migrationBuilder.DropTable("AspNetUserLogins");
            migrationBuilder.DropTable("AspNetUserClaims");
            migrationBuilder.DropTable("AspNetRoleClaims");
            migrationBuilder.DropTable("AspNetUsers");
            migrationBuilder.DropTable("AspNetRoles");
        }
    }
}
