-- Script SQL para agregar tabla de movimientos de caja
-- Ejecutar manualmente o crear migración con Entity Framework

-- Agregar columna usuario_cierre_id a tabla cajas (si no existe)
ALTER TABLE cajas 
ADD COLUMN IF NOT EXISTS usuario_cierre_id INT NULL;

-- Crear tabla movimientos_caja
CREATE TABLE IF NOT EXISTS movimientos_caja (
    id INT AUTO_INCREMENT PRIMARY KEY,
    caja_id INT NOT NULL,
    negocio_id INT NOT NULL,
    usuario_id INT NOT NULL,
    tipo VARCHAR(20) NOT NULL COMMENT 'entrada o salida',
    monto DECIMAL(10, 2) NOT NULL,
    categoria VARCHAR(100) NOT NULL,
    descripcion VARCHAR(500) NULL,
    metodo_pago VARCHAR(50) NULL DEFAULT 'Efectivo',
    fecha_hora DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
    saldo_despues DECIMAL(10, 2) NOT NULL,
    referencia VARCHAR(100) NULL,
    
    FOREIGN KEY (caja_id) REFERENCES cajas(id_caja) ON DELETE CASCADE,
    FOREIGN KEY (usuario_id) REFERENCES usuarios(id) ON DELETE RESTRICT,
    
    INDEX idx_caja_fecha (caja_id, fecha_hora),
    INDEX idx_negocio_fecha (negocio_id, fecha_hora),
    INDEX idx_tipo (tipo)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci;
