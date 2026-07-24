USE [Seguimiento];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'Seguimiento'
BEGIN
    THROW 50001,
        'El diagnóstico debe ejecutarse en la base Seguimiento.',
        1;
END;
GO

/*
    ============================================================
    1. INFORMACIÓN GENERAL
    ============================================================
*/

SELECT
    DB_NAME() AS BaseActual,
    @@SERVERNAME AS ServidorActual,
    SYSDATETIMEOFFSET() AS FechaDiagnostico;
GO

SELECT
    N'dbo.SeguimientoFacturacion' AS Tabla,
    COUNT_BIG(*) AS Registros
FROM dbo.SeguimientoFacturacion

UNION ALL

SELECT N'dbo.Movimientos', COUNT_BIG(*)
FROM dbo.Movimientos

UNION ALL

SELECT N'dbo.Tipo_Doc', COUNT_BIG(*)
FROM dbo.Tipo_Doc

UNION ALL

SELECT N'dbo.T_MOV', COUNT_BIG(*)
FROM dbo.T_MOV

UNION ALL

SELECT N'dbo.ATENCION', COUNT_BIG(*)
FROM dbo.ATENCION

UNION ALL

SELECT N'dbo.COSTO', COUNT_BIG(*)
FROM dbo.COSTO

UNION ALL

SELECT N'dbo.ESTADO', COUNT_BIG(*)
FROM dbo.ESTADO

UNION ALL

SELECT N'dbo.FACTURADOR', COUNT_BIG(*)
FROM dbo.FACTURADOR

UNION ALL

SELECT N'dbo.ASEGURADORA', COUNT_BIG(*)
FROM dbo.ASEGURADORA;
GO

/*
    ============================================================
    2. CALIDAD GENERAL DE LOS CATÁLOGOS
    ============================================================
*/

;WITH Catalogos AS
(
    SELECT
        N'ASEGURADORA' AS Catalogo,
        CODIGO AS Codigo,
        CAST(DESCRIPCION AS nvarchar(500)) AS Texto
    FROM dbo.ASEGURADORA

    UNION ALL

    SELECT
        N'ATENCION',
        CODIGO,
        CAST(DESCRIPCION AS nvarchar(500))
    FROM dbo.ATENCION

    UNION ALL

    SELECT
        N'COSTO',
        CODIGO,
        CAST(DESCRIPCION AS nvarchar(500))
    FROM dbo.COSTO

    UNION ALL

    SELECT
        N'ESTADO',
        CODIGO,
        CAST(DESCRIPCION AS nvarchar(500))
    FROM dbo.ESTADO

    UNION ALL

    SELECT
        N'FACTURADOR',
        CODIGO,
        CAST(NOMBRE AS nvarchar(500))
    FROM dbo.FACTURADOR

    UNION ALL

    SELECT
        N'TIPO_DOCUMENTO',
        Codigo,
        CAST(Descripcion AS nvarchar(500))
    FROM dbo.Tipo_Doc

    UNION ALL

    SELECT
        N'TIPO_MOVIMIENTO',
        CODIGO,
        CAST(DESCRIPCION AS nvarchar(500))
    FROM dbo.T_MOV
)
SELECT
    Catalogo,
    COUNT_BIG(*) AS TotalRegistros,

    SUM(
        CONVERT(
            bigint,
            CASE
                WHEN Codigo <= 0 THEN 1
                ELSE 0
            END)) AS CodigosInvalidos,

    SUM(
        CONVERT(
            bigint,
            CASE
                WHEN NULLIF(LTRIM(RTRIM(Texto)), N'') IS NULL
                    THEN 1
                ELSE 0
            END)) AS TextosVacios
FROM Catalogos
GROUP BY Catalogo
ORDER BY Catalogo;
GO

/*
    ============================================================
    3. DESCRIPCIONES DUPLICADAS EN CATÁLOGOS
    ============================================================
*/

