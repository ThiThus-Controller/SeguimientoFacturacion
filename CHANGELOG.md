# Changelog

Todos los cambios relevantes del proyecto se documentan en este archivo.
El formato sigue Keep a Changelog y el versionado seguirá SemVer cuando
se produzca la primera versión desplegable.

## [Unreleased]

### Pendiente

- Cierre técnico y fusión de la Fase 1.
- Gestión manual con AG Grid.
- Auditoría inmutable de operaciones manuales.
- Dashboard administrativo.

## [0.1.0] - 2026-08-11

### Added

- Solución en capas Domain, Application, Infrastructure y Web.
- Proyectos de pruebas unitarias por capa.
- Persistencia EF Core sobre SQL Server con esquemas separados.
- Entidades de facturas, pacientes, notas, glosas, pagos y aplicaciones.
- Catálogos normalizados de aseguradoras, facturadores, estados,
  atenciones, costos y tipos de documento.
- Importación masiva modular mediante cuatro plantillas XLSX.
- Flujo de análisis, staging, confirmación y procesamiento definitivo.
- Inconsistencias por fila y valor presentado.
- Reintentos controlados de archivos corregidos.
- Recuperación de lotes válidos ya analizados.
- Descarga de plantillas oficiales desde la interfaz.
- Distribución financiera entre valor aplicado y anticipo.
- Soporte para facturas anuladas, saldadas y pagos con excedente.
- Autenticación por cookie.
- Usuarios almacenados fuera de SQL Server en `usuarios.dat`.
- Cifrado AES-256-GCM y contraseñas PBKDF2-HMAC-SHA256.
- Roles predeterminados, permisos granulares, concesiones y revocaciones.
- Administración web de usuarios, aseguradoras y facturadores.
- Usuario autenticado en los campos de trazabilidad.
- Scripts SQL de verificación y certificación.

### Changed

- Se reemplazó el archivo monolítico de seguimiento por plantillas
  independientes para facturas, notas, glosas y pagos.
- El número de nota se conserva como texto para admitir identificadores
  alfanuméricos y ceros iniciales.
- Los movimientos anuales admiten fecha exacta opcional.
- Las glosas incorporan estado y valor aceptado.
- Las retenciones se conservan como datos informativos del pago.
- Los excedentes y pagos sobre facturas saldadas se registran como
  anticipos en lugar de rechazarse.

### Fixed

- Bloqueo indebido de reintentos para archivos analizados con errores.
- Registro de archivos corregidos con el mismo nombre.
- Mapeo y visualización del valor presentado en inconsistencias.
- Procesamiento de notas, glosas y pagos hasta las tablas definitivas.
- Notas y glosas sobre facturas anuladas.
- Cálculo de aplicaciones y anticipos.
- Selección automática y no editable del siguiente código de facturador.
- Activación e inactivación de usuarios y catálogos.

### Security

- La clave AES se obtiene de User Secrets o del almacén del entorno.
- Las cuentas activas se revalidan durante la sesión.
- Las acciones sensibles requieren políticas de autorización.
- Las credenciales y archivos con pacientes están excluidos del
  repositorio.

### Verified

- Compilación completa de la solución.
- Suite automatizada sin errores en la última ejecución registrada.
- Certificación financiera de facturas activas, anuladas, saldadas y con
  pagos excedentes.
- Certificación integral de 25 controles sin hallazgos.
