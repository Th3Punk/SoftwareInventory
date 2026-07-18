using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppInventory.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class Catalog_ApplicationSchema : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                Color = table.Column<string>(type: "character varying(7)", maxLength: 7, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Tags", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "Applications",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                ShortDescription = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                DetailedDescription = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                OwnerTeam = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                SourceControl = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                RepositoryUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                WikiUrl = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true),
                IsDeleted = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                CreatedByUserId = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Applications", x => x.Id);
                table.ForeignKey(
                    name: "FK_Applications_Users_CreatedByUserId",
                    column: x => x.CreatedByUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "ApplicationContacts",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApplicationId = table.Column<int>(type: "integer", nullable: false),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Value = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Label = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApplicationContacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApplicationContacts_Applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "Applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ApplicationEnvironments",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApplicationId = table.Column<int>(type: "integer", nullable: false),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Url = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                Notes = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                IsPublic = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApplicationEnvironments", x => x.Id);
                table.ForeignKey(
                    name: "FK_ApplicationEnvironments_Applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "Applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "ApplicationTags",
            columns: table => new
            {
                ApplicationId = table.Column<int>(type: "integer", nullable: false),
                TagId = table.Column<int>(type: "integer", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ApplicationTags", x => new { x.ApplicationId, x.TagId });
                table.ForeignKey(
                    name: "FK_ApplicationTags_Applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "Applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_ApplicationTags_Tags_TagId",
                    column: x => x.TagId,
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(name: "IX_Applications_Name", table: "Applications", column: "Name", unique: true);
        migrationBuilder.CreateIndex(name: "IX_Applications_OwnerTeam", table: "Applications", column: "OwnerTeam");
        migrationBuilder.CreateIndex(name: "IX_Applications_Status", table: "Applications", column: "Status");
        migrationBuilder.CreateIndex(name: "IX_Applications_IsDeleted", table: "Applications", column: "IsDeleted");
        migrationBuilder.CreateIndex(name: "IX_Applications_CreatedByUserId", table: "Applications", column: "CreatedByUserId");
        migrationBuilder.CreateIndex(name: "IX_ApplicationContacts_ApplicationId", table: "ApplicationContacts", column: "ApplicationId");
        migrationBuilder.CreateIndex(name: "IX_ApplicationEnvironments_ApplicationId", table: "ApplicationEnvironments", column: "ApplicationId");
        migrationBuilder.CreateIndex(name: "IX_ApplicationTags_TagId", table: "ApplicationTags", column: "TagId");
        migrationBuilder.CreateIndex(name: "IX_Tags_Name", table: "Tags", column: "Name", unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "ApplicationTags");
        migrationBuilder.DropTable(name: "ApplicationEnvironments");
        migrationBuilder.DropTable(name: "ApplicationContacts");
        migrationBuilder.DropTable(name: "Applications");
        migrationBuilder.DropTable(name: "Tags");
    }
}
