# Changelog

## Próxima versión

- Plan técnico versionado en `docs/roadmap` y verificación automatizada local.
- Actualización del proveedor SQLite para corregir una vulnerabilidad de gravedad alta.
- Backups consistentes mediante la API de SQLite, validación de integridad y restauración segura.
- Credenciales iniciales aleatorias, cambio obligatorio, política reforzada y bloqueo temporal ante intentos fallidos.
- Operaciones críticas y auditoría atómicas, con control de concurrencia para ventas, stock y caja.
- Auditoría transaccional extendida a productos, precios, proveedores, configuración y vencimientos.
- Esquema versionado mediante migraciones EF Core, backups previos, recuperación automática y restricciones de integridad en SQLite.
- Diagnóstico previo de duplicados que podrían impedir una migración segura.
- Clientes normalizados y reutilizados mediante identidad persistente para evitar historiales y deudas duplicados.
- Pantalla administrativa para revisar y fusionar clientes duplicados con reasignación de ventas.
- Normalización comparativa de productos, variantes, SKU y proveedores.
- Consultas de ventas y pagos filtradas mediante claves de fecha indexadas; auditoría y movimientos limitados en SQLite.
- Historial de ventas paginado con navegación, búsqueda SQL y cancelación de cargas reemplazadas.
- Prueba de carga y verificación del plan de consulta mediante `EXPLAIN QUERY PLAN`.
- CI para build, tests, vulnerabilidades y publicación portable con checksum SHA-256.
- Logs de fallos estructurados, con redacción de secretos y rotación automática.
- Logging operacional para backups, restauraciones y migraciones.
- Workflow de release por tag con validación de versión, ZIP y checksum.
- Documentación operativa y checklist de publicación.
- Validaciones externas de GitHub Actions y Windows limpio documentadas para una futura distribución.

## 1.1.0

- Caja diaria con apertura/cierre y diferencia.
- Tickets imprimibles y exportables TXT.
- Historial completo por cliente.
- Busqueda/agregado rapido en ventas.
- Control avanzado de stock con movimientos manuales y bajo stock.
- Restauracion de backups desde la app.
- Importacion Excel con validaciones y vista previa.
- Datos del negocio configurables para tickets.
- Reportes PDF de ventas e inventario.
- Mejoras visuales generales.

## 1.0.0

- Version inicial portable con SQLite local.
