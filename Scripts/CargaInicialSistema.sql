-- Ejecutar manualmente en phpMyAdmin sobre sistema_web.
-- Compatible con MariaDB 10.4. No borra datos ni modifica registros existentes.
USE sistema_web;

-- Bajas lógicas. Las columnas nuevas quedan activas para preservar disponibilidad
-- de los registros que ya existen.
ALTER TABLE categoria ADD COLUMN IF NOT EXISTS activo TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE producto ADD COLUMN IF NOT EXISTS activo TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE proveedor ADD COLUMN IF NOT EXISTS activo TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE cliente ADD COLUMN IF NOT EXISTS activo TINYINT(1) NOT NULL DEFAULT 1;
ALTER TABLE rol ADD COLUMN IF NOT EXISTS activo TINYINT(1) NOT NULL DEFAULT 1;

START TRANSACTION;

-- Roles utilizados por [Authorize]. INSERT IGNORE conserva cualquier rol existente.
INSERT IGNORE INTO rol (nombre, descripcion, activo) VALUES
('Administrador', 'Administración completa del sistema', 1),
('Vendedor', 'Operación de ventas', 1);
SET @rol_admin = (SELECT idrol FROM rol WHERE nombre = 'Administrador' ORDER BY idrol LIMIT 1);
SET @rol_vendedor = (SELECT idrol FROM rol WHERE nombre = 'Vendedor' ORDER BY idrol LIMIT 1);

-- Contraseñas iniciales solicitadas: administrador 21436587; vendedores 12345678.
-- Se almacenan únicamente hashes de ASP.NET Core PasswordHasher; INSERT IGNORE no sobrescribe cuentas.
INSERT IGNORE INTO usuario (nombre, apellido, correo, password, estado, rol_idrol) VALUES
('Jhonatan', 'Quispe Aruquipa', 'jhonatan.quispe@example.com', 'AQAAAAIAAYagAAAAECME4cvmfMxDCR7I3Q3D3NZU6k8W46lfnmIJEJdj51Pp3bYCGNMYFWC3/BxboxAI/Q==', 'Activo', @rol_admin),
('Amilcar', 'Vicente', 'amilcar.vicente@example.com', 'AQAAAAIAAYagAAAAEPTTdDSrVSKlrjXJ9roMqTNoqhJkXG2KkqBEtOXuSF684lQtWALPvs7stm/WM6yrbw==', 'Activo', @rol_vendedor),
('Danner', 'Apana', 'danner.apana@example.com', 'AQAAAAIAAYagAAAAEHu205fKgRd3mrmvQAhwjVmUldKK+x3Ylr8wnorRUck1WgKNACT9txZcSLV3hBknPg==', 'Activo', @rol_vendedor),
('Alejandro', 'Condori', 'alejandro.condori@example.com', 'AQAAAAIAAYagAAAAELvs++Esqbfsw5Zh4uIaCJJilXBdQBIMAVuL7cge8yXdadEJxbacKSV64gDjDb6QxQ==', 'Activo', @rol_vendedor);
SET @usuario_admin = (SELECT idusuario FROM usuario WHERE correo = 'jhonatan.quispe@example.com' LIMIT 1);

INSERT IGNORE INTO categoria (nombre, descripcion) VALUES
('Smartphones', 'Teléfonos inteligentes'),
('Tablets', 'Tabletas electrónicas'),
('Accesorios', 'Accesorios para equipos tecnológicos'),
('Audífonos', 'Audífonos y audio personal'),
('Cargadores', 'Cargadores y fuentes de alimentación');
SET @cat_smartphones = (SELECT idcategoria FROM categoria WHERE nombre = 'Smartphones' LIMIT 1);

INSERT IGNORE INTO proveedor (nit, nombre, direccion, telefono, correo, activo) VALUES
('SEED-NIT-BO-0001', 'Distribuidora Tecnológica Andina', 'DATO DE PRUEBA, La Paz, Bolivia', '+591 70010001', 'andina@example.com', 1),
('SEED-NIT-BO-0002', 'Importadora Digital Bolivia', 'DATO DE PRUEBA, Cochabamba, Bolivia', '+591 70010002', 'digital@example.com', 1),
('SEED-NIT-BO-0003', 'Comercial Electrónica del Sur', 'DATO DE PRUEBA, Sucre, Bolivia', '+591 70010003', 'sur@example.com', 1),
('SEED-NIT-BO-0004', 'Mayorista Mobile Center', 'DATO DE PRUEBA, Santa Cruz, Bolivia', '+591 70010004', 'mobile@example.com', 1),
('SEED-NIT-BO-0005', 'TecnoDistribuciones La Paz', 'DATO DE PRUEBA, El Alto, Bolivia', '+591 70010005', 'tecnodistribuciones@example.com', 1);