;WITH Catalogos AS
(
    SELECT
        N'ASEGURADORA' AS Catalogo,
        CODIGO AS Codigo,
        CAST(DESCRIPCION AS nvarchar(500)) AS Texto
    FROM dbo.ASEGURADORA

    UNION ALL

    SELECT N'ATENCION', CODIGO, DESCRIPCION
    FROM dbo.ATENCION

    UNION ALL

    SELECT N'COSTO', CODIGO, DESCRIPCION
    FROM dbo.COSTO

    UNION ALL

    SELECT N'ESTADO', CODIGO, DESCRIPCION
    FROM dbo.ESTADO

    UNION ALL

    SELECT N'FACTURADOR', CODIGO, NOMBRE
    FROM dbo.FACTURADOR

    UNION ALL

    SELECT N'TIPO_DOCUMENTO', Codigo, Descripcion
    FROM dbo.Tipo_Doc

    UNION ALL

    SELECT N'TIPO_MOVIMIENTO', CODIGO, DESCRIPCION
    FROM dbo.T_MOV
),
Normalizados AS
(
    SELECT
        Catalogo,
        Codigo,
        UPPER(NULLIF(LTRIM(RTRIM(Texto)), N'')) AS TextoNormalizado
    FROM Catalogos
)
SELECT
    Catalogo,
    TextoNormalizado,
    COUNT_BIG(*) AS Cantidad,
    MIN(Codigo) AS CodigoMinimo,
    MAX(Codigo) AS CodigoMaximo
FROM Normalizados
WHERE TextoNormalizado IS NOT NULL
GROUP BY
    Catalogo,
    TextoNormalizado
HAVING COUNT_BIG(*) > 1
ORDER BY
    Catalogo,
    TextoNormalizado;
GO

/*
    ============================================================
    4. SIGLAS DUPLICADAS EN TIPOS DE DOCUMENTO
    ============================================================
*/

SELECT
    UPPER(NULLIF(LTRIM(RTRIM(Sigla)), N'')) AS SiglaNormalizada,
    COUNT_BIG(*) AS Cantidad,
    MIN(Codigo) AS CodigoMinimo,
    MAX(Codigo) AS CodigoMaximo
FROM dbo.Tipo_Doc
WHERE NULLIF(LTRIM(RTRIM(Sigla)), N'') IS NOT NULL
GROUP BY
    UPPER(NULLIF(LTRIM(RTRIM(Sigla)), N''))
HAVING COUNT_BIG(*) > 1
ORDER BY SiglaNormalizada;
GO

/*
    ============================================================
    5. CALIDAD GENERAL DE FACTURAS HEREDADAS
    ============================================================
*/

