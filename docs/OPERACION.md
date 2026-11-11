# Operación de FPCalzados

## Instalación

1. Descargar y descomprimir `FPCalzados-win-x64` en una carpeta con permisos de escritura.
2. Ejecutar `FPCalzados.App.exe`.
3. Leer `CREDENCIALES-INICIALES.txt` y cambiar las claves temporales de ambos roles.
4. Eliminar el archivo de credenciales una vez actualizadas las dos claves.

No mover únicamente el ejecutable: `data`, `backups`, `exports` y `logs` pertenecen a esa instalación.

## Actualización

1. Cerrar FPCalzados y confirmar que no quede su proceso activo.
2. Crear un backup manual desde la aplicación.
3. Conservar por separado toda la carpeta instalada.
4. Reemplazar los binarios por la nueva publicación, sin eliminar las carpetas de datos.
5. Iniciar la aplicación. Las migraciones crean otro backup antes de modificar el esquema.
6. Verificar login, inventario, últimas ventas y caja.

## Backup

Los backups manuales y diarios se guardan en `backups/`. La aplicación usa una copia consistente de SQLite y valida su integridad antes de publicarla.

Se recomienda copiar periódicamente `backups/` a otro disco o almacenamiento seguro. Un backup en el mismo equipo no protege contra pérdida o rotura del disco.

## Restauración

1. Cerrar operaciones de venta y caja en otros procesos.
2. Elegir un `.sqlite` desde la función de restauración.
3. La aplicación valida integridad y esquema antes de reemplazar la base.
4. Se crea un backup `FPCalzados-pre-restore-*` del estado anterior.
5. Reiniciar y verificar ventas, stock y caja.

## Recuperación ante fallos

- Revisar `logs/crash-*.json`; se conservan los 30 más recientes.
- No publicar logs sin revisar: aunque los secretos comunes se ocultan, un mensaje externo podría contener información del negocio.
- Si la app no inicia, conservar la carpeta completa antes de modificar archivos.
- Restaurar el último backup válido. Si falla, utilizar la copia `pre-restore` o la copia previa a la actualización.
- Nunca editar directamente `FPCalzados.sqlite` sin una copia verificada.

## Verificación técnica

Desde la raíz del repositorio:

```powershell
powershell -ExecutionPolicy Bypass -File scripts\verify.ps1
```

El comando restaura paquetes, compila Release, ejecuta tests con detección de bloqueos y falla si encuentra dependencias vulnerables.
