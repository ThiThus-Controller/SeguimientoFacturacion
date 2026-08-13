# Fase 2 — Gestión manual

## Estado del documento

Propuesta funcional para revisión y aprobación antes de implementar la
interfaz de facturas y pacientes.

## Objetivo

Permitir consultar, crear y modificar información de facturación de
forma segura, con una experiencia cercana a Excel, sin trasladar reglas
de negocio al navegador ni permitir cambios que rompan la trazabilidad
financiera.

## Alcance del primer módulo

El primer módulo de la Fase 2 cubrirá:

- Consulta paginada de facturas y pacientes.
- Filtros, búsqueda, ordenamiento y selección de columnas.
- Creación manual de facturas y pacientes.
- Edición controlada de datos básicos.
- Cambio de radicación, facturador, atención y costo.
- Anulación controlada de facturas.
- Indicadores de oportunidad calculados.
- Resumen financiero de cada factura.
- Autorización, concurrencia y auditoría de cada modificación.

No incluirá todavía la edición manual de notas, glosas o pagos. Estos
procesos tendrán diseños funcionales independientes porque modifican el
saldo y cuentan con permisos propios.

## Fundamentos existentes

La solución ya dispone de:

- `FacturaResumenDto` con datos clínico-administrativos y financieros.
- `FiltroFacturasDto` con paginación y filtros principales.
- `ConsultaFacturasEfCore` optimizada para lectura.
- `CalculadoraIndicadoresTiempoFactura` para días de oportunidad.
- Permisos `Facturas.*` y `Pacientes.*`.
- Catálogos administrables de aseguradoras y facturadores.
- Usuario autenticado disponible para trazabilidad.

La implementación debe ampliar estos componentes sin duplicar sus
reglas.

La calculadora y los comandos manuales utilizarán
`CodigosEstadoFactura.EsAnulada` para tratar los códigos 3 y 5 como
estados vigentes de anulación en indicadores y reglas financieras.

## Diseño de la pantalla

La pantalla tendrá cinco zonas:

1. Barra de acciones autorizadas.
2. Filtros rápidos y búsqueda global.
3. AG Grid con paginación del lado del servidor.
4. Panel lateral de detalle y relaciones.
5. Historial de modificaciones de la factura seleccionada.

### Barra de acciones

| Acción | Permiso | Comportamiento |
|---|---|---|
| Crear factura | `Facturas.Crear` | Abre formulario completo |
| Editar detalle | `Facturas.Editar` | Abre edición controlada |
| Anular factura | `Facturas.Anular` | Exige motivo y confirmación |
| Exportar | `Facturas.Ver` | Exporta el resultado filtrado |
| Importar | `Facturas.Importar` | Conserva el módulo de Fase 1 |
| Actualizar | `Facturas.Ver` | Recarga datos y totales |

La interfaz puede ocultar acciones no autorizadas, pero el servidor
siempre volverá a comprobar el permiso.

## Columnas de la grilla

### Identificación de factura

| Columna | Visible inicialmente | Editable | Regla |
|---|---:|---:|---|
| FE | Sí | No | Identificador estable |
| Prefijo | Sí | No | Inmutable después de crear |
| Factura | Sí | No | Inmutable después de crear |
| Fecha factura | Sí | Formulario | Fecha obligatoria |
| Aseguradora | Sí | Formulario | Debe existir y estar activa |
| Valor factura | Sí | Formulario | Mayor que cero, dos decimales |
| Fecha radicación | Sí | Sí | Nula o posterior/igual a factura |
| Días factura-radicación | Sí | No | Calculado en días calendario |

### Paciente

| Columna | Visible inicialmente | Editable | Regla |
|---|---:|---:|---|
| Tipo documento | Sí | Acción especial | Parte de la identidad |
| Número documento | Sí | Acción especial | Conserva letras y ceros |
| Nombre completo | Sí | Formulario | Se actualiza de forma consistente |

Tipo y número de documento no se editarán como celdas independientes.
Una corrección de identidad utilizará la acción **Reasignar paciente**,
validará si el paciente ya existe y requerirá `Facturas.Editar` y
`Pacientes.Editar`.

Al cambiar el nombre de un paciente se actualizará el registro maestro
y las facturas relacionadas en una sola transacción, evitando nombres
diferentes para la misma identificación.

### Atención y control

| Columna | Visible inicialmente | Editable | Editor |
|---|---:|---:|---|
| Atención | Sí | Sí | Lista de catálogo activa |
| Costo | Sí | Sí | Lista de catálogo activa |
| Número admisión | Sí | Sí | Texto normalizado |
| Fecha admisión | Sí | Sí | Selector de fecha |
| Estado | Sí | No directo | Acción de negocio |
| Facturador | Sí | Sí | Lista de facturadores activos |