SELECT
    COUNT_BIG(*) AS TotalFacturas,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.FE)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS FeVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.PREFIJO)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS PrefijoVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.FACTURA)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS NumeroFacturaVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN f.FECHA_FRA IS NULL THEN 1
            ELSE 0
        END)) AS FechaFacturaNula,

    SUM(CONVERT(bigint,
        CASE
            WHEN f.VALOR IS NULL THEN 1
            ELSE 0
        END)) AS ValorNulo,

    SUM(CONVERT(bigint,
        CASE
            WHEN f.VALOR <= 0 THEN 1
            ELSE 0
        END)) AS ValorMenorOIgualCero,

    SUM(CONVERT(bigint,
        CASE
            WHEN f.VALOR IS NOT NULL
             AND ABS(
                    f.VALOR -
                    CONVERT(
                        float,
                        TRY_CONVERT(decimal(18, 2), f.VALOR)))
                    > 0.000001
                THEN 1
            ELSE 0
        END)) AS ValoresConMasDeDosDecimales,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.ASEGURADORA)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS AseguradoraVacia,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.TIPO_DTO)), '') IS NULL
             AND NULLIF(LTRIM(RTRIM(f.SIGLA)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS TipoDocumentoVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.NUMERO_DTO)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS NumeroDocumentoVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.NOMBRE_COMPLETO)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS NombreCompletoVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.ATENCION)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS AtencionVacia,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.COSTO)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS CostoVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.ESTADO_DE_DTO)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS EstadoVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.FACTURADOR)), '') IS NULL
                THEN 1
            ELSE 0
        END)) AS FacturadorVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN f.FECHA_DE_RADICACION IS NOT NULL
             AND f.FECHA_FRA IS NOT NULL
             AND CONVERT(date, f.FECHA_DE_RADICACION) < f.FECHA_FRA
                THEN 1
            ELSE 0
        END)) AS RadicacionAnteriorFactura,

    SUM(CONVERT(bigint,
        CASE
            WHEN fechaAdmision.TextoFecha IS NOT NULL
             AND fechaAdmision.FechaConvertida IS NULL
                THEN 1
            ELSE 0
        END)) AS FechasAdmisionInvalidas,

    SUM(CONVERT(bigint,
        CASE
            WHEN fechaAdmision.FechaConvertida IS NOT NULL
             AND f.FECHA_FRA IS NOT NULL
             AND fechaAdmision.FechaConvertida > f.FECHA_FRA
                THEN 1
            ELSE 0
        END)) AS AdmisionPosteriorFactura,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(f.FE)), '') IS NOT NULL
             AND NULLIF(LTRIM(RTRIM(f.PREFIJO)), '') IS NOT NULL
             AND NULLIF(LTRIM(RTRIM(f.FACTURA)), '') IS NOT NULL
             AND UPPER(LTRIM(RTRIM(f.FE))) <>
                 UPPER(
                     LTRIM(RTRIM(f.PREFIJO)) +
                     LTRIM(RTRIM(f.FACTURA)))
                THEN 1
            ELSE 0
        END)) AS FeDiferentePrefijoMasFactura
FROM dbo.SeguimientoFacturacion AS f
OUTER APPLY
(
    SELECT
        NULLIF(
            LTRIM(RTRIM(f.FECHA_ADMISION)),
            '') AS TextoFecha
) AS textoAdmision
OUTER APPLY
(
    SELECT
        textoAdmision.TextoFecha,
        COALESCE(
            TRY_CONVERT(date, textoAdmision.TextoFecha, 23),
            TRY_CONVERT(date, textoAdmision.TextoFecha, 103),
            TRY_CONVERT(date, textoAdmision.TextoFecha, 101),
            TRY_CONVERT(date, textoAdmision.TextoFecha, 112),
            TRY_CONVERT(date, textoAdmision.TextoFecha)
        ) AS FechaConvertida
) AS fechaAdmision;
GO

/*
    ============================================================
    6. FE DUPLICADOS
    ============================================================
*/

;WITH Agrupados AS
(
    SELECT
        UPPER(LTRIM(RTRIM(FE))) AS FeNormalizado,
        COUNT_BIG(*) AS Cantidad
    FROM dbo.SeguimientoFacturacion
    WHERE NULLIF(LTRIM(RTRIM(FE)), '') IS NOT NULL
    GROUP BY UPPER(LTRIM(RTRIM(FE)))
    HAVING COUNT_BIG(*) > 1
)
SELECT
    COUNT_BIG(*) AS GruposFeDuplicados,
    COALESCE(SUM(Cantidad - 1), 0) AS RegistrosDuplicadosAdicionales,
    COALESCE(MAX(Cantidad), 0) AS MaximaRepeticion
FROM Agrupados;
GO

/*
    ============================================================
    7. VALORES DE CATÁLOGO SIN CORRESPONDENCIA
    ============================================================
*/

