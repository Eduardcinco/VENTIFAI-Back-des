-- =============================================================================
-- VISTAS OPTIMIZADAS PARA REPORTES DE VENTAS
-- =============================================================================
-- Estas vistas proporcionan datos agregados y detallados de ventas
-- Optimizadas para generar reportes en Excel y PDF
-- =============================================================================

USE zona30;

-- =============================================================================
-- VISTA: vista_reporte_ventas_detalle
-- Obtiene el detalle completo de cada venta con toda la información necesaria
-- =============================================================================
DROP VIEW IF EXISTS vista_reporte_ventas_detalle;

CREATE VIEW vista_reporte_ventas_detalle AS
SELECT 
    -- Información de la venta
    v.id AS venta_id,
    v.folio,
    v.fecha_venta,
    v.total,
    v.subtotal,
    v.iva,
    v.descuento_aplicado,
    v.metodo_pago,
    v.tipo_venta,
    v.estado,
    v.notas,
    
    -- Información del negocio
    v.negocio_id,
    n.nombre_negocio,
    
    -- Información del cajero
    v.cajero_id,
    CONCAT(u.nombre, ' ', COALESCE(u.Apellido1, '')) AS nombre_cajero,
    
    -- Información del cliente (si existe)
    v.cliente_id,
    CASE 
        WHEN c.id IS NOT NULL THEN CONCAT(c.nombre, ' ', COALESCE(c.apellido, ''))
        ELSE 'Público General'
    END AS nombre_cliente,
    c.telefono AS telefono_cliente,
    c.correo AS correo_cliente,
    
    -- Detalle de productos vendidos
    dv.id AS detalle_id,
    dv.producto_id,
    dv.variante_id,
    p.nombre AS producto_nombre,
    p.codigo_barras,
    cat.nombre AS categoria_nombre,
    dv.cantidad,
    dv.precio_unitario,
    dv.subtotal_detalle,
    dv.descuento_detalle,
    
    -- Información de variante (si existe)
    vp.nombre AS variante_nombre,
    vp.sku AS variante_sku,
    
    -- Timestamps
    v.created_at AS fecha_creacion
FROM ventas v
INNER JOIN negocios n ON v.negocio_id = n.id
INNER JOIN usuarios u ON v.cajero_id = u.id
LEFT JOIN clientes c ON v.cliente_id = c.id
INNER JOIN detalle_ventas dv ON v.id = dv.venta_id
INNER JOIN productos p ON dv.producto_id = p.id
LEFT JOIN categorias cat ON p.categoria_id = cat.id
LEFT JOIN variantes_producto vp ON dv.variante_id = vp.id
WHERE v.estado IN ('completada', 'pagada')
ORDER BY v.fecha_venta DESC;

-- =============================================================================
-- VISTA: vista_reporte_ventas_agregado_dia
-- Ventas agregadas por día
-- =============================================================================
DROP VIEW IF EXISTS vista_reporte_ventas_agregado_dia;

CREATE VIEW vista_reporte_ventas_agregado_dia AS
SELECT 
    negocio_id,
    DATE(fecha_venta) AS fecha,
    COUNT(DISTINCT id) AS total_ventas,
    SUM(total) AS total_ingresos,
    SUM(subtotal) AS total_subtotal,
    SUM(iva) AS total_iva,
    SUM(descuento_aplicado) AS total_descuentos,
    AVG(total) AS ticket_promedio,
    MAX(total) AS venta_maxima,
    MIN(total) AS venta_minima,
    COUNT(DISTINCT cajero_id) AS cajeros_activos,
    COUNT(DISTINCT CASE WHEN cliente_id IS NOT NULL THEN cliente_id END) AS clientes_unicos,
    
    -- Ventas por método de pago
    SUM(CASE WHEN metodo_pago = 'efectivo' THEN total ELSE 0 END) AS total_efectivo,
    SUM(CASE WHEN metodo_pago = 'tarjeta' THEN total ELSE 0 END) AS total_tarjeta,
    SUM(CASE WHEN metodo_pago = 'transferencia' THEN total ELSE 0 END) AS total_transferencia,
    
    -- Conteo por método de pago
    COUNT(CASE WHEN metodo_pago = 'efectivo' THEN 1 END) AS ventas_efectivo,
    COUNT(CASE WHEN metodo_pago = 'tarjeta' THEN 1 END) AS ventas_tarjeta,
    COUNT(CASE WHEN metodo_pago = 'transferencia' THEN 1 END) AS ventas_transferencia
