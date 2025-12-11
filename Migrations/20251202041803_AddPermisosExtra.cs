using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddPermisosExtra : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PermisosExtra",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.AddColumn<int>(
                name: "PermisosExtraAsignadoPor",
                table: "usuarios",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "PermisosExtraFecha",
                table: "usuarios",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PermisosExtraNota",
                table: "usuarios",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PermisosExtra",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "PermisosExtraAsignadoPor",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "PermisosExtraFecha",
                table: "usuarios");

            migrationBuilder.DropColumn(
                name: "PermisosExtraNota",
                table: "usuarios");
        }
    }
}
