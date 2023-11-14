using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.SqlServer.Migrations
{
    /// <inheritdoc />
    public partial class TagGroupIcon : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Icon",
                table: "TagGroups",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "fa-tags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Icon",
                table: "TagGroups");
        }
    }
}
