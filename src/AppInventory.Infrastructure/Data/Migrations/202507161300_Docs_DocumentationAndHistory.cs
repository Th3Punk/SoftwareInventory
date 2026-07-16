using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace AppInventory.Infrastructure.Data.Migrations;

/// <inheritdoc />
public partial class Docs_DocumentationAndHistory : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Documentations",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                ApplicationId = table.Column<int>(type: "integer", nullable: false),
                Title = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                Type = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                AuthorUserId = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Documentations", x => x.Id);
                table.ForeignKey(
                    name: "FK_Documentations_Applications_ApplicationId",
                    column: x => x.ApplicationId,
                    principalTable: "Applications",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_Documentations_Users_AuthorUserId",
                    column: x => x.AuthorUserId,
                    principalTable: "Users",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "DocumentationHistories",
            columns: table => new
            {
                Id = table.Column<int>(type: "integer", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                DocumentationId = table.Column<int>(type: "integer", nullable: false),
                Content = table.Column<string>(type: "text", nullable: false),
                Version = table.Column<int>(type: "integer", nullable: false),
                ArchivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                ArchivedByUserId = table.Column<int>(type: "integer", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_DocumentationHistories", x => x.Id);
                table.ForeignKey(
                    name: "FK_DocumentationHistories_Documentations_DocumentationId",
                    column: x => x.DocumentationId,
                    principalTable: "Documentations",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Documentations_ApplicationId",
            table: "Documentations",
            column: "ApplicationId");

        migrationBuilder.CreateIndex(
            name: "IX_Documentations_AuthorUserId",
            table: "Documentations",
            column: "AuthorUserId");

        migrationBuilder.CreateIndex(
            name: "IX_Documentations_Type",
            table: "Documentations",
            column: "Type");

        migrationBuilder.CreateIndex(
            name: "IX_Documentations_Status",
            table: "Documentations",
            column: "Status");

        migrationBuilder.CreateIndex(
            name: "IX_DocumentationHistories_DocumentationId",
            table: "DocumentationHistories",
            column: "DocumentationId");

        migrationBuilder.CreateIndex(
            name: "IX_DocumentationHistories_DocumentationId_Version",
            table: "DocumentationHistories",
            columns: ["DocumentationId", "Version"],
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "DocumentationHistories");
        migrationBuilder.DropTable(name: "Documentations");
    }
}
