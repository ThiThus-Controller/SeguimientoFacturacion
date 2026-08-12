/*
    PASO 052G-2
    Preparación no destructiva de casos para certificar la
    asignación automática de notas crédito a glosas.

    El script NO inserta, actualiza ni elimina información.
    Cada conjunto de resultados entrega filas sugeridas que
    pueden copiarse a una copia de PlantillaNotasFactura.xlsx.
*/

USE [Seguimiento];
GO

SET NOCOUNT ON;

IF OBJECT_ID(
       N'facturacion.Facturas',
       N'U') IS NULL
   OR OBJECT_ID(
       N'facturacion.Glosas',
       N'U') IS NULL
   OR OBJECT_ID(
       N'facturacion.NotasFactura',
       N'U') IS NULL
BEGIN
    RAISERROR(
        'No existe la estructura requerida para la certificación.',
        16,
        1);
    RETURN;
END;

DECLARE @FechaActual date = CONVERT(date, GETDATE());
DECLARE @Sufijo varchar(20) =
    CONVERT(char(8), GETDATE(), 112) +
    REPLACE(CONVERT(char(8), GETDATE(), 108), ':', '');

DROP TABLE IF EXISTS #CuposGlosa;

SELECT
    g.Id AS GlosaId,
    g.FacturaId AS FE,
    f.Prefijo,
    f.Numero AS Factura,
    a.Descripcion AS Aseguradora,
    f.FechaFactura,
    f.EstadoId,
    g.FechaGlosa,
    g.Estado AS EstadoGlosa,
    g.ValorGlosa,
    g.ValorAceptado,
    COALESCE(SUM(
        CASE
            WHEN n.Tipo = 1
             AND n.Anulada = 0
                THEN n.Valor
            ELSE 0
        END), 0) AS NotasCreditoVigentes,
    CASE
        WHEN g.ValorAceptado -
             COALESCE(SUM(
                CASE
                    WHEN n.Tipo = 1
                     AND n.Anulada = 0
                        THEN n.Valor
                    ELSE 0
                END), 0) > 0
            THEN g.ValorAceptado -
                 COALESCE(SUM(
                    CASE
                        WHEN n.Tipo = 1
                         AND n.Anulada = 0
                            THEN n.Valor
                        ELSE 0
                    END), 0)
        ELSE 0
    END AS CupoDisponible
INTO #CuposGlosa
FROM facturacion.Glosas AS g
INNER JOIN facturacion.Facturas AS f
    ON f.Id = g.FacturaId
INNER JOIN facturacion.Aseguradoras AS a
    ON a.Id = f.AseguradoraId
LEFT JOIN facturacion.NotasFactura AS n
    ON n.GlosaId = g.Id
WHERE f.EstadoId NOT IN (3, 5)
GROUP BY
    g.Id,
    g.FacturaId,
    f.Prefijo,
    f.Numero,
    a.Descripcion,
    f.FechaFactura,
    f.EstadoId,
    g.FechaGlosa,
    g.Estado,
    g.ValorGlosa,
    g.ValorAceptado;

;WITH Elegibles AS
(
    SELECT
        FE,
        COUNT(*) AS Cantidad
    FROM #CuposGlosa
    WHERE ValorAceptado > 0
      AND CupoDisponible > 0
    GROUP BY FE
),
Disponibilidad AS
(
    SELECT
        N'NC_VALIDA_ASIGNACION_UNICA' AS Caso,
        COUNT_BIG(*) AS Candidatos
    FROM Elegibles
    WHERE Cantidad = 1

    UNION ALL

    SELECT
        N'NOTA_DEBITO_SIN_GLOSA',
        COUNT_BIG(*)
    FROM facturacion.Facturas AS f
    WHERE f.EstadoId NOT IN (3, 5)

    UNION ALL

    SELECT
        N'FACTURA_SIN_GLOSA',
        COUNT_BIG(*)
    FROM facturacion.Facturas AS f
    WHERE f.EstadoId NOT IN (3, 5)
      AND NOT EXISTS
      (
          SELECT 1
          FROM facturacion.Glosas AS g
          WHERE g.FacturaId = f.Id
      )

    UNION ALL

    SELECT
        N'GLOSA_SIN_VALOR_ACEPTADO',
        COUNT_BIG(DISTINCT FE)
    FROM #CuposGlosa AS c
    WHERE EXISTS
    (
        SELECT 1
        FROM #CuposGlosa AS interna
        WHERE interna.FE = c.FE
    )
      AND NOT EXISTS
    (
        SELECT 1
        FROM #CuposGlosa AS interna
        WHERE interna.FE = c.FE
          AND interna.ValorAceptado > 0
    )

    UNION ALL

    SELECT
        N'NOTA_ANTERIOR_GLOSA',
        COUNT_BIG(*)
    FROM #CuposGlosa
    WHERE ValorAceptado > 0
      AND CupoDisponible > 0
      AND FechaGlosa > FechaFactura

    UNION ALL

    SELECT
        N'CUPO_INSUFICIENTE',
        COUNT_BIG(*)
    FROM #CuposGlosa
    WHERE ValorAceptado > 0
      AND CupoDisponible > 0

    UNION ALL

    SELECT
        N'MULTIPLES_GLOSAS_ELEGIBLES',
        COUNT_BIG(*)
    FROM Elegibles
    WHERE Cantidad > 1
)
SELECT
    Caso,
    Candidatos,
    CASE
        WHEN Candidatos > 0 THEN 'DISPONIBLE'
        ELSE 'REQUIERE PREPARAR DATOS'
    END AS Estado