;WITH NoMapeados AS
(
    SELECT
        N'ASEGURADORA' AS Catalogo,
        LTRIM(RTRIM(f.ASEGURADORA)) AS ValorHeredado,
        COUNT_BIG(*) AS Cantidad
    FROM dbo.SeguimientoFacturacion AS f
    WHERE NULLIF(LTRIM(RTRIM(f.ASEGURADORA)), '') IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ASEGURADORA AS catalogo
          WHERE UPPER(LTRIM(RTRIM(catalogo.DESCRIPCION))) =
                UPPER(LTRIM(RTRIM(f.ASEGURADORA)))
      )
    GROUP BY LTRIM(RTRIM(f.ASEGURADORA))

    UNION ALL

    SELECT
        N'ATENCION',
        LTRIM(RTRIM(f.ATENCION)),
        COUNT_BIG(*)
    FROM dbo.SeguimientoFacturacion AS f
    WHERE NULLIF(LTRIM(RTRIM(f.ATENCION)), '') IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ATENCION AS catalogo
          WHERE UPPER(LTRIM(RTRIM(catalogo.DESCRIPCION))) =
                UPPER(LTRIM(RTRIM(f.ATENCION)))
      )
    GROUP BY LTRIM(RTRIM(f.ATENCION))

    UNION ALL

    SELECT
        N'COSTO',
        LTRIM(RTRIM(f.COSTO)),
        COUNT_BIG(*)
    FROM dbo.SeguimientoFacturacion AS f
    WHERE NULLIF(LTRIM(RTRIM(f.COSTO)), '') IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.COSTO AS catalogo
          WHERE UPPER(LTRIM(RTRIM(catalogo.DESCRIPCION))) =
                UPPER(LTRIM(RTRIM(f.COSTO)))
      )
    GROUP BY LTRIM(RTRIM(f.COSTO))

    UNION ALL

    SELECT
        N'ESTADO',
        LTRIM(RTRIM(f.ESTADO_DE_DTO)),
        COUNT_BIG(*)
    FROM dbo.SeguimientoFacturacion AS f
    WHERE NULLIF(LTRIM(RTRIM(f.ESTADO_DE_DTO)), '') IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.ESTADO AS catalogo
          WHERE UPPER(LTRIM(RTRIM(catalogo.DESCRIPCION))) =
                UPPER(LTRIM(RTRIM(f.ESTADO_DE_DTO)))
      )
    GROUP BY LTRIM(RTRIM(f.ESTADO_DE_DTO))

    UNION ALL

    SELECT
        N'FACTURADOR',
        LTRIM(RTRIM(f.FACTURADOR)),
        COUNT_BIG(*)
    FROM dbo.SeguimientoFacturacion AS f
    WHERE NULLIF(LTRIM(RTRIM(f.FACTURADOR)), '') IS NOT NULL
      AND NOT EXISTS
      (
          SELECT 1
          FROM dbo.FACTURADOR AS catalogo
          WHERE UPPER(LTRIM(RTRIM(catalogo.NOMBRE))) =
                UPPER(LTRIM(RTRIM(f.FACTURADOR)))
      )
    GROUP BY LTRIM(RTRIM(f.FACTURADOR))

    UNION ALL

    SELECT
        N'TIPO_DOCUMENTO',
        CONCAT(
            N'TIPO_DTO=',
            COALESCE(
                NULLIF(LTRIM(RTRIM(f.TIPO_DTO)), ''),
                N'<VACIO>'),
            N'; SIGLA=',
            COALESCE(
                NULLIF(LTRIM(RTRIM(f.SIGLA)), ''),
                N'<VACIO>')),
        COUNT_BIG(*)
    FROM dbo.SeguimientoFacturacion AS f
    WHERE
        (
            NULLIF(LTRIM(RTRIM(f.TIPO_DTO)), '') IS NOT NULL
            OR NULLIF(LTRIM(RTRIM(f.SIGLA)), '') IS NOT NULL
        )
        AND NOT EXISTS
        (
            SELECT 1
            FROM dbo.Tipo_Doc AS catalogo
            WHERE
                UPPER(LTRIM(RTRIM(catalogo.Descripcion))) =
                UPPER(LTRIM(RTRIM(f.TIPO_DTO)))
                OR
                UPPER(LTRIM(RTRIM(catalogo.Sigla))) =
                UPPER(LTRIM(RTRIM(f.SIGLA)))
                OR
                UPPER(LTRIM(RTRIM(catalogo.Sigla))) =
                UPPER(LTRIM(RTRIM(f.TIPO_DTO)))
        )
    GROUP BY
        CONCAT(
            N'TIPO_DTO=',
            COALESCE(
                NULLIF(LTRIM(RTRIM(f.TIPO_DTO)), ''),
                N'<VACIO>'),
            N'; SIGLA=',
            COALESCE(
                NULLIF(LTRIM(RTRIM(f.SIGLA)), ''),
                N'<VACIO>'))
)
SELECT
    Catalogo,
    ValorHeredado,
    Cantidad
