# Seguridad y permisos

## Contraseñas

- Mínimo 10 caracteres.
- Deben incluir al menos una letra y un número.
- Se almacenan con PBKDF2-SHA256, salt aleatorio y 210.000 iteraciones.
- Los hashes antiguos se actualizan después de un login válido.
- Cinco intentos fallidos bloquean el rol durante cinco minutos.

## Matriz de permisos

| Operación | Administrador | Vendedor |
|---|:---:|:---:|
| Consultar inventario y precios | Sí | Sí |
| Crear ventas, cobrar y devolver | Sí | Sí |
| Abrir/cerrar caja | Sí | Sí |
| Modificar productos, stock y precios | Sí | No |
| Importar stock | Sí | No |
| Administrar proveedores | Sí | No |
| Cambiar reglas y datos del negocio | Sí | No |
| Consultar/exportar auditoría | Sí | No |
| Cambiar la contraseña del propio rol | Sí | Sí |

La autorización se valida en la capa de aplicación cuando existe una sesión autenticada; la interfaz no es la única barrera.

## Archivos sensibles

`CREDENCIALES-INICIALES.txt`, bases, backups y logs no deben enviarse ni incorporarse al repositorio. El archivo de credenciales se elimina automáticamente cuando ambos roles cambian sus claves temporales.
