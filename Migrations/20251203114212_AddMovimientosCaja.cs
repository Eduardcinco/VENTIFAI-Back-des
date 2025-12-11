using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace VentifyAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMovimientosCaja : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_ventas_clientes_cliente_id",
                table: "ventas");

            migrationBuilder.DropTable(
                name: "clientes");

            migrationBuilder.DropIndex(
                name: "IX_ventas_cliente_id",
                table: "ventas");

            migrationBuilder.DropColumn(
                name: "cliente_id",
                table: "ventas");

            migrationBuilder.RenameColumn(
                name: "UsuarioCierreId",
                table: "cajas",
                newName: "usuario_cierre_id");

            migrationBuilder.CreateTable(
                name: "movimientos_caja",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    caja_id = table.Column<int>(type: "int", nullable: false),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    usuario_id = table.Column<int>(type: "int", nullable: false),
                    tipo = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    monto = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    categoria = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    descripcion = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    metodo_pago = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_hora = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    saldo_despues = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    referencia = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_movimientos_caja", x => x.id);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_cajas_caja_id",
                        column: x => x.caja_id,
                        principalTable: "cajas",
                        principalColumn: "id_caja",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_movimientos_caja_usuarios_usuario_id",
                        column: x => x.usuario_id,
                        principalTable: "usuarios",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_caja_id_fecha_hora",
                table: "movimientos_caja",
                columns: new[] { "caja_id", "fecha_hora" });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_negocio_id_fecha_hora",
                table: "movimientos_caja",
                columns: new[] { "negocio_id", "fecha_hora" });

            migrationBuilder.CreateIndex(
                name: "IX_movimientos_caja_usuario_id",
                table: "movimientos_caja",
                column: "usuario_id");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "movimientos_caja");

            migrationBuilder.RenameColumn(
                name: "usuario_cierre_id",
                table: "cajas",
                newName: "UsuarioCierreId");

            migrationBuilder.AddColumn<int>(
                name: "cliente_id",
                table: "ventas",
                type: "int",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "clientes",
                columns: table => new
                {
                    id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    activo = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true),
                    correo = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    direccion = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    fecha_creacion = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    limite_credito = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    negocio_id = table.Column<int>(type: "int", nullable: false),
                    nombre_completo = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    notas = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    rfc = table.Column<string>(type: "varchar(13)", maxLength: 13, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    saldo_actual = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    telefono = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_clientes", x => x.id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_ventas_cliente_id",
                table: "ventas",
                column: "cliente_id");

            migrationBuilder.CreateIndex(
                name: "IX_clientes_negocio_id_nombre_completo",
                table: "clientes",
                columns: new[] { "negocio_id", "nombre_completo" });

            migrationBuilder.AddForeignKey(
                name: "FK_ventas_clientes_cliente_id",
                table: "ventas",
                column: "cliente_id",
                principalTable: "clientes",
                principalColumn: "id");
        }
    }
}
