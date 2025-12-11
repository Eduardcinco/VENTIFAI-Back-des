using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddSettingsFieldsToNegocioAndUsuario : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "FechaIngreso",
                table: "usuarios",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NumeroSeguroSocial",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Puesto",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RFC",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Correo",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Direccion",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "GiroComercial",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "RFC",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<string>(
                name: "Telefono",
                table: "negocios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FechaIngreso",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "NumeroSeguroSocial",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "Puesto",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "RFC",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "Correo",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "Direccion",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "GiroComercial",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "RFC",
                table: "negocios");

            migrationBuilder.DropColumn(
                name: "Telefono",
                table: "negocios");
        }
    }
}