FROM NoMapeados
ORDER BY
    Catalogo,
    Cantidad DESC,
    ValorHeredado;
GO

/*
    ============================================================
    8. CALIDAD GENERAL DE MOVIMIENTOS
    ============================================================
*/

SELECT
    COUNT_BIG(*) AS TotalMovimientos,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(m.FE)), N'') IS NULL
                THEN 1
            ELSE 0
        END)) AS FeVacio,

    SUM(CONVERT(bigint,
        CASE
            WHEN NULLIF(LTRIM(RTRIM(m.FE)), N'') IS NOT NULL
             AND facturaAsociada.Existe IS NULL
                THEN 1
            ELSE 0
        END)) AS MovimientosSinFactura,

    SUM(CONVERT(bigint,
        CASE
            WHEN fechaMovimiento.TextoFecha IS NULL
              OR fechaMovimiento.FechaConvertida IS NULL
                THEN 1
            ELSE 0
        END)) AS FechasInvalidas,

    SUM(CONVERT(bigint,
        CASE
            WHEN TRY_CONVERT(
                    int,
                    NULLIF(LTRIM(RTRIM(m.ANO)), N'')) IS NULL
                THEN 1
            ELSE 0
        END)) AS AniosInvalidos,

    SUM(CONVERT(bigint,
        CASE
            WHEN fechaMovimiento.FechaConvertida IS NOT NULL
             AND TRY_CONVERT(
                    int,
                    NULLIF(LTRIM(RTRIM(m.ANO)), N'')) IS NOT NULL
             AND YEAR(fechaMovimiento.FechaConvertida) <>
                 TRY_CONVERT(
                     int,
                     NULLIF(LTRIM(RTRIM(m.ANO)), N''))
                THEN 1
            ELSE 0
        END)) AS AnioDiferenteFecha,

    SUM(CONVERT(bigint,
        CASE
            WHEN m.VALOR IS NULL THEN 1
            ELSE 0
        END)) AS ValorNulo,

    SUM(CONVERT(bigint,
        CASE
            WHEN m.VALOR < 0 THEN 1
            ELSE 0
        END)) AS ValorNegativo,

    SUM(CONVERT(bigint,
        CASE
            WHEN m.VALOR IS NOT NULL
             AND ABS(
                    m.VALOR -
                    CONVERT(
                        float,
                        TRY_CONVERT(decimal(18, 2), m.VALOR)))
                    > 0.000001
                THEN 1
            ELSE 0
        END)) AS ValoresConMasDeDosDecimales,

    SUM(CONVERT(bigint,
        CASE
            WHEN tipoMovimiento.Codigo IS NULL
                THEN 1
            ELSE 0
        END)) AS TiposMovimientoInvalidos,

    SUM(CONVERT(bigint,
        CASE
            WHEN tipoMovimiento.Codigo = 1
             AND (m.N_CREDITO IS NULL OR m.N_CREDITO <= 0)
                THEN 1
            ELSE 0
        END)) AS NotasCreditoSinNumeroValido,

    SUM(CONVERT(bigint,
        CASE
            WHEN tipoMovimiento.Codigo <> 1
             AND m.N_CREDITO = 0
                THEN 1
            ELSE 0
        END)) AS OtrosTiposConNumeroCero,

    SUM(CONVERT(bigint,
        CASE
            WHEN tipoMovimiento.Codigo <> 1
             AND m.N_CREDITO > 0
                THEN 1
            ELSE 0
        END)) AS OtrosTiposConNumeroPositivo

