using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.Migrations
{
    /// <inheritdoc />
    public partial class ValueType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ValueType",
                table: "Variables",
                type: "nvarchar(16)",
                maxLength: 16,
                nullable: false,
                defaultValue: "String");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ValueType",
                table: "Variables");
        }
    }
}
