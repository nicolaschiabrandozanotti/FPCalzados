# FPCalzados

Aplicacion de escritorio portable para Windows, con base local SQLite, orientada a una zapateria.

Version actual: `1.1.0`.

## Ejecutar la version portable

La carpeta publicada queda en:

```powershell
publish\FPCalzados-win-x64
```

Abrir:

```powershell
publish\FPCalzados-win-x64\FPCalzados.App.exe
```

La app crea junto al ejecutable:

- `data\FPCalzados.sqlite`
- `backups\`
- `exports\`

En el primer inicio se genera `CREDENCIALES-INICIALES.txt` junto al ejecutable con contraseñas temporales aleatorias para Administrador y Vendedor. Cada rol debe cambiar su contraseña al ingresar por primera vez; después se recomienda eliminar el archivo.

## Funciones incluidas

- Productos por marca, modelo, color, talle y codigo opcional.
- Costo proveedor, precio efectivo y precios especiales por color, talle o color+talle.
- Reglas de pago para efectivo, debito, credito y cuotas.
- Venta con carrito, descuentos, pagos mixtos y saldo pendiente por cliente.
- Ticket de venta imprimible y exportable en TXT, ademas de comprobante XLSX.
- Devolucion por item con ajuste automatico de stock y dinero/deuda.
- Dashboard con ventas, cobros, pendientes, devoluciones, bajo stock, vencimientos y ranking.
- Caja diaria con apertura, cierre, efectivo esperado, efectivo real y diferencia.
- Importacion XLSX de stock, plantilla XLSX y reportes XLSX de ventas/inventario.
- Backup automatico diario y backup manual.
- Login local por rol Admin/Vendedor.
- Configuracion de datos del negocio para mostrar en tickets.

## Documentación operativa

- [Instalación, actualización, backup y recuperación](docs/OPERACION.md)
- [Checklist de publicación](docs/RELEASE_CHECKLIST.md)
- [Seguridad y matriz de permisos](docs/SEGURIDAD.md)
- [Rendimiento, paginación e índices](docs/RENDIMIENTO.md)
- [Validaciones externas diferidas](docs/VALIDACIONES_DIFERIDAS.md)
- [Plan y estado de mejoras](docs/roadmap/README.md)

## Compilar

El repo espera .NET SDK `10.0.300`. En este workspace se instalo localmente en `.dotnet\`.

```powershell
.\.dotnet\dotnet.exe restore FPCalzados.slnx
.\.dotnet\dotnet.exe test FPCalzados.slnx
.\.dotnet\dotnet.exe publish src\FPCalzados.App\FPCalzados.App.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\FPCalzados-win-x64
```
