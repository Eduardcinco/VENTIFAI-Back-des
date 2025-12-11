using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddEmpleadoFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Apellido1",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Apellido2",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<decimal>(
                name: "SueldoDiario",
                table: "usuarios",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Apellido1",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "Apellido2",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "SueldoDiario",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "usuarios");
        }
    }
}
