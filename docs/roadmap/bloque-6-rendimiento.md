# Bloque 6 — Rendimiento y experiencia

**Estado:** COMPLETADO (100 %)

## Tareas

- [x] Evitar materialización previa a filtros y ordenamientos.
- [x] Ejecutar filtros y agrupaciones en SQLite.
- [x] Paginar o acotar ventas, movimientos, auditoría e historiales.
- [x] Revisar índices y planes de consulta.
- [x] Añadir carga asíncrona para operaciones largas.
- [x] Mejorar mensajes de error.
- [x] Implementar paginación real con navegación en el historial de ventas y límites defensivos en auditoría/movimientos.
- [x] Incorporar cancelación por reemplazo de cargas de ventas.
- [x] Crear prueba de carga con más de mil ventas.
- [x] Registrar y revisar el plan de consulta del historial paginado.
- [x] Eliminar o documentar y justificar los `AsEnumerable()` restantes.
- [x] Probar consultas con filtros representativos.

## Criterios de aceptación

- Los listados no cargan conjuntos completos innecesariamente.
- Los tiempos y el consumo de memoria permanecen razonables con datos de volumen.
- Las tareas largas no bloquean la interfaz sin feedback.

## Registro

- 2026-07-12: claves de fecha indexadas para ventas y pagos eliminan filtrados completos en memoria.
- 2026-07-12: movimientos y auditoría se ordenan y limitan en SQLite antes de materializar.
- 2026-07-12: historiales filtran por `CustomerId` y los listados conservan límites defensivos.
- 2026-07-12: verificación inicial exitosa, 47/47 tests y sin vulnerabilidades conocidas.
- 2026-07-12: historial paginado de 100 filas con navegación, conteo y búsqueda ejecutados en SQLite.
- 2026-07-12: cancelación de cargas reemplazadas y guía `docs/RENDIMIENTO.md` con justificación de materializaciones.
- 2026-07-12: prueba con 1.250 ventas y `EXPLAIN QUERY PLAN` confirma el índice de fecha; verificación 63/63.
