# Bloque 3 — Integridad de ventas, pagos y stock

**Estado:** COMPLETADO (100 %)

## Tareas

- [x] Unificar cada operación comercial y su auditoría en una transacción.
- [x] Cubrir ventas, pagos, devoluciones, stock y caja.
- [x] Incorporar control de concurrencia para stock y saldos.
- [x] Impedir sobreventa, doble devolución, sobrepago y doble cierre.
- [x] Reforzar validaciones de dominio.
- [x] Completar atomicidad de auditoría en operaciones de ventas, productos, stock, proveedores y configuración.
- [x] Agregar pruebas de contención real sobre saldos y protección de caja.
- [x] Agregar prueba de rollback provocando una violación de restricción.
- [x] Agregar prueba base del token de concurrencia.

## Criterios de aceptación

- Cada operación se aplica completamente o se revierte completamente.
- Las actualizaciones simultáneas no rompen stock, saldos ni caja.
- La auditoría no puede quedar separada de la operación auditada.

## Registro

- 2026-07-12: auditoría integrada en la misma unidad de persistencia para ventas, pagos, devoluciones, ajustes/importaciones y caja.
- 2026-07-12: tokens de concurrencia agregados a ventas, variantes y sesiones de caja; índice único impide dos cajas abiertas.
- 2026-07-12: validación reforzada para motivos de devolución y mensajes claros ante conflictos.
- 2026-07-12: verificación inicial exitosa, 44/44 tests y sin vulnerabilidades conocidas.
- 2026-07-12: auditoría movida a la unidad de persistencia de productos, precios, proveedores, configuración y vencimientos.
- 2026-07-12: pruebas de pago concurrente, rollback conjunto de entidad/auditoría y doble cierre; verificación 57/57.
