# Bloque 2 — Acceso y contraseñas

**Estado:** COMPLETADO (100 %)

## Tareas

- [x] Eliminar credenciales iniciales públicas y predecibles.
- [x] Implementar configuración o generación segura inicial.
- [x] Exigir cambio de contraseña en el primer acceso.
- [x] Reforzar longitud y política de contraseña.
- [x] Manejar hashes inválidos sin cerrar la aplicación.
- [x] Permitir actualización transparente de hashes antiguos.
- [x] Aplicar retraso progresivo o bloqueo temporal.
- [x] Eliminar credenciales del README y agregar tests.
- [x] Probar actualización desde un hash PBKDF2 antiguo real.
- [x] Auditar y probar autorización por rol para cada operación administrativa.
- [x] Definir una política base: mínimo 10 caracteres con letras y números.

## Criterios de aceptación

- Una instalación nueva no es accesible con credenciales conocidas.
- Los intentos reiterados se limitan.
- Contraseñas y hashes malformados se manejan de forma segura.

## Registro

- 2026-07-12: contraseñas temporales aleatorias, cambio obligatorio, PBKDF2 reforzado y actualización de hashes implementados.
- 2026-07-12: bloqueo persistente de cinco minutos luego de cinco intentos fallidos.
- 2026-07-12: verificación inicial exitosa, 43/43 tests y sin vulnerabilidades conocidas.
- 2026-07-12: cierre definitivo con rehash PBKDF2 real, matriz de permisos y pruebas de Vendedor; verificación 54/54.