La fecha de admisión no puede ser posterior a la fecha de factura.
El estado no se cambiará mediante una lista desplegable porque activa
reglas financieras y de auditoría.

### Resumen financiero

| Columna | Editable | Fórmula o fuente |
|---|---:|---|
| Notas débito | No | Notas débito vigentes |
| Notas crédito | No | Notas crédito vigentes |
| Pagos aplicados | No | Aplicaciones definitivas |
| Glosa pendiente | No | Glosas abiertas o respondidas |
| Saldo cartera | No | Valor + débito - crédito - aplicado |
| Saldo disponible gestión | No | Saldo cartera - glosa pendiente |

Los cálculos se realizan en el servidor. AG Grid solo presenta los
resultados y aplica formato monetario.

### Indicadores de oportunidad

| Indicador | Inicio | Final | Sin fecha final |
|---|---|---|---|
| Factura a radicación | Fecha factura | Fecha radicación | Calcula hasta fecha de corte |
| Radicación a objeción | Fecha radicación | Primera glosa | Calcula hasta fecha de corte |
| Objeción a respuesta | Fecha glosa | Fecha respuesta | Calcula hasta fecha de corte |

Para una factura con varias glosas, la grilla mostrará el máximo de días
entre objeción y respuesta. El panel de detalle mostrará cada glosa por
separado.

Los estados visuales serán:

- **Definitivo**: ambas fechas existen.
- **Pendiente**: se calcula hasta la fecha de corte.
- **No aplica**: faltan antecedentes requeridos o la factura está anulada.
- **Inconsistente**: las fechas están en orden cronológico inválido.

## Estrategia de edición

### Edición rápida con guardado automático

Se permitirá doble clic y guardado al terminar la edición para:

- Fecha de radicación.
- Atención.
- Costo.
- Número de admisión.
- Fecha de admisión.
- Facturador.

Cada celda se envía al servidor como un comando individual. Si la
validación falla, el valor visual regresa al anterior y se presenta el
mensaje exacto.

### Edición mediante formulario

Se utilizará un formulario para cambios que afectan varias reglas:

- Fecha de factura.
- Aseguradora.
- Valor original.
- Identidad o nombre del paciente.
- Estado de la factura.

El formulario mostrará el valor anterior y el nuevo antes de confirmar.

### Restricciones financieras

- Prefijo, número y FE nunca se modifican después de crear.
- No se eliminan facturas físicamente.
- Aseguradora y valor no se modifican cuando existen notas, glosas o
  aplicaciones de pago; el sistema explicará cuál dependencia bloquea.
- Una factura anulada es de solo lectura, salvo observaciones y acciones
  administrativas expresamente diseñadas.
- La anulación exige motivo.
- Las notas y glosas activas deben resolverse antes de anular.
- Las aplicaciones existentes deben reclasificarse a anticipo dentro de
  la misma transacción de anulación.
- Los estados de anulación con códigos 3 y 5 se consultan y gestionan.
- El formulario de anulación permite seleccionar el estado 3 o 5 según
  corresponda al proceso empresarial y exige un motivo.
- Ambos estados activan las mismas restricciones sobre notas, glosas,
  saldo y aplicaciones de pago.

## Creación manual

El formulario solicitará los mismos campos de la plantilla oficial de
facturas. El sistema:

1. Normaliza prefijo y número.
2. Construye FE y lo presenta como solo lectura.
3. Comprueba que FE no exista.
4. Busca el paciente por tipo y número de documento.
5. Permite utilizar el paciente existente o crear uno nuevo.
6. Valida catálogos y fechas.
7. Guarda paciente y factura en una transacción.
8. Registra usuario, fecha y evento de auditoría.

## Filtros y paginación

Filtros iniciales:

- Búsqueda por FE, prefijo, factura, documento, paciente o aseguradora.
- Aseguradora.
- Estado.
- Facturador.
- Rango de fecha de factura.
- Rango de fecha de radicación.
- Con saldo / sin saldo.
- Radicada / sin radicar.
- Con glosa pendiente.

La página inicial tendrá 50 registros. Se permitirán tamaños 25, 50,
100 y 200. El ordenamiento, filtrado y paginación se ejecutarán en SQL
Server; nunca se cargará toda la base en el navegador.

## Copiar y pegar