INSERT IGNORE INTO cliente (documento, nombre, apellido, telefono, correo, activo) VALUES
('SEED-CI-0001', 'Carlos', 'Mendoza', '+591 70020001', 'carlos.mendoza@example.com', 1),
('SEED-CI-0002', 'María Fernanda', 'Rojas', '+591 70020002', 'maria.rojas@example.com', 1),
('SEED-CI-0003', 'Luis Fernando', 'Flores', '+591 70020003', 'luis.flores@example.com', 1),
('SEED-CI-0004', 'Andrea', 'Salazar', '+591 70020004', 'andrea.salazar@example.com', 1),
('SEED-CI-0005', 'Diego', 'Ramírez', '+591 70020005', 'diego.ramirez@example.com', 1);

-- Catálogo de cinco equipos de prueba; cada código se usa como clave idempotente.
INSERT IGNORE INTO producto (nombre, marca, modelo, codigo_barras, precio_venta, stock_minimo, categoria_idcategoria, activo) VALUES
('Samsung Galaxy A07 128 GB', 'Samsung', 'Galaxy A07 128 GB', 'SEED-TEC-PROD-0001', 2199.00, 2, @cat_smartphones, 1),
('Xiaomi Redmi 15C 128 GB', 'Xiaomi', 'Redmi 15C 128 GB', 'SEED-TEC-PROD-0002', 2299.00, 2, @cat_smartphones, 1),
('Motorola Moto G15 128 GB', 'Motorola', 'Moto G15 128 GB', 'SEED-TEC-PROD-0003', 2099.00, 2, @cat_smartphones, 1),
('Samsung Galaxy A17 128 GB', 'Samsung', 'Galaxy A17 128 GB', 'SEED-TEC-PROD-0004', 2799.00, 2, @cat_smartphones, 1),
('Honor Play 20A 128 GB', 'Honor', 'Play 20A 128 GB', 'SEED-TEC-PROD-0005', 2199.00, 2, @cat_smartphones, 1);
SET @p1 = (SELECT idproducto FROM producto WHERE codigo_barras = 'SEED-TEC-PROD-0001' LIMIT 1);
SET @p2 = (SELECT idproducto FROM producto WHERE codigo_barras = 'SEED-TEC-PROD-0002' LIMIT 1);
SET @p3 = (SELECT idproducto FROM producto WHERE codigo_barras = 'SEED-TEC-PROD-0003' LIMIT 1);
SET @p4 = (SELECT idproducto FROM producto WHERE codigo_barras = 'SEED-TEC-PROD-0004' LIMIT 1);
SET @p5 = (SELECT idproducto FROM producto WHERE codigo_barras = 'SEED-TEC-PROD-0005' LIMIT 1);

-- Una compra por proveedor, con costo inferior al precio de venta.
INSERT INTO compra (fecha_compra, numero_factura, total, proveedor_idproveedor, usuario_idusuario)
SELECT NOW(), 'SEED-COMPRA-0001', 5100.00, idproveedor, @usuario_admin FROM proveedor WHERE nit='SEED-NIT-BO-0001'
AND NOT EXISTS (SELECT 1 FROM compra WHERE numero_factura='SEED-COMPRA-0001');
INSERT INTO compra (fecha_compra, numero_factura, total, proveedor_idproveedor, usuario_idusuario)
SELECT NOW(), 'SEED-COMPRA-0002', 5400.00, idproveedor, @usuario_admin FROM proveedor WHERE nit='SEED-NIT-BO-0002'
AND NOT EXISTS (SELECT 1 FROM compra WHERE numero_factura='SEED-COMPRA-0002');
INSERT INTO compra (fecha_compra, numero_factura, total, proveedor_idproveedor, usuario_idusuario)
SELECT NOW(), 'SEED-COMPRA-0003', 4950.00, idproveedor, @usuario_admin FROM proveedor WHERE nit='SEED-NIT-BO-0003'
AND NOT EXISTS (SELECT 1 FROM compra WHERE numero_factura='SEED-COMPRA-0003');
INSERT INTO compra (fecha_compra, numero_factura, total, proveedor_idproveedor, usuario_idusuario)
SELECT NOW(), 'SEED-COMPRA-0004', 6600.00, idproveedor, @usuario_admin FROM proveedor WHERE nit='SEED-NIT-BO-0004'
AND NOT EXISTS (SELECT 1 FROM compra WHERE numero_factura='SEED-COMPRA-0004');
INSERT INTO compra (fecha_compra, numero_factura, total, proveedor_idproveedor, usuario_idusuario)
SELECT NOW(), 'SEED-COMPRA-0005', 5100.00, idproveedor, @usuario_admin FROM proveedor WHERE nit='SEED-NIT-BO-0005'
AND NOT EXISTS (SELECT 1 FROM compra WHERE numero_factura='SEED-COMPRA-0005');
SET @c1=(SELECT idcompra FROM compra WHERE numero_factura='SEED-COMPRA-0001' ORDER BY idcompra LIMIT 1);
SET @c2=(SELECT idcompra FROM compra WHERE numero_factura='SEED-COMPRA-0002' ORDER BY idcompra LIMIT 1);
SET @c3=(SELECT idcompra FROM compra WHERE numero_factura='SEED-COMPRA-0003' ORDER BY idcompra LIMIT 1);
SET @c4=(SELECT idcompra FROM compra WHERE numero_factura='SEED-COMPRA-0004' ORDER BY idcompra LIMIT 1);
SET @c5=(SELECT idcompra FROM compra WHERE numero_factura='SEED-COMPRA-0005' ORDER BY idcompra LIMIT 1);
INSERT INTO detalle_compra (cantidad,costo_unitario,subtotal,compra_idcompra,producto_idproducto)
SELECT 3,1700.00,5100.00,@c1,@p1 WHERE NOT EXISTS (SELECT 1 FROM detalle_compra WHERE compra_idcompra=@c1 AND producto_idproducto=@p1);
INSERT INTO detalle_compra (cantidad,costo_unitario,subtotal,compra_idcompra,producto_idproducto)
SELECT 3,1800.00,5400.00,@c2,@p2 WHERE NOT EXISTS (SELECT 1 FROM detalle_compra WHERE compra_idcompra=@c2 AND producto_idproducto=@p2);
INSERT INTO detalle_compra (cantidad,costo_unitario,subtotal,compra_idcompra,producto_idproducto)
SELECT 3,1650.00,4950.00,@c3,@p3 WHERE NOT EXISTS (SELECT 1 FROM detalle_compra WHERE compra_idcompra=@c3 AND producto_idproducto=@p3);
INSERT INTO detalle_compra (cantidad,costo_unitario,subtotal,compra_idcompra,producto_idproducto)
SELECT 3,2200.00,6600.00,@c4,@p4 WHERE NOT EXISTS (SELECT 1 FROM detalle_compra WHERE compra_idcompra=@c4 AND producto_idproducto=@p4);
INSERT INTO detalle_compra (cantidad,costo_unitario,subtotal,compra_idcompra,producto_idproducto)
SELECT 3,1700.00,5100.00,@c5,@p5 WHERE NOT EXISTS (SELECT 1 FROM detalle_compra WHERE compra_idcompra=@c5 AND producto_idproducto=@p5);

