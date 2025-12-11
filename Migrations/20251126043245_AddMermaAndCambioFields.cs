using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMermaAndCambioFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "cambio",
                table: "ventas",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cliente_id",
                table: "ventas",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "monto_recibido",
                table: "ventas",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "cantidad_inicial",
                table: "productos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "merma",
                table: "productos",
                type: "int",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "cambio",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "cliente_id",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "monto_recibido",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "cantidad_inicial",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "merma",
                table: "productos");
        }
    }
}
