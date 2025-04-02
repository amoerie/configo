using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.NpgSql.Migrations
{
    /// <inheritdoc />
    public partial class DropApiKeyTagOrder : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Order",
                table: "ApiKeyTags");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "ApiKeyTags",
                type: "integer",
                nullable: false,
                defaultValue: 0);
        }
    }
}
