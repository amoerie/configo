using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.Migrations
{
    /// <inheritdoc />
    public partial class TagGroups : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Variables",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Variables",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateTime>(
                name: "ActiveFromUtc",
                table: "Variables",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc),
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Tags",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "TagGroupId",
                table: "Tags",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Tags",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Applications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Applications",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.CreateTable(
                name: "TagGroups",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Name = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false, defaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc))
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TagGroups", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Tags_TagGroupId",
                table: "Tags",
                column: "TagGroupId");

            migrationBuilder.AddForeignKey(
                name: "FK_Tags_TagGroups_TagGroupId",
                table: "Tags",
                column: "TagGroupId",
                principalTable: "TagGroups",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Tags_TagGroups_TagGroupId",
                table: "Tags");

            migrationBuilder.DropTable(
                name: "TagGroups");

            migrationBuilder.DropIndex(
                name: "IX_Tags_TagGroupId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "TagGroupId",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Tags");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Applications");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Applications");

            migrationBuilder.AlterColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Variables",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AlterColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Variables",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));

            migrationBuilder.AlterColumn<DateTime>(
                name: "ActiveFromUtc",
                table: "Variables",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2",
                oldDefaultValue: new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
        }
    }
}
