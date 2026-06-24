using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppInventory.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class Auth_InitialSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Permissions",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ResourceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Permissions", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsSystemRole = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DisplayName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                Email = table.Column<string>(type: "character varying(320)", maxLength: 320, nullable: false),
                IsActive = table.Column<bool>(type: "boolean", nullable: false),
                LastLogin = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "GroupRoleMappings",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProviderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ExternalGroupRef = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                RoleId = table.Column<int>(type: "integer", nullable: false),
                Description = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(type: "boolean", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_GroupRoleMappings", x => x.Id);
                table.ForeignKey(
                    name: "FK_GroupRoleMappings_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            columns: table => new
            {
                RoleId = table.Column<int>(type: "integer", nullable: false),
                PermissionId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                table.ForeignKey(
                    name: "FK_RolePermissions_Permissions_PermissionId",
                    column: x => x.PermissionId,
                    principalTable: "Permissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RolePermissions_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ExternalIdentities",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                ProviderType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                ExternalId = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                ExternalGroupsJson = table.Column<string>(type: "text", nullable: true),
                LastSyncedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ExternalIdentities", x => x.Id);
                table.ForeignKey(
                    name: "FK_ExternalIdentities_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "LocalCredentials",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(type: "integer", nullable: false),
                PasswordHash = table.Column<string>(type: "text", nullable: false),
                MustChangePassword = table.Column<bool>(type: "boolean", nullable: false),
                FailedAttempts = table.Column<int>(type: "integer", nullable: false),
                LockedUntil = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                PasswordUpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_LocalCredentials", x => x.Id);
                table.ForeignKey(
                    name: "FK_LocalCredentials_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                UserId = table.Column<int>(type: "integer", nullable: false),
                RoleId = table.Column<int>(type: "integer", nullable: false),
                GrantedByUserId = table.Column<int>(type: "integer", nullable: true),
                GrantedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                Source = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        // Seed system roles
        migrationBuilder.InsertData(
            table: "Roles",
            columns: new[] { "Id", "Name", "Description", "IsSystemRole" },
            values: new object[,]
            {
                { 1, "Admin", "Full access; user management, group mappings, all CRUD", true },
                { 2, "Developer", "Application CRUD, developer/ops docs read and write", true },
                { 3, "ApplicationOwner", "Edit own applications, write user docs", true },
                { 4, "ReadOnly", "Read all public content (user docs, catalog)", true }
            });

        // Indexes
        migrationBuilder.CreateIndex(name: "IX_ExternalIdentities_ProviderType_ExternalId", table: "ExternalIdentities", columns: new[] { "ProviderType", "ExternalId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_ExternalIdentities_UserId", table: "ExternalIdentities", column: "UserId");
        migrationBuilder.CreateIndex(name: "IX_GroupRoleMappings_ProviderType_ExternalGroupRef_RoleId", table: "GroupRoleMappings", columns: new[] { "ProviderType", "ExternalGroupRef", "RoleId" }, unique: true);
        migrationBuilder.CreateIndex(name: "IX_GroupRoleMappings_RoleId", table: "GroupRoleMappings", column: "RoleId");
        migrationBuilder.CreateIndex(name: "IX_LocalCredentials_UserId", table: "LocalCredentials", column: "UserId", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Permissions_Name", table: "Permissions", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_RolePermissions_PermissionId", table: "RolePermissions", column: "PermissionId");
        migrationBuilder.CreateIndex(name: "IX_Roles_Name", table: "Roles", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_UserRoles_RoleId", table: "UserRoles", column: "RoleId");
        migrationBuilder.CreateIndex(name: "IX_Users_Email", table: "Users", column: "Email", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ExternalIdentities");
        migrationBuilder.DropTable(name: "LocalCredentials");
        migrationBuilder.DropTable(name: "UserRoles");
        migrationBuilder.DropTable(name: "RolePermissions");
        migrationBuilder.DropTable(name: "GroupRoleMappings");
        migrationBuilder.DropTable(name: "Permissions");
        migrationBuilder.DropTable(name: "Roles");
        migrationBuilder.DropTable(name: "Users");
    }
}