FROM Disponibilidad
ORDER BY Caso;

/* Caso 1: debe superar el análisis y asignarse automáticamente. */
;WITH Conteo AS
(
    SELECT
        FE,
        COUNT(*) AS Cantidad
    FROM #CuposGlosa
    WHERE ValorAceptado > 0
      AND CupoDisponible > 0
    GROUP BY FE
),
Candidato AS
(
    SELECT TOP (1)
        c.*
    FROM #CuposGlosa AS c
    INNER JOIN Conteo AS conteo
        ON conteo.FE = c.FE
       AND conteo.Cantidad = 1
    WHERE c.ValorAceptado > 0
      AND c.CupoDisponible > 0
    ORDER BY c.CupoDisponible DESC,
             c.FE
)
SELECT
    N'NC_VALIDA_ASIGNACION_UNICA' AS Caso,
    FE,
    Prefijo AS PREFIJO,
    Factura AS FACTURA,
    Aseguradora AS ASEGURADORA,
    'NC' AS [TIPO NOTA],
    CASE
        WHEN FechaGlosa > @FechaActual THEN FechaGlosa
        ELSE @FechaActual
    END AS [FECHA NOTA],
    'CERT-OK-' + @Sufijo AS [NUMERO NOTA],
    CAST(
        CASE
            WHEN CupoDisponible >= 100 THEN 100
            ELSE CupoDisponible
        END AS decimal(18, 2)) AS [VALOR NOTA],
    GlosaId,
    CupoDisponible,
    N'Debe superar el análisis.' AS ResultadoEsperado
FROM Candidato;

/* Caso 2: una nota débito no necesita glosa. */
SELECT TOP (1)
    N'NOTA_DEBITO_SIN_GLOSA' AS Caso,
    f.Id AS FE,
    f.Prefijo AS PREFIJO,
    f.Numero AS FACTURA,
    a.Descripcion AS ASEGURADORA,
    'ND' AS [TIPO NOTA],
    CASE
        WHEN f.FechaFactura > @FechaActual THEN f.FechaFactura
        ELSE @FechaActual
    END AS [FECHA NOTA],
    'CERT-ND-' + @Sufijo AS [NUMERO NOTA],
    CAST(1 AS decimal(18, 2)) AS [VALOR NOTA],
    N'Debe superar el análisis sin asociar una glosa.'
        AS ResultadoEsperado
FROM facturacion.Facturas AS f
INNER JOIN facturacion.Aseguradoras AS a
    ON a.Id = f.AseguradoraId
WHERE f.EstadoId NOT IN (3, 5)
ORDER BY f.Id;

/* Caso 3: factura sin glosa. */
SELECT TOP (1)
    N'FACTURA_SIN_GLOSA' AS Caso,
    f.Id AS FE,
    f.Prefijo AS PREFIJO,
    f.Numero AS FACTURA,
    a.Descripcion AS ASEGURADORA,
    'NC' AS [TIPO NOTA],
    CASE
        WHEN f.FechaFactura > @FechaActual THEN f.FechaFactura
        ELSE @FechaActual
    END AS [FECHA NOTA],
    'CERT-SIN-GLOSA-' + @Sufijo AS [NUMERO NOTA],
    CAST(1 AS decimal(18, 2)) AS [VALOR NOTA],
    N'FACTURA_SIN_GLOSA_PARA_NC' AS ResultadoEsperado
