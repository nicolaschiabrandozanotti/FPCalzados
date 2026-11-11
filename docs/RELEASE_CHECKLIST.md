# Checklist de publicación

## Preparación

- [ ] `CHANGELOG.md` actualizado.
- [ ] Versión de `FPCalzados.App.csproj` actualizada de forma coherente.
- [ ] Roadmap y documentación actualizados.
- [ ] Migraciones nuevas revisadas, incluyendo `Down` y preservación de datos.
- [ ] No existen credenciales, bases, backups ni datos personales en Git.

## Validación

- [ ] Ejecutar `scripts/verify.ps1`.
- [ ] Confirmar build sin advertencias y todos los tests aprobados.
- [ ] Confirmar ausencia de vulnerabilidades altas o críticas.
- [ ] Probar actualización sobre una copia de la versión anterior.
- [ ] Probar instalación limpia y cambio de contraseñas temporales.
- [ ] Probar backup y restauración.
- [ ] Probar venta, pago, devolución, stock y apertura/cierre de caja.

## Publicación

```powershell
.\.dotnet\dotnet.exe publish src\FPCalzados.App\FPCalzados.App.csproj `
  -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true `
  -o publish\FPCalzados-win-x64
```

- [ ] Verificar que el ejecutable muestre la versión esperada.
- [ ] Comprimir la carpeta completa de publicación.
- [ ] Generar y conservar el SHA-256 del archivo distribuido.
- [ ] Etiquetar el commit con `v<versión>`.
- [ ] Adjuntar changelog, artefacto y SHA-256 a la entrega.

## Pospublicación

- [ ] Conservar el artefacto exacto distribuido.
- [ ] Registrar fecha, versión y destinatarios.
- [ ] Mantener disponible el artefacto anterior para rollback.