FROM dbo.Movimientos AS m

OUTER APPLY
(
    SELECT TOP (1)
        CONVERT(bit, 1) AS Existe
    FROM dbo.SeguimientoFacturacion AS factura
    WHERE UPPER(LTRIM(RTRIM(factura.FE))) =
          UPPER(LTRIM(RTRIM(m.FE)))
) AS facturaAsociada

OUTER APPLY
(
    SELECT
        NULLIF(
            LTRIM(RTRIM(m.FECHA)),
            N'') AS TextoFecha
) AS textoMovimiento

OUTER APPLY
(
    SELECT
        textoMovimiento.TextoFecha,
        COALESCE(
            TRY_CONVERT(date, textoMovimiento.TextoFecha, 23),
            TRY_CONVERT(date, textoMovimiento.TextoFecha, 103),
            TRY_CONVERT(date, textoMovimiento.TextoFecha, 101),
            TRY_CONVERT(date, textoMovimiento.TextoFecha, 112),
            TRY_CONVERT(date, textoMovimiento.TextoFecha)
        ) AS FechaConvertida
) AS fechaMovimiento

OUTER APPLY
(
    SELECT TOP (1)
        catalogo.CODIGO AS Codigo
    FROM dbo.T_MOV AS catalogo
    WHERE
        catalogo.CODIGO =
            TRY_CONVERT(
                int,
                NULLIF(LTRIM(RTRIM(m.TIPO)), N''))
        OR
        UPPER(LTRIM(RTRIM(catalogo.DESCRIPCION))) =
            UPPER(LTRIM(RTRIM(m.TIPO)))
    ORDER BY
        CASE
            WHEN catalogo.CODIGO =
                 TRY_CONVERT(
                     int,
                     NULLIF(LTRIM(RTRIM(m.TIPO)), N''))
                THEN 0
            ELSE 1
        END
) AS tipoMovimiento;
GO
/*
    ============================================================
    9. DISTRIBUCIÓN DE TIPOS DE MOVIMIENTO
    ============================================================
*/

SELECT
    LTRIM(RTRIM(m.TIPO)) AS TipoHeredado,
    tipoMovimiento.Codigo AS CodigoInterpretado,
    tipoMovimiento.Descripcion AS DescripcionInterpretada,
    COUNT_BIG(*) AS Cantidad
FROM dbo.Movimientos AS m
OUTER APPLY
(
    SELECT TOP (1)
        catalogo.CODIGO AS Codigo,
        catalogo.DESCRIPCION AS Descripcion
    FROM dbo.T_MOV AS catalogo
    WHERE
        catalogo.CODIGO =
            TRY_CONVERT(
                int,
                NULLIF(LTRIM(RTRIM(m.TIPO)), N''))
        OR
        UPPER(LTRIM(RTRIM(catalogo.DESCRIPCION))) =
            UPPER(LTRIM(RTRIM(m.TIPO)))
    ORDER BY
        CASE
            WHEN catalogo.CODIGO =
                 TRY_CONVERT(
                     int,
                     NULLIF(LTRIM(RTRIM(m.TIPO)), N''))
                THEN 0
            ELSE 1
        END
) AS tipoMovimiento
GROUP BY
    LTRIM(RTRIM(m.TIPO)),
    tipoMovimiento.Codigo,
    tipoMovimiento.Descripcion
ORDER BY
    tipoMovimiento.Codigo,
    TipoHeredado;
