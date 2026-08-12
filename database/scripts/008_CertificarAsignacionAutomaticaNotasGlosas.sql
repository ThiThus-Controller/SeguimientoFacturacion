/*
    PASO 052G-2
    Certificación no destructiva posterior a las pruebas
    funcionales de notas crédito, débito y glosas.

    Opcional: asigne @LoteId para revisar un lote concreto.
    Si queda NULL se utiliza el último lote de notas registrado.
*/

USE [Seguimiento];
GO

SET NOCOUNT ON;

DECLARE @LoteId uniqueidentifier = NULL;

IF OBJECT_ID(
       N'facturacion.NotasFactura',
       N'U') IS NULL
   OR COL_LENGTH(
       N'facturacion.NotasFactura',
       N'GlosaId') IS NULL
BEGIN
    RAISERROR(
        'La estructura de notas y glosas está incompleta.',
        16,
        1);
    RETURN;
END;

IF @LoteId IS NULL
BEGIN
    SELECT TOP (1)
        @LoteId = l.Id
    FROM importacion.LotesImportacion AS l
    WHERE l.Tipo = 3
    ORDER BY l.FechaCreacionUtc DESC,
             l.Id DESC;
END;

SELECT
    DB_NAME() AS BaseDatosActual,
    @LoteId AS LoteSeleccionado,
    CASE
        WHEN @LoteId IS NULL THEN 'SIN LOTES DE NOTAS'
        ELSE 'LOTE LOCALIZADO'
    END AS EstadoSeleccion;

SELECT
    l.Id AS LoteId,
    l.NombreArchivo,
    l.Estado,
    CASE l.Estado
        WHEN 1 THEN 'Pendiente'
        WHEN 2 THEN 'Analizada'
        WHEN 3 THEN 'Confirmada'
        WHEN 4 THEN 'Procesando'
        WHEN 5 THEN 'Completada'
        WHEN 6 THEN 'Fallida'
        WHEN 7 THEN 'Cancelada'
        ELSE 'Desconocida'
    END AS EstadoDescripcion,
    l.TotalFilas,
    l.TotalFilasValidas,
    l.TotalFilasConError,
    l.TotalErrores,
    l.ConfirmadoPor,
    l.FechaInicioProcesamientoUtc,
    l.FechaFinalizacionUtc
FROM importacion.LotesImportacion AS l
WHERE l.Id = @LoteId;

SELECT
    i.NumeroFila,
    i.Columna,
    i.Codigo,
    i.Mensaje
FROM importacion.InconsistenciasImportacion AS i
WHERE i.LoteImportacionId = @LoteId
ORDER BY i.NumeroFila,
         i.Codigo;

DECLARE @Hallazgos TABLE
(
    Orden int NOT NULL,
    Area varchar(30) NOT NULL,
    Validacion nvarchar(220) NOT NULL,
    Hallazgos bigint NOT NULL
);

INSERT INTO @Hallazgos
SELECT
    10,
    'NOTAS',
    N'Notas crédito vigentes sin glosa interna',
    COUNT_BIG(*)
FROM facturacion.NotasFactura AS n
WHERE n.Tipo = 1
  AND n.Anulada = 0
  AND n.GlosaId IS NULL;

INSERT INTO @Hallazgos
SELECT
    20,
    'NOTAS',
    N'Notas débito asociadas indebidamente a una glosa',
    COUNT_BIG(*)
FROM facturacion.NotasFactura AS n
WHERE n.Tipo = 2
  AND n.GlosaId IS NOT NULL;

INSERT INTO @Hallazgos
SELECT
    30,
    'RELACION',
    N'Notas asociadas a una glosa de otra factura',
    COUNT_BIG(*)
FROM facturacion.NotasFactura AS n
INNER JOIN facturacion.Glosas AS g
    ON g.Id = n.GlosaId
WHERE n.FacturaId <> g.FacturaId;

INSERT INTO @Hallazgos
SELECT
    40,
    'FECHAS',
    N'Notas crédito anteriores a la glosa asociada',
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
    'GLOSAS',
    N'Notas crédito respaldadas por glosas sin valor aceptado',
    COUNT_BIG(*)
FROM facturacion.NotasFactura AS n
INNER JOIN facturacion.Glosas AS g
    ON g.Id = n.GlosaId
WHERE n.Tipo = 1
  AND n.Anulada = 0
  AND g.ValorAceptado <= 0;

INSERT INTO @Hallazgos
SELECT
    60,
    'CUPOS',
    N'Glosas cuyo acumulado de NC supera el valor aceptado',
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
) AS glosasExcedidas;

INSERT INTO @Hallazgos
SELECT
    70,
    'ESTADOS',
    N'Notas vigentes relacionadas con facturas anuladas',
    COUNT_BIG(*)
FROM facturacion.NotasFactura AS n
INNER JOIN facturacion.Facturas AS f
    ON f.Id = n.FacturaId
