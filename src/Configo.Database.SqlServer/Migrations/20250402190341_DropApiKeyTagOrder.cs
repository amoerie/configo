using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.SqlServer.Migrations
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
                type: "int",
                nullable: false,
                defaultValue: 0);
        }
    }
}
