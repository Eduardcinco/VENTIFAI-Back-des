using Microsoft.EntityFrameworkCore.Migrations;

namespace VentifyAPI.Migrations
{
    public partial class RemoveClientes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Drop foreign keys referencing clientes (e.g., ventas.cliente_id)
            migrationBuilder.Sql(@"
                SET @fk_name := (
                  SELECT CONSTRAINT_NAME
                  FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'ventas'
                    AND COLUMN_NAME = 'cliente_id'
                    AND REFERENCED_TABLE_NAME = 'clientes'
                  LIMIT 1
                );
                SET @drop_fk := IF(@fk_name IS NOT NULL, CONCAT('ALTER TABLE `ventas` DROP FOREIGN KEY `', @fk_name, '`'), 'SELECT 1');
                PREPARE stmt FROM @drop_fk; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            // Drop index on cliente_id if exists
            migrationBuilder.Sql(@"
                SET @idx_name := (
                  SELECT INDEX_NAME
                  FROM INFORMATION_SCHEMA.STATISTICS
                  WHERE TABLE_SCHEMA = DATABASE()
                    AND TABLE_NAME = 'ventas'
                    AND COLUMN_NAME = 'cliente_id'
                  LIMIT 1
                );
                SET @drop_idx := IF(@idx_name IS NOT NULL, CONCAT('ALTER TABLE `ventas` DROP INDEX `', @idx_name, '`'), 'SELECT 1');
                PREPARE stmt FROM @drop_idx; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            // Drop column cliente_id if exists
            migrationBuilder.Sql(@"
                SET @col_exists := (
                  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ventas' AND COLUMN_NAME = 'cliente_id'
                );
                SET @drop_col := IF(@col_exists > 0, 'ALTER TABLE `ventas` DROP COLUMN `cliente_id`', 'SELECT 1');
                PREPARE stmt FROM @drop_col; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");

            // Finally drop clientes table if exists
            migrationBuilder.Sql("DROP TABLE IF EXISTS `clientes`;");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Minimal recreation to allow rollback (optional)
            migrationBuilder.Sql(@"
                CREATE TABLE IF NOT EXISTS `clientes` (
                  `id` int NOT NULL AUTO_INCREMENT,
                  `negocio_id` int NOT NULL,
                  `nombre_completo` varchar(200) NOT NULL,
                  `telefono` varchar(20) NULL,
                  `rfc` varchar(13) NULL,
                  `correo` varchar(150) NULL,
                  `direccion` text NULL,
                  `limite_credito` decimal(18,2) NULL,
                  `saldo_actual` decimal(18,2) NULL,
                  `notas` text NULL,
                  `fecha_creacion` datetime NULL,
                  `activo` tinyint(1) NOT NULL DEFAULT 1,
                  PRIMARY KEY (`id`),
                  INDEX `idx_clientes_negocio_nombre` (`negocio_id`, `nombre_completo`)
                ) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4;
            ");

            // Re-add column in ventas
            migrationBuilder.Sql(@"
                SET @col_exists := (
                  SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS
                  WHERE TABLE_SCHEMA = DATABASE() AND TABLE_NAME = 'ventas' AND COLUMN_NAME = 'cliente_id'
                );
                SET @add_col := IF(@col_exists = 0, 'ALTER TABLE `ventas` ADD COLUMN `cliente_id` int NULL', 'SELECT 1');
                PREPARE stmt FROM @add_col; EXECUTE stmt; DEALLOCATE PREPARE stmt;
            ");
        }
    }
}
