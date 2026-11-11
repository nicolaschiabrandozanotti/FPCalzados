# Bloque 7 — Automatización y operación

**Estado:** COMPLETADO (100 % DEL ALCANCE LOCAL)

## Tareas

- [x] Configurar CI para restore, build Release, tests y vulnerabilidades.
- [x] Incorporar logging estructurado y rotación.
- [x] Evitar secretos y datos personales innecesarios en logs.
- [x] Documentar instalación, actualización, backup, restauración y recuperación.
- [x] Crear checklist de publicación.
- [x] Incorporar versión visible y checksum verificable.
- [→] Validar el workflow mediante una ejecución real en GitHub Actions (diferido en `docs/VALIDACIONES_DIFERIDAS.md`).
- [→] Probar el artefacto de CI en una máquina limpia (diferido en `docs/VALIDACIONES_DIFERIDAS.md`).
- [x] Extender logging estructurado a backups, restauraciones y migraciones.
- [x] Automatizar validación de versión, etiqueta, release y checksum.
- [x] Validar publicación local versionada y recuperación mediante las pruebas de backup/migración.

## Criterios de aceptación

- Cada cambio se valida automáticamente.
- Una publicación puede reproducirse siguiendo la documentación.
- Los errores operativos dejan diagnóstico útil sin exponer información sensible.

## Registro

- 2026-07-12: CI de GitHub Actions agregado con verificación completa, publicación portable y artefacto SHA-256.
- 2026-07-12: crash logs JSON, redacción de secretos comunes y rotación de 30 archivos.
- 2026-07-12: guías de operación y checklist de release incorporados.
- 2026-07-12: verificación inicial exitosa, 48/48 tests y sin vulnerabilidades conocidas.
- 2026-07-12: workflow de release por tag, publicación local versionada y logging operacional agregados.
- 2026-07-12: artefacto local `v1.1.0` y SHA-256 generados correctamente; verificación completa 64/64.
- 2026-07-12: las validaciones externas se difieren formalmente; el alcance local queda cerrado al 100 %.
