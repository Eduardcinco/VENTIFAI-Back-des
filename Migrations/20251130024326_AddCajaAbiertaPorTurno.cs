using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddCajaAbiertaPorTurno : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AbiertaPor",
                table: "cajas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Turno",
                table: "cajas",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AbiertaPor",
                table: "cajas");

            migrationBuilder.DropColumn(
                name: "Turno",
                table: "cajas");
        }
    }
}
