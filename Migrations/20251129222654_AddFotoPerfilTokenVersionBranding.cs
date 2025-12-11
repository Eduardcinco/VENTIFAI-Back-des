using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddFotoPerfilTokenVersionBranding : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FotoPerfil",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "TokenVersion",
                table: "usuarios",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ColorAcento",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorFondo",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorPrimario",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "ColorSecundario",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<bool>(
                name: "ModoOscuro",
                table: "negocios",
                type: "tinyint(1)",
                nullable: false,
                defaultValue: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FotoPerfil",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "TokenVersion",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "ColorAcento",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "ColorFondo",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "ColorPrimario",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "ColorSecundario",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "ModoOscuro",
                table: "negocios");
        }
    }
}