GO

/*
    ============================================================
    10. FECHAS DE MOVIMIENTO NO CONVERTIBLES
    ============================================================
*/

SELECT TOP (50)
    LTRIM(RTRIM(m.FECHA)) AS FechaOriginal,
    COUNT_BIG(*) AS Cantidad
FROM dbo.Movimientos AS m
OUTER APPLY
(
    SELECT
        COALESCE(
            TRY_CONVERT(
                date,
                NULLIF(LTRIM(RTRIM(m.FECHA)), N''),
                23),
            TRY_CONVERT(
                date,
                NULLIF(LTRIM(RTRIM(m.FECHA)), N''),
                103),
            TRY_CONVERT(
                date,
                NULLIF(LTRIM(RTRIM(m.FECHA)), N''),
                101),
            TRY_CONVERT(
                date,
                NULLIF(LTRIM(RTRIM(m.FECHA)), N''),
                112),
            TRY_CONVERT(
                date,
                NULLIF(LTRIM(RTRIM(m.FECHA)), N''))
        ) AS FechaConvertida
) AS fechaMovimiento
WHERE NULLIF(LTRIM(RTRIM(m.FECHA)), N'') IS NOT NULL
  AND fechaMovimiento.FechaConvertida IS NULL
GROUP BY LTRIM(RTRIM(m.FECHA))
ORDER BY
    Cantidad DESC,
    FechaOriginal;
GO

/*
    ============================================================
    11. POSIBLES MOVIMIENTOS DUPLICADOS
    ============================================================
*/

;WITH Agrupados AS
(
    SELECT
        UPPER(LTRIM(RTRIM(FE))) AS FeNormalizado,
        LTRIM(RTRIM(FECHA)) AS FechaNormalizada,
        UPPER(LTRIM(RTRIM(TIPO))) AS TipoNormalizado,
        N_CREDITO AS NumeroNotaCredito,
        TRY_CONVERT(decimal(18, 2), VALOR) AS ValorNormalizado,
        COUNT_BIG(*) AS Cantidad
    FROM dbo.Movimientos
    GROUP BY
        UPPER(LTRIM(RTRIM(FE))),
        LTRIM(RTRIM(FECHA)),
        UPPER(LTRIM(RTRIM(TIPO))),
        N_CREDITO,
        TRY_CONVERT(decimal(18, 2), VALOR)
    HAVING COUNT_BIG(*) > 1
)
SELECT
    COUNT_BIG(*) AS GruposDuplicados,
    COALESCE(SUM(Cantidad - 1), 0)
        AS RegistrosDuplicadosAdicionales,
    COALESCE(MAX(Cantidad), 0)
        AS MaximaRepeticion
FROM Agrupados;
GO

/*
    ============================================================
    12. ESTADO ACTUAL DE LAS TABLAS NORMALIZADAS
    ============================================================
*/

SELECT
    (SELECT COUNT_BIG(*)
     FROM facturacion.Aseguradoras) AS Aseguradoras,

    (SELECT COUNT_BIG(*)
     FROM facturacion.Atenciones) AS Atenciones,

    (SELECT COUNT_BIG(*)
     FROM facturacion.Costos) AS Costos,

    (SELECT COUNT_BIG(*)
     FROM facturacion.Estados) AS Estados,

    (SELECT COUNT_BIG(*)
     FROM facturacion.Facturadores) AS Facturadores,

    (SELECT COUNT_BIG(*)
     FROM facturacion.TiposDocumento) AS TiposDocumento,

    (SELECT COUNT_BIG(*)
     FROM facturacion.TiposMovimiento) AS TiposMovimiento,

    (SELECT COUNT_BIG(*)
     FROM facturacion.Facturas) AS Facturas,

    (SELECT COUNT_BIG(*)
     FROM facturacion.Movimientos) AS Movimientos;
GO

PRINT N'Diagnóstico finalizado sin modificar datos.';
GO