FROM ventas
WHERE estado IN ('completada', 'pagada')
GROUP BY negocio_id, DATE(fecha_venta)
ORDER BY fecha DESC;

-- =============================================================================
-- VISTA: vista_reporte_ventas_agregado_semana
-- Ventas agregadas por semana
-- =============================================================================
DROP VIEW IF EXISTS vista_reporte_ventas_agregado_semana;

CREATE VIEW vista_reporte_ventas_agregado_semana AS
SELECT 
    negocio_id,
    YEAR(fecha_venta) AS anio,
    WEEK(fecha_venta, 1) AS semana,
    DATE(DATE_SUB(fecha_venta, INTERVAL WEEKDAY(fecha_venta) DAY)) AS fecha_inicio_semana,
    DATE(DATE_ADD(DATE_SUB(fecha_venta, INTERVAL WEEKDAY(fecha_venta) DAY), INTERVAL 6 DAY)) AS fecha_fin_semana,
    COUNT(DISTINCT id) AS total_ventas,
    SUM(total) AS total_ingresos,
    SUM(subtotal) AS total_subtotal,
    SUM(iva) AS total_iva,
    SUM(descuento_aplicado) AS total_descuentos,
    AVG(total) AS ticket_promedio,
    COUNT(DISTINCT cajero_id) AS cajeros_activos,
    COUNT(DISTINCT CASE WHEN cliente_id IS NOT NULL THEN cliente_id END) AS clientes_unicos,
    
    -- Ventas por método de pago
    SUM(CASE WHEN metodo_pago = 'efectivo' THEN total ELSE 0 END) AS total_efectivo,
    SUM(CASE WHEN metodo_pago = 'tarjeta' THEN total ELSE 0 END) AS total_tarjeta,
    SUM(CASE WHEN metodo_pago = 'transferencia' THEN total ELSE 0 END) AS total_transferencia
FROM ventas
WHERE estado IN ('completada', 'pagada')
GROUP BY negocio_id, YEAR(fecha_venta), WEEK(fecha_venta, 1)
ORDER BY anio DESC, semana DESC;

-- =============================================================================
-- VISTA: vista_reporte_ventas_agregado_mes
-- Ventas agregadas por mes
-- =============================================================================
DROP VIEW IF EXISTS vista_reporte_ventas_agregado_mes;

CREATE VIEW vista_reporte_ventas_agregado_mes AS
SELECT 
    negocio_id,
    YEAR(fecha_venta) AS anio,
    MONTH(fecha_venta) AS mes,
    DATE_FORMAT(fecha_venta, '%Y-%m-01') AS fecha_inicio_mes,
    LAST_DAY(fecha_venta) AS fecha_fin_mes,
    COUNT(DISTINCT id) AS total_ventas,
    SUM(total) AS total_ingresos,
    SUM(subtotal) AS total_subtotal,
    SUM(iva) AS total_iva,
    SUM(descuento_aplicado) AS total_descuentos,
    AVG(total) AS ticket_promedio,
    COUNT(DISTINCT cajero_id) AS cajeros_activos,
    COUNT(DISTINCT CASE WHEN cliente_id IS NOT NULL THEN cliente_id END) AS clientes_unicos,
    
    -- Ventas por método de pago
    SUM(CASE WHEN metodo_pago = 'efectivo' THEN total ELSE 0 END) AS total_efectivo,
    SUM(CASE WHEN metodo_pago = 'tarjeta' THEN total ELSE 0 END) AS total_tarjeta,
    SUM(CASE WHEN metodo_pago = 'transferencia' THEN total ELSE 0 END) AS total_transferencia
