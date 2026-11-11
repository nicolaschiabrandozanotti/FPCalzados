# Bloque 5 — Clientes y calidad de datos

**Estado:** COMPLETADO (100 %)

## Tareas

- [x] Identificar historiales y deudas mediante `CustomerId`.
- [x] Reutilizar clientes persistidos en nuevas ventas.
- [x] Normalizar nombres, teléfonos, documentos y datos de productos.
- [x] Detectar y permitir fusionar duplicados.
- [x] Agregar índices/restricciones aplicables.
- [x] Crear una pantalla para revisar y fusionar duplicados manualmente.
- [x] Completar normalización comparativa de productos, marcas, colores, talles, SKU y proveedores.
- [x] Probar teléfonos compartidos y documentos diferentes, además de reutilización por documento.
- [x] Probar reutilización básica con nombre, teléfono y documento normalizados.

## Criterios de aceptación

- Las ventas y deudas se asignan inequívocamente al cliente correcto.
- La aplicación previene o advierte duplicados lógicos.
- La normalización es consistente en altas y búsquedas.

## Registro

- 2026-07-12: identidad persistente por `CustomerId` para deudas e historiales.
- 2026-07-12: normalización de nombre, teléfono y documento; reutilización por documento o teléfono inequívoco.
- 2026-07-12: la migración fusiona duplicados históricos y reasigna sus ventas antes de crear índices.
- 2026-07-12: verificación inicial exitosa, 47/47 tests y sin vulnerabilidades conocidas.
- 2026-07-12: pantalla administrativa para revisar y fusionar duplicados, con reasignación transaccional de ventas y auditoría.
- 2026-07-12: clientes con documentos distintos ya no se fusionan por compartir teléfono; SKU y dimensiones se normalizan.
- 2026-07-12: cierre definitivo con verificación 62/62 y sin vulnerabilidades conocidas.