FROM facturacion.Facturas AS f
INNER JOIN facturacion.Aseguradoras AS a
    ON a.Id = f.AseguradoraId
WHERE f.EstadoId NOT IN (3, 5)
  AND NOT EXISTS
  (
      SELECT 1
      FROM facturacion.Glosas AS g
      WHERE g.FacturaId = f.Id
  )
ORDER BY f.Id;

/* Caso 4: existen glosas, pero ninguna tiene valor aceptado. */
SELECT TOP (1)
    N'GLOSA_SIN_VALOR_ACEPTADO' AS Caso,
    c.FE,
    c.Prefijo AS PREFIJO,
    c.Factura AS FACTURA,
    c.Aseguradora AS ASEGURADORA,
    'NC' AS [TIPO NOTA],
    CASE
        WHEN MAX(c.FechaGlosa) > @FechaActual
            THEN MAX(c.FechaGlosa)
        ELSE @FechaActual
    END AS [FECHA NOTA],
    'CERT-SIN-ACEPTADO-' + @Sufijo AS [NUMERO NOTA],
    CAST(1 AS decimal(18, 2)) AS [VALOR NOTA],
    N'FACTURA_SIN_GLOSA_ACEPTADA_PARA_NC' AS ResultadoEsperado
FROM #CuposGlosa AS c
GROUP BY
    c.FE,
    c.Prefijo,
    c.Factura,
    c.Aseguradora
HAVING MAX(c.ValorAceptado) = 0
ORDER BY c.FE;

/* Caso 5: la fecha de la NC antecede a todas las glosas aceptadas. */
;WITH CandidatoFecha AS
(
    SELECT
        FE,
        Prefijo,
        Factura,
        Aseguradora,
        MAX(FechaFactura) AS FechaFactura,
        MIN(FechaGlosa) AS PrimeraFechaGlosa,
        MIN(CupoDisponible) AS CupoMinimo
    FROM #CuposGlosa
    WHERE ValorAceptado > 0
      AND CupoDisponible > 0
    GROUP BY
        FE,
        Prefijo,
        Factura,
        Aseguradora
    HAVING MIN(FechaGlosa) > MAX(FechaFactura)
)
SELECT TOP (1)
    N'NOTA_ANTERIOR_GLOSA' AS Caso,
    FE,
    Prefijo AS PREFIJO,
    Factura AS FACTURA,
    Aseguradora AS ASEGURADORA,
    'NC' AS [TIPO NOTA],
    DATEADD(day, -1, PrimeraFechaGlosa) AS [FECHA NOTA],
    'CERT-FECHA-' + @Sufijo AS [NUMERO NOTA],
    CAST(
        CASE
            WHEN CupoMinimo >= 1 THEN 1
            ELSE CupoMinimo
        END AS decimal(18, 2)) AS [VALOR NOTA],
    PrimeraFechaGlosa,
    N'NOTA_ANTERIOR_GLOSA' AS ResultadoEsperado
FROM CandidatoFecha
ORDER BY PrimeraFechaGlosa DESC;

/* Caso 6: el valor de la NC supera el cupo aceptado. */
;WITH Conteo AS
(
    SELECT
        FE,
        COUNT(*) AS CantidadElegibles
    FROM #CuposGlosa
    WHERE ValorAceptado > 0
      AND CupoDisponible > 0
    GROUP BY FE
)
SELECT TOP (1)
    N'CUPO_INSUFICIENTE' AS Caso,
    c.FE,
    c.Prefijo AS PREFIJO,
    c.Factura AS FACTURA,
    c.Aseguradora AS ASEGURADORA,
    'NC' AS [TIPO NOTA],
    CASE
        WHEN c.FechaGlosa > @FechaActual THEN c.FechaGlosa
        ELSE @FechaActual
    END AS [FECHA NOTA],
    'CERT-CUPO-' + @Sufijo AS [NUMERO NOTA],
    CAST(c.CupoDisponible + 0.01 AS decimal(18, 2))
        AS [VALOR NOTA],
    c.GlosaId,
    c.CupoDisponible,
    N'GLOSA_SIN_CUPO_SUFICIENTE_NC' AS ResultadoEsperado
