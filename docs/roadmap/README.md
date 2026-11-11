# Plan de mejoras

Este directorio registra el avance de las mejoras técnicas de FPCalzados.

## Estados

- `PENDIENTE`: todavía no iniciado.
- `EN CURSO`: implementación activa.
- `BLOQUEADO`: requiere una decisión o dependencia externa.
- `PARCIAL`: tiene mejoras entregadas, pero aún quedan criterios pendientes.
- `COMPLETADO`: todos los criterios de aceptación fueron verificados.

## Orden de ejecución

| Orden | Bloque | Tema | Estado |
|---:|---|---|---|
| 0 | [Bloque 0](bloque-0-linea-base.md) | Línea base y seguridad del repositorio | COMPLETADO (100 %) |
| 1 | [Bloque 1](bloque-1-backups.md) | Backups y restauración segura | COMPLETADO (100 %) |
| 2 | [Bloque 2](bloque-2-autenticacion.md) | Acceso y contraseñas | COMPLETADO (100 %) |
| 3 | [Bloque 3](bloque-3-integridad.md) | Integridad de ventas, pagos y stock | COMPLETADO (100 %) |
| 4 | [Bloque 4](bloque-4-migraciones.md) | Migraciones y restricciones | COMPLETADO (100 %) |
| 5 | [Bloque 5](bloque-5-clientes.md) | Clientes y calidad de datos | COMPLETADO (100 %) |
| 6 | [Bloque 6](bloque-6-rendimiento.md) | Rendimiento y experiencia | COMPLETADO (100 %) |
| 7 | [Bloque 7](bloque-7-operacion.md) | Automatización y operación | COMPLETADO (100 % del alcance local) |

## Regla de cierre

Un bloque solo pasa a `COMPLETADO` cuando:

1. Sus tareas y criterios de aceptación están cubiertos.
2. La solución compila en Release.
3. Los tests relevantes finalizan correctamente.
4. Se actualizan este índice, el archivo del bloque y `CHANGELOG.md`.

## Registro

- 2026-07-12: se crea el plan y comienza el Bloque 0.
- 2026-07-12: Bloque 0 completado; comienza el Bloque 1.
- 2026-07-12: Bloque 1 completado con 38/38 tests; comienza el Bloque 2.
- 2026-07-12: Bloque 2 completado con 43/43 tests; comienza el Bloque 3.
- 2026-07-12: Bloque 3 completado con 44/44 tests; comienza el Bloque 4.
- 2026-07-12: Bloque 4 completado con 46/46 tests; comienza el Bloque 5.
- 2026-07-12: Bloque 5 completado con 47/47 tests; comienza el Bloque 6.
- 2026-07-12: Bloque 6 completado con 47/47 tests; comienza el Bloque 7.
- 2026-07-12: se alcanzan 48/48 tests y se entrega una primera pasada de los bloques 0–7.
- 2026-07-12: auditoría posterior corrige el estado: solo el Bloque 0 estaba cerrado al 100 %. Se reabre el Bloque 1 y se registran pendientes verificables para los bloques 1–7.
- 2026-07-12: Bloque 1 cerrado al 100 % con pruebas WAL y recuperación tras fallo inyectado; comienza el cierre del Bloque 2.
- 2026-07-12: Bloque 2 cerrado al 100 % con política reforzada, rehash real y autorización por rol; comienza el Bloque 3.
- 2026-07-12: Bloque 3 cerrado al 100 % con auditoría transaccional completa y pruebas de concurrencia/rollback; comienza el Bloque 4.
- 2026-07-12: Bloque 4 cerrado al 100 % con recuperación de migraciones, diagnóstico de duplicados y pruebas legacy; comienza el Bloque 5.
- 2026-07-12: Bloque 5 cerrado al 100 % con revisión/fusión manual y normalización comercial; comienza el Bloque 6.
- 2026-07-12: Bloque 6 cerrado al 100 % con paginación UI, cancelación y prueba de carga/plan; comienza el Bloque 7.
- 2026-07-12: Bloque 7 llega al 95 %. La publicación local está validada; faltan un repositorio remoto para ejecutar Actions y una prueba en otra máquina.
- 2026-07-12: por decisión del responsable, las dos validaciones externas se trasladan a `docs/VALIDACIONES_DIFERIDAS.md`. Bloque 7 y plan local cerrados.
