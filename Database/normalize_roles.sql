-- Script para normalizar roles en la base de datos
-- Ejecutar esto en Railway para asegurar consistencia

-- Normalizar tabla usuarios
UPDATE usuarios SET rol = 'dueño' WHERE LOWER(rol) IN ('dueno', 'dueño', 'owner', 'admin');
UPDATE usuarios SET rol = 'gerente' WHERE LOWER(rol) IN ('gerente', 'manager');
UPDATE usuarios SET rol = 'cajero' WHERE LOWER(rol) IN ('cajero', 'cashier');
UPDATE usuarios SET rol = 'almacenista' WHERE LOWER(rol) IN ('almacenista', 'almacen', 'bodega', 'warehouse');

-- Verificar que todos los roles sean válidos
SELECT DISTINCT rol FROM usuarios;
