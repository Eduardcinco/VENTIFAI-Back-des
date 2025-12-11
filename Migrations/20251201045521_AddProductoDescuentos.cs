using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddProductoDescuentos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "descuento_fecha_fin",
                table: "productos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "descuento_fecha_inicio",
                table: "productos",
                type: "datetime(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "descuento_hora_fin",
                table: "productos",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<TimeSpan>(
                name: "descuento_hora_inicio",
                table: "productos",
                type: "time(6)",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "descuento_porcentaje",
                table: "productos",
                type: "decimal(65,30)",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "negocio_id",
                table: "productos",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "merma_eventos",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    producto_id = table.Column<int>(type: "int", nullable: false),
                    cantidad = table.Column<int>(type: "int", nullable: false),
                    motivo = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    fecha_utc = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    stock_antes = table.Column<int>(type: "int", nullable: false),
                    stock_despues = table.Column<int>(type: "int", nullable: false),
                    merma_antes = table.Column<int>(type: "int", nullable: false),
                    merma_despues = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_merma_eventos", x => x.id);
                    table.ForeignKey(
                        name: "FK_merma_eventos_productos_producto_id",
                        column: x => x.producto_id,
                        principalTable: "productos",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_merma_eventos_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_cliente_id",
                table: "ventas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_merma_eventos_producto_id",
                table: "merma_eventos",
                column: "producto_id");

            migrationBuilder.CreateIndex(
                name: "IX_merma_eventos_usuario_id",
                table: "merma_eventos",
                column: "usuario_id");

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_clientes_cliente_id",
                table: "ventas",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ventas_clientes_cliente_id",
                table: "ventas");

            migrationBuilder.DropTable(
                name: "merma_eventos");

            migrationBuilder.DropIndex(
                name: "IX_ventas_cliente_id",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "descuento_fecha_fin",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "descuento_fecha_inicio",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "descuento_hora_fin",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "descuento_hora_inicio",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "descuento_porcentaje",
                table: "productos");

            migrationBuilder.DropColumn(
                name: "negocio_id",
                table: "productos");
        }
    }
}
