# Reglas de importación masiva

## Principios

- Solo se admiten archivos `.xlsx` de hasta 50 MB.
- Los encabezados están en la fila 1 y los datos comienzan en la fila 2.
- Los encabezados se normalizan, pero deben pertenecer al contrato
  seleccionado.
- El archivo se analiza antes de modificar tablas definitivas.
- Un lote con errores no puede confirmarse ni procesarse.
- Confirmar y procesar son operaciones diferentes y autorizadas.
- El procesamiento revalida referencias y reglas dentro de una
  transacción.
- Los datos de staging se eliminan únicamente al terminar correctamente.

## Flujo operativo

1. Seleccionar el tipo de importación.
2. Descargar la plantilla oficial.
3. Diligenciar sin cambiar encabezados ni agregar columnas.
4. Registrar y analizar el archivo.
5. Corregir todos los errores identificados.
6. Volver a cargar el archivo corregido.
7. Confirmar el lote válido.
8. Procesar definitivamente.
9. Revisar los totales y el usuario responsable.

## Facturas y pacientes

Encabezados oficiales, en orden:

1. `FE`
2. `PREFIJO`
3. `FACTURA`
4. `FECHA FACTURA`
5. `ASEGURADORA`
6. `VALOR`
7. `FECHA DE RADICACION`
8. `TIPO DTO`
9. `NUMERO DTO`
10. `NOMBRE COMPLETO`
11. `ATENCION`
12. `COSTO`
13. `NO ADMISION`
14. `FECHA ADMISION`
15. `ESTADO DE DTO`
16. `FACTURADOR`

Reglas relevantes:

- `FE` debe coincidir con `PREFIJO + FACTURA`.
- El valor debe ser mayor que cero y conservar dos decimales.
- Aseguradora, tipo de documento, atención, costo, estado y facturador
  deben existir en sus catálogos.
- La fecha de radicación puede quedar vacía, especialmente en facturas
  anuladas.
- La fecha de radicación no puede ser anterior a la factura.
- La fecha de admisión no puede ser posterior a la fecha de factura.
- Tipo y número de documento identifican al paciente.

## Notas crédito y débito

Encabezados oficiales:

1. `FE`
2. `PREFIJO`
3. `FACTURA`
4. `ASEGURADORA`
5. `TIPO NOTA`
6. `FECHA NOTA`
7. `NUMERO NOTA`
8. `VALOR NOTA`

Valores de `TIPO NOTA`:

- `CREDITO`: disminuye el saldo.
- `DEBITO`: aumenta el saldo.

Reglas relevantes:

- La factura debe existir y coincidir con la aseguradora.
- La factura no puede estar anulada.
- Número y valor de la nota son obligatorios.
- El valor debe ser mayor que cero.
- Una nota se identifica por factura, tipo y número.
- Una nota débito no requiere una glosa.
- Toda nota crédito debe estar respaldada por una glosa de la misma
  factura con valor aceptado pendiente.
- El sistema asocia internamente la nota crédito con la única glosa
  elegible; el usuario no diligencia fecha ni valor de glosa en esta
  plantilla.
- La fecha de la nota crédito no puede ser anterior a la glosa que la
  respalda.
- El valor acumulado de las notas crédito vigentes y del mismo archivo
  no puede superar el valor aceptado de la glosa.
- Si no existe una glosa elegible o existen varias posibles, el lote se
  bloquea para evitar una asociación contable arbitraria.
- Una nota crédito puede agotar el saldo, pero no crea un pago.

## Glosas

Encabezados oficiales:

1. `FE`
2. `PREFIJO`
3. `FACTURA`
4. `ASEGURADORA`
5. `FECHA GLOSA`
6. `VALOR GLOSA`
7. `FECHA RTA GLOSA`
8. `ESTADO GLOSA`
9. `VALOR ACEPTADO`

También se admite `FECHA RESPUESTA GLOSA` como alias de
`FECHA RTA GLOSA`.

Estados válidos:

| Código | Texto | Fecha de respuesta | Valor aceptado |
|---:|---|---|---:|
| 1 | ABIERTA | Vacía | 0 |
| 2 | RESPONDIDA | Obligatoria | 0 |
| 3 | ACEPTADA | Obligatoria | Mayor que 0 y hasta el valor glosado |
| 4 | LEVANTADA | Obligatoria | 0 |
| 5 | CONCILIADA | Obligatoria | Entre 0 y el valor glosado |

La fecha de respuesta no puede ser anterior a la fecha de glosa. Una
factura anulada no admite glosas. La glosa no disminuye directamente el
saldo de cartera; el valor aceptado se respalda mediante una nota crédito
cuando corresponda.

## Pagos y aplicaciones

Encabezados oficiales:

1. `FE`
2. `PREFIJO`
3. `FACTURA`
4. `ASEGURADORA`
5. `VALOR PAGADO`
6. `RETENCION`
7. `RETE ICA`
8. `FECHA DE PAGO`
9. `RECIBO`
10. `NOTAS`

Reglas relevantes:

- La factura debe existir y coincidir con la aseguradora.
- El valor pagado debe ser mayor que cero.
- Retención y rete ICA son informativas y no negativas.
- El recibo es obligatorio y único dentro de la aseguradora.
- El pago se distribuye automáticamente entre aplicación y anticipo.
- Una factura anulada o previamente saldada recibe 100 % como anticipo.
- Un excedente no se rechaza: se conserva como anticipo.

Consulte [REGLAS_PAGOS.md](REGLAS_PAGOS.md) para conocer las fórmulas.

## Catálogos

Los textos se normalizan antes de compararse. Un valor no reconocido
produce una inconsistencia `CATALOGO_*_NO_MAPEADO`; nunca se crea el
catálogo silenciosamente durante la carga.

Los catálogos de aseguradoras y facturadores se administran desde
módulos autorizados. Inactivar un catálogo evita su uso futuro sin
eliminar su historial.

## Reintentos y duplicados

- Un lote válido en estado `Analizada` debe recuperarse para continuar
  con confirmación y procesamiento.
- Un lote con errores puede reemplazarse mediante un archivo corregido.
- Un lote completado no puede procesarse por segunda vez.
- El hash SHA-256 ayuda a identificar archivos equivalentes.
- El nombre del archivo no es por sí solo una clave de duplicidad.

## Datos sensibles

Las plantillas pueden contener datos personales y clínicos. No deben
subirse al repositorio, enviarse por canales no autorizados ni conservarse
en carpetas públicas. Las plantillas oficiales descargables no incluyen
datos reales.
