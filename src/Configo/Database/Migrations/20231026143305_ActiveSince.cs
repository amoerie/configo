using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.Migrations
{
    /// <inheritdoc />
    public partial class ActiveSince : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ActiveFromUtc",
                table: "Variables");

            migrationBuilder.DropColumn(
                name: "Name",
                table: "ApiKeys");

            migrationBuilder.RenameColumn(
                name: "ActiveFromUtc",
                table: "ApiKeys",
                newName: "ActiveSinceUtc");

            migrationBuilder.CreateTable(
                name: "ApiKeyTags",
                columns: table => new
                {
                    ApiKeyId = table.Column<int>(type: "int", nullable: false),
                    TagId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeyTags", x => new { x.ApiKeyId, x.TagId });
                    table.ForeignKey(
                        name: "FK_ApiKeyTags_ApiKeys_ApiKeyId",
                        column: x => x.ApiKeyId,
                        principalTable: "ApiKeys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ApiKeyTags_Tags_TagId",
                        column: x => x.TagId,
                        principalTable: "Tags",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeyTags_TagId",
                table: "ApiKeyTags",
                column: "TagId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ApiKeyTags");

            migrationBuilder.RenameColumn(
                name: "ActiveSinceUtc",
                table: "ApiKeys",
                newName: "ActiveFromUtc");

            migrationBuilder.AddColumn<DateTime>(
                name: "ActiveFromUtc",
                table: "Variables",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "ApiKeys",
                type: "nvarchar(256)",
                maxLength: 256,
                nullable: false,
                defaultValue: "");
        }
    }
}
