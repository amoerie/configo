using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Configo.Database.NpgSql.Migrations
{
    /// <inheritdoc />
    public partial class Order : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Order",
                table: "TagGroups",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateIndex(
                name: "IX_TagGroups_Order",
                table: "TagGroups",
                column: "Order",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_TagGroups_Order",
                table: "TagGroups");

            migrationBuilder.DropColumn(
                name: "Order",
                table: "TagGroups");
        }
    }
}
