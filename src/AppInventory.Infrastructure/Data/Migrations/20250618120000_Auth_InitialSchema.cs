using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable enable

namespace AppInventory.Infrastructure.Data.Migrations;

public partial class Auth_InitialSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Users",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DisplayName = table.Column<string>(maxLength: 200, nullable: false),
                Email = table.Column<string>(maxLength: 320, nullable: false),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true),
                LastLogin = table.Column<DateTime>(nullable: true),
                CreatedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "now()")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Users", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Users_Email",
            table: "Users",
            column: "Email",
            unique: true);

        migrationBuilder.CreateTable(
            name: "Roles",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(maxLength: 100, nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                IsSystemRole = table.Column<bool>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Roles", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Roles_Name",
            table: "Roles",
            column: "Name",
            unique: true);

        migrationBuilder.InsertData(
            table: "Roles",
            columns: new[] { "Id", "Name", "Description", "IsSystemRole" },
            values: new object[,]
            {
                { 1, "Admin", "Full system access", true },
                { 2, "Developer", "Developer access to applications and documentation", true },
                { 3, "ApplicationOwner", "Manage owned applications", true },
                { 4, "ReadOnly", "Read-only access", true }
            });

        migrationBuilder.CreateTable(
            name: "Permissions",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(maxLength: 200, nullable: false),
                ResourceType = table.Column<string>(maxLength: 100, nullable: false),
                Action = table.Column<string>(maxLength: 100, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Permissions", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Permissions_Name",
            table: "Permissions",
            column: "Name",
            unique: true);

        migrationBuilder.CreateTable(
            name: "ExternalIdentities",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(nullable: false),
                ProviderType = table.Column<string>(maxLength: 50, nullable: false),
                ExternalId = table.Column<string>(maxLength: 500, nullable: false),
                ExternalGroupsJson = table.Column<string>(nullable: true),
                LastSyncedAt = table.Column<DateTime>(nullable: false)
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

        migrationBuilder.CreateIndex(
            name: "IX_ExternalIdentities_ProviderType_ExternalId",
            table: "ExternalIdentities",
            columns: new[] { "ProviderType", "ExternalId" },
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_ExternalIdentities_UserId",
            table: "ExternalIdentities",
            column: "UserId");

        migrationBuilder.CreateTable(
            name: "LocalCredentials",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                UserId = table.Column<int>(nullable: false),
                PasswordHash = table.Column<string>(nullable: false),
                MustChangePassword = table.Column<bool>(nullable: false, defaultValue: true),
                FailedAttempts = table.Column<int>(nullable: false, defaultValue: 0),
                LockedUntil = table.Column<DateTime>(nullable: true),
                PasswordUpdatedAt = table.Column<DateTime>(nullable: false)
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

        migrationBuilder.CreateIndex(
            name: "IX_LocalCredentials_UserId",
            table: "LocalCredentials",
            column: "UserId",
            unique: true);

        migrationBuilder.CreateTable(
            name: "RolePermissions",
            columns: table => new
            {
                RoleId = table.Column<int>(nullable: false),
                PermissionId = table.Column<int>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RolePermissions", x => new { x.RoleId, x.PermissionId });
                table.ForeignKey(
                    name: "FK_RolePermissions_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RolePermissions_Permissions_PermissionId",
                    column: x => x.PermissionId,
                    principalTable: "Permissions",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RolePermissions_PermissionId",
            table: "RolePermissions",
            column: "PermissionId");

        migrationBuilder.CreateTable(
            name: "UserRoles",
            columns: table => new
            {
                UserId = table.Column<int>(nullable: false),
                RoleId = table.Column<int>(nullable: false),
                GrantedByUserId = table.Column<int>(nullable: true),
                GrantedAt = table.Column<DateTime>(nullable: false, defaultValueSql: "now()"),
                Source = table.Column<string>(maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_UserRoles", x => new { x.UserId, x.RoleId });
                table.ForeignKey(
                    name: "FK_UserRoles_Users_UserId",
                    column: x => x.UserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_UserRoles_Roles_RoleId",
                    column: x => x.RoleId,
                    principalTable: "Roles",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_UserRoles_RoleId",
            table: "UserRoles",
            column: "RoleId");

        migrationBuilder.CreateTable(
            name: "GroupRoleMappings",
            columns: table => new
            {
                Id = table.Column<int>(nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ProviderType = table.Column<string>(maxLength: 50, nullable: false),
                ExternalGroupRef = table.Column<string>(maxLength: 500, nullable: false),
                RoleId = table.Column<int>(nullable: false),
                Description = table.Column<string>(maxLength: 500, nullable: true),
                IsActive = table.Column<bool>(nullable: false, defaultValue: true)
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

        migrationBuilder.CreateIndex(
            name: "IX_GroupRoleMappings_RoleId",
            table: "GroupRoleMappings",
            column: "RoleId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "GroupRoleMappings");
        migrationBuilder.DropTable(name: "UserRoles");
        migrationBuilder.DropTable(name: "RolePermissions");
        migrationBuilder.DropTable(name: "LocalCredentials");
        migrationBuilder.DropTable(name: "ExternalIdentities");
        migrationBuilder.DropTable(name: "Permissions");
        migrationBuilder.DropTable(name: "Roles");
        migrationBuilder.DropTable(name: "Users");
    }
}
