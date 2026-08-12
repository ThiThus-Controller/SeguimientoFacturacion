/*
    PASO 052E
    Verificación no destructiva de las reglas:
    - Toda NC debe estar respaldada por una glosa de la misma factura.
    - La fecha de la NC no puede anteceder a la glosa.
    - Las NC vigentes no pueden superar el valor aceptado.
    - Una factura anulada no conserva valores aplicados a cartera.
*/

USE [Seguimiento];
GO

SET NOCOUNT ON;

SELECT
    DB_NAME() AS BaseDatosActual,
    CASE
        WHEN COL_LENGTH(
            'facturacion.NotasFactura',
            'GlosaId') IS NOT NULL
         AND COL_LENGTH(
            'importacion.NotasFacturaTemporales',
            'GlosaId') IS NOT NULL
         AND COL_LENGTH(
            'facturacion.Glosas',
            'VersionFila') IS NOT NULL
            THEN 'ESTRUCTURA CORRECTA'
        ELSE 'ESTRUCTURA INCOMPLETA'
    END AS EstadoEstructura;

SELECT
    fk.name AS Restriccion,
    OBJECT_SCHEMA_NAME(fk.parent_object_id) AS Esquema,
    OBJECT_NAME(fk.parent_object_id) AS Tabla
FROM sys.foreign_keys AS fk
WHERE fk.name IN
(
    'FK_NotasFactura_Glosas_GlosaId',
    'FK_NotasFacturaTemporales_Glosas_GlosaId'
)
ORDER BY fk.name;

SELECT
    cc.name AS Restriccion,
    OBJECT_SCHEMA_NAME(cc.parent_object_id) AS Esquema,
    OBJECT_NAME(cc.parent_object_id) AS Tabla,
    cc.definition AS Definicion
FROM sys.check_constraints AS cc
WHERE cc.name IN
(
    'CK_NotasFactura_Glosa',
    'CK_NotasFacturaTemporales_Glosa'
)
ORDER BY cc.name;

DECLARE @Hallazgos TABLE
(
    Orden int NOT NULL,
    Validacion nvarchar(200) NOT NULL,
    Hallazgos bigint NOT NULL
);

IF COL_LENGTH(
       'facturacion.NotasFactura',
       'GlosaId') IS NOT NULL
BEGIN
    INSERT INTO @Hallazgos
    SELECT
        10,
        N'Notas crédito vigentes sin glosa',
        COUNT_BIG(*)
    FROM facturacion.NotasFactura AS n
    WHERE n.Tipo = 1
      AND n.Anulada = 0
      AND n.GlosaId IS NULL;

    INSERT INTO @Hallazgos
    SELECT
        20,
        N'Notas débito asociadas a glosa',
        COUNT_BIG(*)
    FROM facturacion.NotasFactura AS n
    WHERE n.Tipo = 2
      AND n.GlosaId IS NOT NULL;

    INSERT INTO @Hallazgos
    SELECT
        30,
        N'Notas asociadas a glosa de otra factura',
        COUNT_BIG(*)
    FROM facturacion.NotasFactura AS n
    INNER JOIN facturacion.Glosas AS g
        ON g.Id = n.GlosaId
    WHERE n.FacturaId <> g.FacturaId;

    INSERT INTO @Hallazgos
    SELECT
        40,
        N'Notas crédito anteriores a la glosa',
        COUNT_BIG(*)
    FROM facturacion.NotasFactura AS n
    INNER JOIN facturacion.Glosas AS g
        ON g.Id = n.GlosaId
    WHERE n.Tipo = 1
      AND n.Anulada = 0
      AND n.Fecha < g.FechaGlosa;

    INSERT INTO @Hallazgos
    SELECT
        50,
        N'Glosas con NC vigente superior al valor aceptado',
        COUNT_BIG(*)
    FROM
    (
        SELECT
            g.Id
        FROM facturacion.Glosas AS g
        LEFT JOIN facturacion.NotasFactura AS n
            ON n.GlosaId = g.Id
           AND n.Tipo = 1
           AND n.Anulada = 0
        GROUP BY
            g.Id,
            g.ValorAceptado
        HAVING COALESCE(SUM(n.Valor), 0) > g.ValorAceptado
    ) AS excedidas;
END;
ELSE
BEGIN
    INSERT INTO @Hallazgos
    VALUES
        (10, N'Notas crédito vigentes sin glosa', 1),
        (20, N'Notas débito asociadas a glosa', 1),
        (30, N'Notas asociadas a glosa de otra factura', 1),
        (40, N'Notas crédito anteriores a la glosa', 1),
        (50, N'Glosas con NC vigente superior al valor aceptado', 1);
END;

INSERT INTO @Hallazgos
SELECT
    60,
    N'Facturas anuladas con valor aplicado a cartera',
    COUNT_BIG(*)
FROM cartera.AplicacionesPago AS a
INNER JOIN facturacion.Facturas AS f
    ON f.Id = a.FacturaId
WHERE f.EstadoId IN (3, 5)
  AND a.ValorAplicado > 0;

SELECT
    Orden,
    Validacion,
    Hallazgos,
    CASE
        WHEN Hallazgos = 0 THEN 'CORRECTO'
        ELSE 'REVISAR'
    END AS Estado
FROM @Hallazgos
ORDER BY Orden;

SELECT
    CASE
        WHEN SUM(Hallazgos) = 0
            THEN 'VERIFICACION CORRECTA'
        ELSE 'VERIFICACION CON NOVEDADES'
    END AS ResultadoFinal,
    SUM(Hallazgos) AS TotalHallazgos
FROM @Hallazgos;
GO
