# Bloque 4 — Migraciones y restricciones

**Estado:** COMPLETADO (100 %)

## Tareas

- [x] Sustituir `EnsureCreated` y cambios manuales por migraciones EF Core.
- [x] Diseñar actualización segura de bases existentes.
- [x] Versionar el esquema.
- [x] Agregar restricciones de cantidades, stock, precios, pagos y descuentos.
- [x] Agregar índices y unicidad para variantes y precios especiales.
- [x] Crear backup previo a una migración.
- [x] Probar actualización desde la migración inicial.
- [x] Probar actualización representativa de base legacy sin historial y desde la migración inicial.
- [x] Probar fallo de migración y recuperación completa.
- [x] Agregar diagnóstico previo de duplicados que impedirían índices únicos.

## Criterios de aceptación

- Una base existente se actualiza sin perder datos.
- Una migración fallida conserva una copia recuperable.
- SQLite rechaza datos inválidos aunque se omita una validación de aplicación.

## Registro

- 2026-07-12: se incorporó `dotnet-ef` local y migraciones versionadas `InitialSchema` y `AddIntegrityConstraints`.
- 2026-07-12: las bases previas se reconocen, respaldan y actualizan conservando sus datos.
- 2026-07-12: checks e índices únicos protegen stock, cantidades, importes, variantes y precios.
- 2026-07-12: verificación inicial exitosa, 46/46 tests y sin vulnerabilidades conocidas.
- 2026-07-12: recuperación automática desde backup ante fallo de migración y prueba con fallo inyectado.
- 2026-07-12: diagnóstico previo de variantes, SKU y precios duplicados; pruebas de base legacy sin historial.
- 2026-07-12: cierre definitivo con verificación completa 60/60 y sin vulnerabilidades conocidas.
