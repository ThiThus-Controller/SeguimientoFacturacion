# SeguimientoFacturacion

Sistema web para el seguimiento de facturación, cartera y glosas del
sector salud. Reemplaza el control histórico realizado en Excel por una
aplicación con reglas centralizadas, importación masiva segura,
trazabilidad y autorización granular.

## Estado del proyecto

La **Fase 1 — Importación masiva modular** está funcionalmente terminada
y certificada mediante 25 controles integrales sin hallazgos.

| Módulo | Estado |
|---|---|
| Facturas y pacientes | Certificado |
| Notas crédito y débito | Certificado |
| Glosas | Certificado |
| Pagos y aplicaciones | Certificado |
| Usuarios y permisos | Operativo |
| Aseguradoras y facturadores | Operativo |
| Gestión manual tipo Excel | Fase 2 |
| Dashboard administrativo | Fase 3 |

## Tecnologías

- ASP.NET Core MVC sobre `.NET 10`.
- C# con tipos anulables habilitados.
- SQL Server y Entity Framework Core.
- ClosedXML para lectura de archivos XLSX.
- FluentValidation y AutoMapper.
- Bootstrap 5.
- Autenticación por cookie y autorización mediante políticas.
- PBKDF2-HMAC-SHA256 y AES-256-GCM para las cuentas locales.

AG Grid, Chart.js, ClosedXML para exportaciones, Select2 y SweetAlert2
forman parte del alcance de las siguientes fases cuando corresponda.

## Arquitectura

La solución utiliza cuatro capas principales y proyectos de pruebas
independientes:

```text
SeguimientoFacturacion.Web
        |
        v
SeguimientoFacturacion.Application
        |
        v
SeguimientoFacturacion.Domain

SeguimientoFacturacion.Infrastructure
        |
        v
SQL Server / archivos cifrados
```

- **Domain**: entidades, reglas, enumeraciones y value objects.
- **Application**: casos de uso, DTO, contratos y validadores.
- **Infrastructure**: EF Core, SQL Server, ClosedXML y seguridad.
- **Web**: MVC, autenticación, autorización, controladores y vistas.

Web nunca accede directamente a SQL Server. Consulte
[ARQUITECTURA.md](ARQUITECTURA.md) para conocer las dependencias y
decisiones técnicas.

## Importación masiva

Cada archivo atraviesa tres etapas:

1. **Analizar**: valida estructura, contenido, catálogos y reglas.
2. **Confirmar**: autoriza expresamente el lote válido.
3. **Procesar**: revalida y guarda en una transacción definitiva.

Los tipos admitidos son:

- Facturas y pacientes.
- Notas crédito y débito.
- Glosas y sus respuestas.
- Pagos y aplicaciones automáticas.

Las plantillas oficiales se descargan desde la interfaz y se encuentran
en `SeguimientoFacturacion/wwwroot/plantillas/importacion`.

Consulte [REGLAS_IMPORTACION.md](REGLAS_IMPORTACION.md) y
[REGLAS_PAGOS.md](REGLAS_PAGOS.md).

## Seguridad

Los usuarios **no se almacenan en SQL Server**. El repositorio local
`usuarios.dat` contiene un JSON cifrado mediante AES-256-GCM. Las
contraseñas se derivan con PBKDF2-HMAC-SHA256 y al menos 600.000
iteraciones.

La clave AES nunca debe incluirse en Git, `appsettings.json`, scripts o
capturas. Debe suministrarse mediante User Secrets en desarrollo y un
almacén de secretos en producción.

Los roles predeterminados son:

- Administrador.
- Supervisor.
- OperadorFacturas.
- OperadorNotas.
- OperadorGlosas.
- OperadorCartera.
- Consulta.
- Personalizado.

Los permisos heredados se pueden conceder o revocar individualmente.

## Requisitos locales

- SDK de .NET 10.
- SQL Server accesible desde el equipo.
- Visual Studio 2022 actualizado o una terminal compatible.
- PowerShell 5.1 o superior.

## Configuración inicial

Desde la raíz del repositorio:

```powershell
cd "C:\Users\USUARIO\source\repos\SeguimientoFacturacion"

dotnet user-secrets set `
    "ConnectionStrings:SeguimientoDatabase" `
    "Server=<SERVIDOR>;Database=Seguimiento;User Id=<USUARIO>;Password=<CLAVE>;Encrypt=True;TrustServerCertificate=True;MultipleActiveResultSets=True" `
    --project ".\SeguimientoFacturacion\SeguimientoFacturacion.Web.csproj"
```

Configure también `Seguridad:Usuarios:ClaveCifradoBase64` mediante User
Secrets. No reutilice la contraseña de SQL Server como clave de cifrado.

## Compilación y pruebas

```powershell
dotnet restore ".\SeguimientoFacturacion.slnx"

dotnet build ".\SeguimientoFacturacion.slnx"

dotnet test ".\SeguimientoFacturacion.slnx" --no-restore

dotnet list ".\SeguimientoFacturacion.slnx" package `
    --vulnerable `
    --include-transitive
```

## Migraciones

El proyecto de inicio es Web y el contexto está en Infrastructure:

```powershell
dotnet tool run dotnet-ef database update `
    --project ".\SeguimientoFacturacion.Infrastructure\SeguimientoFacturacion.Infrastructure.csproj" `
    --startup-project ".\SeguimientoFacturacion\SeguimientoFacturacion.Web.csproj" `
    --context SeguimientoDbContext `
    --connection "<CONEXION_EXPLICITA>"
```

La conexión explícita evita actualizar accidentalmente la base local de
diseño. Haga copia de seguridad y revise cada migración antes de
aplicarla.

## Ejecución

```powershell
dotnet run `
    --project ".\SeguimientoFacturacion\SeguimientoFacturacion.Web.csproj"
```

Utilice la URL HTTPS indicada por `launchSettings.json`.

## Certificación de la Fase 1

Los siguientes scripts son de solo lectura:

- `PASO-051B-verificar-pagos.sql`.
- `PASO-051C-3-certificacion-consolidada-pagos.sql`.
- `PASO-051D-certificacion-integral-fase1.sql`.

El último resultado esperado es:

```text
ControlesEjecutados: 25
ControlesCorrectos: 25
ControlesConNovedad: 0
TotalHallazgos: 0
ResultadoFinal: FASE 1 CERTIFICADA
```

## Documentación

- [ARQUITECTURA.md](ARQUITECTURA.md)
- [REGLAS_IMPORTACION.md](REGLAS_IMPORTACION.md)
- [REGLAS_PAGOS.md](REGLAS_PAGOS.md)
- [CONVENCIONES.md](CONVENCIONES.md)
- [ROADMAP.md](ROADMAP.md)
- [CHANGELOG.md](CHANGELOG.md)

## Información sensible

Nunca versionar:

- Contraseñas o cadenas de conexión reales.
- Claves AES, sales o credenciales administrativas.
- `usuarios.dat`.
- Copias de seguridad de SQL Server.
- Archivos de importación con información real de pacientes.