FROM ventas
WHERE estado IN ('completada', 'pagada')
GROUP BY negocio_id, YEAR(fecha_venta), MONTH(fecha_venta)
ORDER BY anio DESC, mes DESC;

-- =============================================================================
-- VISTA: vista_reporte_ventas_agregado_anio
-- Ventas agregadas por año
-- =============================================================================
DROP VIEW IF EXISTS vista_reporte_ventas_agregado_anio;

CREATE VIEW vista_reporte_ventas_agregado_anio AS
SELECT 
    negocio_id,
    YEAR(fecha_venta) AS anio,
    COUNT(DISTINCT id) AS total_ventas,
    SUM(total) AS total_ingresos,
    SUM(subtotal) AS total_subtotal,
    SUM(iva) AS total_iva,
    SUM(descuento_aplicado) AS total_descuentos,
    AVG(total) AS ticket_promedio,
    COUNT(DISTINCT cajero_id) AS cajeros_activos,
    COUNT(DISTINCT CASE WHEN cliente_id IS NOT NULL THEN cliente_id END) AS clientes_unicos,
    
    -- Ventas por método de pago
    SUM(CASE WHEN metodo_pago = 'efectivo' THEN total ELSE 0 END) AS total_efectivo,
    SUM(CASE WHEN metodo_pago = 'tarjeta' THEN total ELSE 0 END) AS total_tarjeta,
    SUM(CASE WHEN metodo_pago = 'transferencia' THEN total ELSE 0 END) AS total_transferencia
FROM ventas
WHERE estado IN ('completada', 'pagada')
GROUP BY negocio_id, YEAR(fecha_venta)
ORDER BY anio DESC;

-- =============================================================================
-- VISTA: vista_productos_mas_vendidos
-- Top productos más vendidos por período
-- =============================================================================
DROP VIEW IF EXISTS vista_productos_mas_vendidos;

CREATE VIEW vista_productos_mas_vendidos AS
SELECT 
    v.negocio_id,
    DATE(v.fecha_venta) AS fecha,
    p.id AS producto_id,
    p.nombre AS producto_nombre,
    p.codigo_barras,
    cat.nombre AS categoria_nombre,
    SUM(dv.cantidad) AS cantidad_vendida,
    SUM(dv.subtotal_detalle) AS total_ventas,
    COUNT(DISTINCT v.id) AS numero_transacciones,
    AVG(dv.precio_unitario) AS precio_promedio
FROM ventas v
INNER JOIN detalle_ventas dv ON v.id = dv.venta_id
INNER JOIN productos p ON dv.producto_id = p.id
LEFT JOIN categorias cat ON p.categoria_id = cat.id
WHERE v.estado IN ('completada', 'pagada')
GROUP BY v.negocio_id, DATE(v.fecha_venta), p.id, p.nombre, p.codigo_barras, cat.nombre
ORDER BY cantidad_vendida DESC;

-- =============================================================================
-- ÍNDICES PARA OPTIMIZAR CONSULTAS DE REPORTES
-- =============================================================================

-- Índice compuesto para filtros por negocio y fecha
CREATE INDEX IF NOT EXISTS idx_ventas_negocio_fecha 
ON ventas(negocio_id, fecha_venta, estado);

-- Índice para búsquedas por folio
CREATE INDEX IF NOT EXISTS idx_ventas_folio 
ON ventas(folio);

-- Índice para detalle de ventas
CREATE INDEX IF NOT EXISTS idx_detalle_ventas_venta_id 
ON detalle_ventas(venta_id);

-- =============================================================================
-- VERIFICACIÓN
-- =============================================================================
SELECT 'Vistas creadas exitosamente' AS resultado;
SELECT COUNT(*) AS total_ventas FROM vista_reporte_ventas_detalle;
