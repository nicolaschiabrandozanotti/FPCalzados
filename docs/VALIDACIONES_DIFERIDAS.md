# Validaciones diferidas

Estas validaciones fueron excluidas del cierre actual por decisión del responsable del proyecto. No representan tareas de código pendientes, pero deben ejecutarse antes de considerar una distribución externa plenamente certificada.

## 1. Ejecución real de GitHub Actions

**Estado:** DIFERIDA

El repositorio local no tiene un `git remote` configurado. Cuando se publique en GitHub:

1. Configurar el remote.
2. Subir la rama principal.
3. Confirmar que `.github/workflows/ci.yml` finalice correctamente.
4. Crear una etiqueta que coincida con la versión del proyecto, por ejemplo `v1.1.0`.
5. Confirmar que `.github/workflows/release.yml` cree:
   - Release de GitHub.
   - ZIP portable para Windows x64.
   - `SHA256SUMS.txt`.
6. Descargar el artefacto generado por GitHub y verificar su checksum.

## 2. Prueba en Windows limpio

**Estado:** DIFERIDA

Ejecutar el artefacto de CI en una computadora o máquina virtual Windows que no tenga .NET SDK ni Visual Studio:

1. Verificar que la aplicación inicie sin instalar dependencias.
2. Confirmar la creación de `data`, `backups`, `exports` y `logs`.
3. Completar el cambio de las contraseñas temporales.
4. Probar una venta, pago, devolución y cierre de caja.
5. Probar backup y restauración.
6. Cerrar y volver a abrir la aplicación para confirmar persistencia.
7. Registrar versión de Windows, versión de FPCalzados, fecha y resultado.

## Evidencia disponible al cierre

- Publicación local portable generada correctamente.
- ZIP local y checksum SHA-256 generados.
- Build Release sin advertencias.
- 64/64 tests aprobados.
- Sin vulnerabilidades conocidas detectadas.

**Decisión de cierre:** las validaciones externas quedan documentadas para una futura publicación y el plan técnico se considera cerrado en el alcance local actual.
