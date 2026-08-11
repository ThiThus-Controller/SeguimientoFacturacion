# Roadmap

## Estado general

| Fase | Avance | Estado |
|---|---:|---|
| 1. Importación masiva modular | 100 % funcional | Certificada; cierre técnico en curso |
| 2. Gestión manual | 25 % | Diseño y componentes transversales disponibles |
| 3. Dashboard administrativo | 5 % | Requisitos definidos |

## Fase 1 — Importación masiva modular

### Terminado

- Arquitectura por capas y proyectos de pruebas.
- Modelo definitivo y staging por lote.
- Importación de facturas y pacientes.
- Importación de notas crédito y débito.
- Importación de glosas y respuestas.
- Importación de pagos y aplicaciones.
- Distribución automática entre cartera y anticipo.
- Reintentos controlados y recuperación de lotes analizados.
- Descarga de plantillas oficiales.
- Usuarios externos a SQL Server y autorización granular.
- Administración de usuarios, aseguradoras y facturadores.
- Certificación financiera de cuatro escenarios.
- Certificación integral: 25 controles, 0 hallazgos.

### Cierre técnico pendiente

- Consolidar documentación.
- Ejecutar compilación, pruebas y análisis de vulnerabilidades finales.
- Revisar secretos, binarios y archivos sensibles antes de fusionar.
- Fusionar `refactor/importacion-modular` en `main`.
- Crear etiqueta de cierre de la Fase 1.

## Fase 2 — Gestión manual

Antes de implementar cada módulo se revisarán con el usuario las
columnas, acciones, permisos y reglas particulares.

### Consulta y grilla

- AG Grid con consulta paginada del lado del servidor.
- Búsqueda global, filtros, ordenamiento y selección de columnas.
- Copiar y pegar desde Excel con validación.
- Exportación controlada a Excel y PDF.
- Preferencias de grilla por usuario.
- Estados de carga, errores y confirmaciones con SweetAlert2.

### Columnas calculadas

- Días entre fecha de factura y fecha de radicación.
- Días entre fecha de radicación y fecha de objeción.
- Días entre fecha de objeción y fecha de respuesta.
- Saldo vigente y composición financiera.
- Valores glosados, aceptados, aplicados y en anticipo.

Estos valores serán calculados desde datos fuente y no editables.

### Operaciones manuales

- Crear, editar y anular facturas.
- Actualizar información del paciente con control de identidad.
- Crear, editar y anular notas crédito o débito.
- Registrar, responder, levantar, aceptar y conciliar glosas.
- Registrar pagos y distribuir o reversar aplicaciones.
- Gestionar anticipos.
- Motivo obligatorio en operaciones sensibles.
- Concurrencia optimista para impedir sobrescrituras silenciosas.
- Auditoría inmutable antes y después de cada cambio.

### Seguridad

- Verificar permisos en servidor para cada comando.
- Separar consulta, creación, edición, anulación, confirmación y proceso.
- Permitir concesiones y revocaciones particulares.
- Invalidar sesiones cuando una cuenta sea inactivada.

## Fase 3 — Dashboard administrativo

- Facturas totales y por estado.
- Valor facturado, saldo y recuperación.
- Pagos, anticipos, notas y glosas.
- Cartera por aseguradora y antigüedad.
- Indicadores de oportunidad por rangos de días.
- Tendencias mensuales con Chart.js.
- Filtros por fecha, aseguradora, facturador y estado.
- Exportación de reportes.

## Preparación para producción

- Pruebas de integración con SQL Server aislado.
- Pruebas de autorización negativas.
- Copias de seguridad y restauración documentadas.
- Rotación de claves de cifrado.
- Protección de datos y política de retención.
- Serilog con destino y nivel configurables.
- Health checks y observabilidad.
- Cabeceras de seguridad, HTTPS y cookies endurecidas.
- Despliegue reproducible y procedimiento de reversión.
