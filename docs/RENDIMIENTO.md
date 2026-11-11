# Rendimiento y consultas

## Paginación

El historial de ventas se consulta en páginas de 100 registros. El conteo, filtro por período, búsqueda, orden, `Skip` y `Take` se ejecutan en SQLite. La interfaz permite navegar con **Anterior** y **Siguiente** y muestra página y total.

Una búsqueda nueva cancela la carga anterior antes de iniciar otra consulta. Los reportes mantienen métodos separados para exportar el período completo deliberadamente.

## Índices principales

- `IX_Sales_CreatedDateKey`: períodos de ventas y dashboard.
- `IX_Payments_PaidDateKey`: cobranzas por período.
- `IX_AuditLogs_OccurredAt`: auditoría.
- Índices de SKU, cliente y dimensiones: búsquedas e integridad.

La prueba `Sales_pagination_handles_thousands_of_rows_and_uses_date_index` carga 1.250 ventas, verifica páginas y comprueba mediante `EXPLAIN QUERY PLAN` que SQLite utiliza el índice de fecha.

## Materializaciones deliberadas

Los `AsEnumerable()` restantes tienen alcance previo acotado o requieren lógica no traducible por SQLite:

- Auditoría y movimientos: se ordenan y limitan en SQL antes de materializar.
- Cobranzas: primero se filtran pagos por índice y se cargan únicamente las ventas relacionadas; el saldo es una propiedad calculada del dominio.
- Deudas: SQLite filtra estados abiertos antes de calcular saldos y agrupaciones del dominio.
- Historial: primero se resuelve `CustomerId` y SQLite filtra sus ventas.
- Altas/importaciones: la comparación normalizada con eliminación de acentos ocurre sobre conjuntos pequeños de un producto o proveedor durante una mutación, no en listados masivos.

## Límites defensivos

- Auditoría: máximo 2.000 filas por consulta.
- Movimientos de stock: máximo 1.000 filas.
- Página de ventas: entre 20 y 500 filas.

Si el volumen supera estos supuestos, se debe repetir `EXPLAIN QUERY PLAN` y las pruebas de carga antes de aumentar los límites.
