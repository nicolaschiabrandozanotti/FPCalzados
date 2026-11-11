# Bloque 1 — Backups y restauración segura

**Estado:** COMPLETADO (100 %)

## Tareas

- [x] Usar la API nativa de backup de SQLite en lugar de copiar una base activa.
- [x] Validar backups con `PRAGMA integrity_check`.
- [x] Restaurar primero a un archivo temporal validado.
- [x] Validar esquema y compatibilidad.
- [x] Reemplazar la base de forma segura y recuperar la copia previa ante fallos.
- [x] Cubrir backup, corrupción, formato inválido, restauración fallida y conservación de datos con tests.
- [x] Probar backup consistente con datos confirmados en WAL mientras la conexión permanece abierta.
- [x] Inyectar un fallo durante el reemplazo y verificar recuperación automática real.

## Criterios de aceptación

- Ningún backup se publica sin validar su integridad.
- Una restauración inválida no modifica la base activa.
- Un fallo durante la restauración permite recuperar automáticamente el estado anterior.

## Registro

- 2026-07-12: backup nativo, validación de integridad/esquema, reemplazo atómico y recuperación implementados.
- 2026-07-12: verificación inicial exitosa, 38/38 tests y sin vulnerabilidades conocidas.
- 2026-07-12: cierre definitivo con prueba WAL y recuperación posreemplazo; verificación completa 50/50.
