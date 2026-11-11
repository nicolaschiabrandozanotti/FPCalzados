# Bloque 0 — Línea base y seguridad del repositorio

**Estado:** COMPLETADO

## Tareas

- [x] Confirmar restore y build Release.
- [x] Diagnosticar la duración/bloqueo de los tests.
- [x] Conseguir que la suite completa finalice correctamente.
- [x] Actualizar la dependencia SQLite vulnerable.
- [x] Verificar paquetes vulnerables.
- [x] Incorporar comandos repetibles de verificación.
- [x] Confirmar exclusión de artefactos y datos locales.

## Criterios de aceptación

- Build Release exitoso.
- Suite completa exitosa en un tiempo razonable.
- Sin vulnerabilidades conocidas de gravedad alta o crítica.
- Verificación reproducible mediante un único script/comando documentado.

## Registro

- 2026-07-12: bloque iniciado. La primera ejecución excedió 120 segundos y detectó `GHSA-2m69-gcr7-jv3q` en `SQLitePCLRaw.lib.e_sqlite3 2.1.11`.
- 2026-07-12: se actualizó el proveedor SQLite a `SQLitePCLRaw.bundle_e_sqlite3 3.0.3`.
- 2026-07-12: `scripts/verify.ps1` completó build sin advertencias, 34/34 tests y análisis sin paquetes vulnerables. La demora inicial no era un bloqueo: la suite tarda aproximadamente 80 segundos y ahora posee detección de cuelgues.
