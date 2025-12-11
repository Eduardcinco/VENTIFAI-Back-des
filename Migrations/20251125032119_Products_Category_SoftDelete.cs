using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class Products_Category_SoftDelete : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "category_id",
                table: "productos",
                type: "int",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_productos_category_id",
                table: "productos",
                column: "category_id");

            migrationBuilder.AddForeignKey(
                name: "FK_productos_categories_category_id",
                table: "productos",
                column: "category_id",
                principalTable: "categories",
                principalColumn: "id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_productos_categories_category_id",
                table: "productos");

            migrationBuilder.DropIndex(
                name: "IX_productos_category_id",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "category_id",
                table: "productos");
        }
    }
}