-- Identificadores ficticios de prueba, no son IMEI reales. Se crean tres unidades por producto.
INSERT IGNORE INTO inventario (imei, numero_serie, estado, producto_idproducto) VALUES
('SEED-IMEI-01-001','SEED-SERIE-01-001','Disponible',@p1),('SEED-IMEI-01-002','SEED-SERIE-01-002','Disponible',@p1),('SEED-IMEI-01-003','SEED-SERIE-01-003','Disponible',@p1),
('SEED-IMEI-02-001','SEED-SERIE-02-001','Disponible',@p2),('SEED-IMEI-02-002','SEED-SERIE-02-002','Disponible',@p2),('SEED-IMEI-02-003','SEED-SERIE-02-003','Disponible',@p2),
('SEED-IMEI-03-001','SEED-SERIE-03-001','Disponible',@p3),('SEED-IMEI-03-002','SEED-SERIE-03-002','Disponible',@p3),('SEED-IMEI-03-003','SEED-SERIE-03-003','Disponible',@p3),
('SEED-IMEI-04-001','SEED-SERIE-04-001','Disponible',@p4),('SEED-IMEI-04-002','SEED-SERIE-04-002','Disponible',@p4),('SEED-IMEI-04-003','SEED-SERIE-04-003','Disponible',@p4),
('SEED-IMEI-05-001','SEED-SERIE-05-001','Disponible',@p5),('SEED-IMEI-05-002','SEED-SERIE-05-002','Disponible',@p5),('SEED-IMEI-05-003','SEED-SERIE-05-003','Disponible',@p5);

COMMIT;

-- Resumen para validar la carga sin exponer los hashes de las cuentas.
SELECT 'roles', COUNT(*) FROM rol WHERE nombre IN ('Administrador','Vendedor');
SELECT 'usuarios_de_carga', COUNT(*) FROM usuario WHERE correo IN ('jhonatan.quispe@example.com','amilcar.vicente@example.com','danner.apana@example.com','alejandro.condori@example.com');
SELECT 'categorias_de_carga', COUNT(*) FROM categoria WHERE nombre IN ('Smartphones','Tablets','Accesorios','Audífonos','Cargadores');
SELECT 'proveedores_de_carga', COUNT(*) FROM proveedor WHERE nit LIKE 'SEED-NIT-BO-%';
SELECT 'clientes_de_carga', COUNT(*) FROM cliente WHERE documento LIKE 'SEED-CI-%';
SELECT 'productos_de_carga', COUNT(*) FROM producto WHERE codigo_barras LIKE 'SEED-TEC-PROD-%';
SELECT 'compras_de_carga', COUNT(*) FROM compra WHERE numero_factura LIKE 'SEED-COMPRA-%';
SELECT 'unidades_de_carga', COUNT(*) FROM inventario WHERE imei LIKE 'SEED-IMEI-%';
