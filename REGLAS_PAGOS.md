# Reglas de negocio de pagos

## Contrato de importación

La plantilla oficial contiene, en este orden:

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

`RETENCION` y `RETE ICA` son datos informativos no negativos. No se
suman ni se restan de `VALOR PAGADO` para decidir cuánto se aplica.

## Agrupación del recibo

Un pago definitivo se identifica por `AseguradoraId + Recibo`. Varias
filas pueden pertenecer al mismo recibo y a facturas distintas. Los
valores pagados, retenciones y rete ICA del recibo son la suma de sus
filas. La aseguradora debe existir y coincidir con la factura.

## Distribución automática

Cada fila conserva el dinero recibido y lo divide así:

```text
Saldo disponible = max(0, Valor factura + Notas débito
                           - Notas crédito - Pagos aplicados previos)

Valor aplicado = min(Valor pagado de la fila, Saldo disponible)
Valor anticipo = Valor pagado de la fila - Valor aplicado
```

Siempre debe cumplirse:

```text
Valor recibido = Valor aplicado + Valor anticipo
```

Los pagos no se rechazan por superar el saldo. El exceso se registra
como anticipo.

## Facturas anuladas o agotadas por nota crédito

- Los estados `3` y `5` se consideran anulados.
- Si las notas crédito vigentes son iguales o superiores a
  `Valor factura + Notas débito`, la factura se considera agotada.
- En ambos casos, el nuevo pago se registra 100 % como anticipo.

## Glosas

Una glosa abierta es informativa y no reduce el saldo disponible para
un pago. Cuando una glosa es aceptada, su efecto financiero se
materializa mediante una nota crédito.

## Revalidación definitiva

La distribución calculada durante el análisis es preliminar. Al
procesar definitivamente el lote, el sistema vuelve a consultar notas,
estado y pagos aplicados, y recalcula las filas en orden determinista.
Esto evita aplicar importes con un saldo desactualizado.

## Escenarios financieros certificados

La certificación funcional de la Fase 1 cubre los siguientes casos:

| Escenario | Comportamiento esperado |
|---|---|
| Factura activa con saldo | Aplica hasta el saldo disponible |
| Factura anulada | Conserva el valor recibido como anticipo |
| Factura saldada antes del lote | Conserva el valor recibido como anticipo |
| Pago con excedente | Aplica hasta el saldo y registra el excedente como anticipo |

La ejecución certificada incluyó 14 facturas activas con saldo, 3
facturas anuladas, 1 factura saldada antes del lote y 1 pago con
excedente. Todos los casos cumplieron las fórmulas de distribución.

## Controles de cierre

Después de procesar un lote deben verificarse estas condiciones:

- No quedan pagos ni aplicaciones temporales del lote completado.
- Ninguna aplicación supera el valor recibido ni el saldo disponible.
- Cada pago cumple `ValorRecibido = ValorAplicado + ValorAnticipo`.
- Los pagos de facturas anuladas no incrementan la cartera aplicada.
- Los lotes conservan usuario y fechas de confirmación y procesamiento.
- Los valores consolidados coinciden con el detalle de las aplicaciones.

## Cambios posteriores

Al anular una factura o registrar una nota crédito que reduzca el saldo
por debajo de pagos previamente aplicados, la porción excedente debe
reclasificarse de aplicación a anticipo, sin alterar el total recibido.
Las operaciones deben dejar auditoría y ejecutarse en una transacción.