WHERE n.Anulada = 0
  AND f.EstadoId IN (3, 5);

INSERT INTO @Hallazgos
SELECT
    80,
    'STAGING',
    N'Notas temporales pertenecientes a lotes completados',
    COUNT_BIG(*)
FROM importacion.NotasFacturaTemporales AS n
INNER JOIN importacion.LotesImportacion AS l
    ON l.Id = n.LoteImportacionId
WHERE l.Tipo = 3
  AND l.Estado = 5;

SELECT
    Orden,
    Area,
    Validacion,
    Hallazgos,
    CASE
        WHEN Hallazgos = 0 THEN 'CORRECTO'
        ELSE 'REVISAR'
    END AS Estado
FROM @Hallazgos
ORDER BY Orden;

DECLARE @CodigosEsperados TABLE
(
    Orden int NOT NULL,
    Codigo varchar(100) NOT NULL,
    Escenario nvarchar(180) NOT NULL
);

INSERT INTO @CodigosEsperados
VALUES
    (10,
     'FACTURA_SIN_GLOSA_PARA_NC',
     N'Factura sin glosa'),
    (20,
     'FACTURA_SIN_GLOSA_ACEPTADA_PARA_NC',
     N'Glosas sin valor aceptado'),
    (30,
     'NOTA_ANTERIOR_GLOSA',
     N'Fecha de NC anterior a la glosa'),
    (40,
     'GLOSA_SIN_CUPO_SUFICIENTE_NC',
     N'Cupo individual o acumulado insuficiente'),
    (50,
     'GLOSA_AMBIGUA_PARA_NC',
     N'Más de una glosa elegible');

SELECT
    esperado.Orden,
    esperado.Escenario,
    esperado.Codigo,
    COUNT(i.Id) AS Evidencias,
    CASE
        WHEN COUNT(i.Id) > 0 THEN 'CERTIFICADO'
        ELSE 'PENDIENTE DE PRUEBA'
    END AS Estado
FROM @CodigosEsperados AS esperado
LEFT JOIN importacion.InconsistenciasImportacion AS i
    ON i.Codigo = esperado.Codigo
GROUP BY
    esperado.Orden,
    esperado.Escenario,
    esperado.Codigo
ORDER BY esperado.Orden;

SELECT
    n.Id AS NotaId,
    n.FacturaId,
    CASE n.Tipo
        WHEN 1 THEN 'NOTA CREDITO'
        WHEN 2 THEN 'NOTA DEBITO'
        ELSE 'DESCONOCIDA'
    END AS TipoNota,
    n.Numero,
    n.Fecha AS FechaNota,
    n.Valor AS ValorNota,
    n.GlosaId,
    g.FechaGlosa,
    g.ValorGlosa,
    g.ValorAceptado,
    SUM(
        CASE
            WHEN n.Tipo = 1 AND n.Anulada = 0
                THEN n.Valor
            ELSE 0
        END) OVER (PARTITION BY n.GlosaId)
        AS TotalNcVigenteGlosa,
    CASE
        WHEN n.Tipo = 2 AND n.GlosaId IS NULL
            THEN 'CORRECTO'
        WHEN n.Tipo = 1
         AND n.GlosaId IS NOT NULL
         AND n.FacturaId = g.FacturaId
         AND n.Fecha >= g.FechaGlosa
         AND g.ValorAceptado > 0
            THEN 'CORRECTO'
        ELSE 'REVISAR'
    END AS ResultadoRelacion
FROM facturacion.NotasFactura AS n
LEFT JOIN facturacion.Glosas AS g
    ON g.Id = n.GlosaId
WHERE n.FechaCreacionUtc >=
      DATEADD(day, -7, SYSUTCDATETIME())
ORDER BY n.FechaCreacionUtc DESC,
         n.FacturaId,
         n.Numero;

DECLARE @TotalHallazgos bigint =
(
    SELECT COALESCE(SUM(Hallazgos), 0)
    FROM @Hallazgos
);

DECLARE @ControlesPendientes int =
(
    SELECT COUNT(*)
    FROM @CodigosEsperados AS esperado
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM importacion.InconsistenciasImportacion AS i
        WHERE i.Codigo = esperado.Codigo
    )
);

SELECT
    @LoteId AS LoteRevisado,
    @TotalHallazgos AS TotalHallazgosIntegridad,
    @ControlesPendientes AS ControlesNegativosPendientes,
    CASE
        WHEN @TotalHallazgos > 0
            THEN 'CERTIFICACION CON NOVEDADES'
        WHEN @ControlesPendientes > 0
            THEN 'INTEGRIDAD CORRECTA; FALTAN ESCENARIOS NEGATIVOS'
        ELSE 'PASO 052G-2 CERTIFICADO'
    END AS ResultadoFinal;
GO