FROM #CuposGlosa AS c
INNER JOIN Conteo AS conteo
    ON conteo.FE = c.FE
   AND conteo.CantidadElegibles = 1
WHERE c.ValorAceptado > 0
  AND c.CupoDisponible > 0
ORDER BY c.CupoDisponible,
         c.FE;

/* Caso 7: más de una glosa podría respaldar la misma NC. */
;WITH Elegibles AS
(
    SELECT
        *,
        COUNT(*) OVER (PARTITION BY FE) AS CantidadElegibles,
        MIN(CupoDisponible) OVER (PARTITION BY FE) AS CupoMinimo,
        MAX(FechaGlosa) OVER (PARTITION BY FE) AS UltimaFechaGlosa
    FROM #CuposGlosa
    WHERE ValorAceptado > 0
      AND CupoDisponible > 0
)
SELECT TOP (1)
    N'MULTIPLES_GLOSAS_ELEGIBLES' AS Caso,
    FE,
    Prefijo AS PREFIJO,
    Factura AS FACTURA,
    Aseguradora AS ASEGURADORA,
    'NC' AS [TIPO NOTA],
    CASE
        WHEN UltimaFechaGlosa > @FechaActual THEN UltimaFechaGlosa
        ELSE @FechaActual
    END AS [FECHA NOTA],
    'CERT-AMBIGUA-' + @Sufijo AS [NUMERO NOTA],
    CAST(
        CASE
            WHEN CupoMinimo >= 1 THEN 1
            ELSE CupoMinimo
        END AS decimal(18, 2)) AS [VALOR NOTA],
    CantidadElegibles,
    N'GLOSA_AMBIGUA_PARA_NC' AS ResultadoEsperado
FROM Elegibles
WHERE CantidadElegibles > 1
ORDER BY FE;

/*
    Caso 8: dos filas son válidas individualmente, pero juntas
    exceden el cupo. Copie ambas filas en el mismo archivo.
*/
;WITH Conteo AS
(
    SELECT
        FE,
        COUNT(*) AS Cantidad
    FROM #CuposGlosa
    WHERE ValorAceptado > 0
      AND CupoDisponible >= 0.02
    GROUP BY FE
),
Candidato AS
(
    SELECT TOP (1)
        c.*
    FROM #CuposGlosa AS c
    INNER JOIN Conteo AS conteo
        ON conteo.FE = c.FE
       AND conteo.Cantidad = 1
    WHERE c.ValorAceptado > 0
      AND c.CupoDisponible >= 0.02
    ORDER BY c.CupoDisponible DESC,
             c.FE
),
Valores AS
(
    SELECT
        *,
        CAST(ROUND(CupoDisponible * 0.60, 2)
            AS decimal(18, 2)) AS ValorPrimera
    FROM Candidato
)
SELECT
    N'CUPO_ACUMULADO_MISMO_ARCHIVO' AS Caso,
    FE,
    Prefijo AS PREFIJO,
    Factura AS FACTURA,
    Aseguradora AS ASEGURADORA,
    'NC' AS [TIPO NOTA],
    CASE
        WHEN FechaGlosa > @FechaActual THEN FechaGlosa
        ELSE @FechaActual
    END AS [FECHA NOTA],
    'CERT-ACUM-1-' + @Sufijo AS [NUMERO NOTA],
    ValorPrimera AS [VALOR NOTA],
    CupoDisponible,
    N'La primera fila puede ser válida; la segunda debe reportar ' +
    N'GLOSA_SIN_CUPO_SUFICIENTE_NC.' AS ResultadoEsperado
FROM Valores

UNION ALL

SELECT
    N'CUPO_ACUMULADO_MISMO_ARCHIVO',
    FE,
    Prefijo,
    Factura,
    Aseguradora,
    'NC',
    CASE
        WHEN FechaGlosa > @FechaActual THEN FechaGlosa
        ELSE @FechaActual
    END,
    'CERT-ACUM-2-' + @Sufijo,
    CAST(CupoDisponible - ValorPrimera + 0.01
        AS decimal(18, 2)),
    CupoDisponible,
    N'La primera fila puede ser válida; la segunda debe reportar ' +
    N'GLOSA_SIN_CUPO_SUFICIENTE_NC.'
FROM Valores;

DROP TABLE IF EXISTS #CuposGlosa;
GO