La primera entrega permitirá copiar celdas desde la grilla. El pegado
masivo no guardará directamente: abrirá una vista previa de validación
y reutilizará el modelo de staging de la Fase 1. Esto evita operaciones
parciales e inconsistencias provocadas por pegar cientos de filas.

## Concurrencia

Facturas y pacientes incorporarán una versión de fila administrada por
SQL Server. Cada comando enviará esa versión.

Si otro usuario modificó el registro, el sistema no sobrescribirá el
cambio. Mostrará:

- Datos que el usuario intentó guardar.
- Versión actual de la base.
- Opción de recargar y volver a aplicar el cambio.

## Auditoría

Cada operación manual registrará un evento inmutable con:

- Usuario autenticado.
- Fecha UTC.
- Tipo de entidad e identificador.
- Acción ejecutada.
- Valores anteriores y nuevos en JSON.
- Motivo cuando sea obligatorio.
- Identificador de correlación.

Los campos `CreadoPor` y `ModificadoPor` continúan siendo el resumen de
trazabilidad; no sustituyen el historial inmutable.

## Manejo de errores

- Validación de celda: mensaje junto a la celda y restauración del valor.
- Concurrencia: diálogo comparativo, sin sobrescritura automática.
- Permiso insuficiente: respuesta 403 y mensaje funcional.
- Registro inexistente: recarga de la fila.
- Error transaccional: ningún cambio parcial.
- Error inesperado: código de correlación sin exponer SQL ni secretos.

## Entregas técnicas posteriores

1. Contratos de consulta compatibles con AG Grid.
2. Versión de fila y migración de concurrencia.
3. Servicios de creación y edición de factura/paciente.
4. Auditoría inmutable de comandos manuales.
5. Endpoints MVC/JSON y políticas de autorización.
6. Grilla de solo lectura con filtros del servidor.
7. Edición rápida y formularios sensibles.
8. Exportación, copiar/pegar y pruebas integrales.

## Criterios de aceptación del módulo

- Un usuario de consulta nunca puede modificar datos.
- Cada columna respeta su modo de edición definido.
- Los cálculos financieros coinciden con la Fase 1.
- Los tres indicadores de días se calculan en el servidor.
- Ningún conflicto de concurrencia sobrescribe información.
- Toda modificación manual queda auditada.
- No existen accesos directos a SQL desde Web.
- Las pruebas de Domain, Application, Infrastructure y Web son correctas.

## Gestión manual de glosas y respuestas

La glosa utiliza los estados `Abierta`, `Respondida`, `Aceptada`,
`Levantada`, `Conciliada`, `Anulada` y `EnNegociacion`. El estado
`Anulada` es exclusivo de la operación manual. `EnNegociacion` identifica
una aceptación parcial cuyo valor restante todavía no tiene una decisión
definitiva.

La observación admite hasta 1.000 caracteres. Es opcional al crear o
responder, y obligatoria al aceptar, levantar, conciliar o anular.

La anulación solo podrá ejecutarse cuando no existan notas crédito
vigentes asociadas. Una glosa anulada no participa en indicadores ni
conserva valor aceptado. La operación exige permiso `Glosas.Anular`,
concurrencia optimista y auditoría inmutable.

Una glosa aceptada o conciliada no reduce directamente la cartera ni
genera automáticamente una nota crédito. El sistema mostrará el valor
aceptado pendiente de respaldo hasta registrar la nota correspondiente.

Una aceptación parcial conserva como pendiente la diferencia entre el
valor glosado y el valor aceptado acumulado. El valor aceptado nunca puede
disminuir, porque podría dejar notas crédito vigentes sin respaldo. Puede
ampliarse posteriormente y ese incremento habilita nuevo cupo para otra
nota crédito asociada a la misma glosa.

Si la institución gana la discusión, la glosa se concilia conservando el
valor aceptado acumulado; la diferencia queda reconocida a su favor y se
aplicará a cartera únicamente cuando se registre el pago. Si pierde la
discusión, el valor aceptado puede ampliarse hasta el valor total glosado y
la nueva nota crédito consumirá exclusivamente ese cupo adicional.

## Decisiones funcionales propuestas

Antes de iniciar la implementación se solicita aprobar o ajustar:

1. Los indicadores se expresan en días calendario.
2. El nombre del paciente se mantiene único por identificación y se
   propaga a sus facturas.
3. Valor y aseguradora se bloquean cuando existen dependencias.
4. La anulación reclasifica aplicaciones de pago a anticipos.
5. El pegado masivo utiliza validación previa y staging.
6. Los códigos 3 y 5 se administran como estados válidos de anulación y
   comparten las mismas protecciones financieras.
