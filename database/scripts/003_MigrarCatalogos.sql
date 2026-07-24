USE [Seguimiento];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'Seguimiento'
BEGIN
    THROW 50001,
        'La migración debe ejecutarse en la base Seguimiento.',
        1;
END;
GO

/*
    ============================================================
    VALIDACIONES PREVIAS
    ============================================================
*/

IF SCHEMA_ID(N'facturacion') IS NULL
BEGIN
    THROW 50002,
        'No existe el esquema facturacion.',
        1;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM
    (
        SELECT
            CODIGO AS Codigo,
            CAST(DESCRIPCION AS nvarchar(500)) AS Texto
        FROM dbo.ASEGURADORA

        UNION ALL

        SELECT CODIGO, DESCRIPCION
        FROM dbo.ATENCION

        UNION ALL

        SELECT CODIGO, DESCRIPCION
        FROM dbo.COSTO

        UNION ALL

        SELECT CODIGO, DESCRIPCION
        FROM dbo.ESTADO

        UNION ALL

        SELECT CODIGO, NOMBRE
        FROM dbo.FACTURADOR

        UNION ALL

        SELECT Codigo, Descripcion
        FROM dbo.Tipo_Doc

        UNION ALL

        SELECT CODIGO, DESCRIPCION
        FROM dbo.T_MOV
    ) AS Catalogos
    WHERE Codigo <= 0
       OR NULLIF(LTRIM(RTRIM(Texto)), N'') IS NULL
)
BEGIN
    THROW 50003,
        'Existen códigos inválidos o descripciones vacías.',
        1;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM dbo.Tipo_Doc
    WHERE NULLIF(LTRIM(RTRIM(Sigla)), N'') IS NULL
)
BEGIN
    THROW 50004,
        'Existen tipos de documento sin sigla.',
        1;
END;
GO

IF EXISTS
(
    SELECT
        UPPER(LTRIM(RTRIM(Sigla)))
    FROM dbo.Tipo_Doc
    GROUP BY UPPER(LTRIM(RTRIM(Sigla)))
    HAVING COUNT_BIG(*) > 1
)
BEGIN
    THROW 50005,
        'Existen siglas duplicadas en Tipo_Doc.',
        1;
END;
GO

IF
(
    SELECT COUNT_BIG(*)
    FROM dbo.T_MOV
) <> 4
OR EXISTS
(
    SELECT 1
    FROM dbo.T_MOV
    WHERE CODIGO NOT IN (1, 2, 3, 4)
)
BEGIN
    THROW 50006,
        'La tabla T_MOV no contiene los cuatro códigos oficiales.',
        1;
END;
GO

/*
    ============================================================
    MIGRACIÓN TRANSACCIONAL E IDEMPOTENTE
    ============================================================
*/

BEGIN TRY
    BEGIN TRANSACTION;

    /*
        ASEGURADORAS
    */

    UPDATE destino
    SET destino.Descripcion =
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM facturacion.Aseguradoras AS destino
    INNER JOIN dbo.ASEGURADORA AS origen
        ON origen.CODIGO = destino.Id;

    INSERT INTO facturacion.Aseguradoras
    (
        Id,
        Descripcion
    )
    SELECT
        origen.CODIGO,
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM dbo.ASEGURADORA AS origen
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM facturacion.Aseguradoras AS destino
        WHERE destino.Id = origen.CODIGO
    );

    /*
        ATENCIONES
    */

    UPDATE destino
    SET destino.Descripcion =
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM facturacion.Atenciones AS destino
    INNER JOIN dbo.ATENCION AS origen
        ON origen.CODIGO = destino.Id;

    INSERT INTO facturacion.Atenciones
    (
        Id,
        Descripcion
    )
    SELECT
        origen.CODIGO,
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM dbo.ATENCION AS origen
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM facturacion.Atenciones AS destino
        WHERE destino.Id = origen.CODIGO
    );

    /*
        COSTOS
    */

    UPDATE destino
    SET destino.Descripcion =
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM facturacion.Costos AS destino
    INNER JOIN dbo.COSTO AS origen
        ON origen.CODIGO = destino.Id;

    INSERT INTO facturacion.Costos
    (
        Id,
        Descripcion
    )
    SELECT
        origen.CODIGO,
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM dbo.COSTO AS origen
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM facturacion.Costos AS destino
        WHERE destino.Id = origen.CODIGO
    );

    /*
        ESTADOS
    */

    UPDATE destino
    SET destino.Descripcion =
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM facturacion.Estados AS destino
    INNER JOIN dbo.ESTADO AS origen
        ON origen.CODIGO = destino.Id;

    INSERT INTO facturacion.Estados
    (
        Id,
        Descripcion
    )
    SELECT
        origen.CODIGO,
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM dbo.ESTADO AS origen
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM facturacion.Estados AS destino
        WHERE destino.Id = origen.CODIGO
    );

    /*
        FACTURADORES
    */

    UPDATE destino
    SET destino.Nombre =
        LTRIM(RTRIM(origen.NOMBRE))
    FROM facturacion.Facturadores AS destino
    INNER JOIN dbo.FACTURADOR AS origen
        ON origen.CODIGO = destino.Id;

    INSERT INTO facturacion.Facturadores
    (
        Id,
        Nombre
    )
    SELECT
        origen.CODIGO,
        LTRIM(RTRIM(origen.NOMBRE))
    FROM dbo.FACTURADOR AS origen
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM facturacion.Facturadores AS destino
        WHERE destino.Id = origen.CODIGO
    );

    /*
        TIPOS DE DOCUMENTO
    */

    UPDATE destino
    SET
        destino.Descripcion =
            LTRIM(RTRIM(origen.Descripcion)),
        destino.Sigla =
            UPPER(LTRIM(RTRIM(origen.Sigla)))
    FROM facturacion.TiposDocumento AS destino
    INNER JOIN dbo.Tipo_Doc AS origen
        ON origen.Codigo = destino.Id;

    INSERT INTO facturacion.TiposDocumento
    (
        Id,
        Descripcion,
        Sigla
    )
    SELECT
        origen.Codigo,
        LTRIM(RTRIM(origen.Descripcion)),
        UPPER(LTRIM(RTRIM(origen.Sigla)))
    FROM dbo.Tipo_Doc AS origen
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM facturacion.TiposDocumento AS destino
        WHERE destino.Id = origen.Codigo
    );

    /*
        TIPOS DE MOVIMIENTO
    */

    UPDATE destino
    SET destino.Descripcion =
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM facturacion.TiposMovimiento AS destino
    INNER JOIN dbo.T_MOV AS origen
        ON origen.CODIGO = destino.Id;

    INSERT INTO facturacion.TiposMovimiento
    (
        Id,
        Descripcion
    )
    SELECT
        origen.CODIGO,
        LTRIM(RTRIM(origen.DESCRIPCION))
    FROM dbo.T_MOV AS origen
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM facturacion.TiposMovimiento AS destino
        WHERE destino.Id = origen.CODIGO
    );

    COMMIT TRANSACTION;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0
    BEGIN
        ROLLBACK TRANSACTION;
    END;

    THROW;
END CATCH;
GO

/*
    ============================================================
    VERIFICACIÓN POSTERIOR
    ============================================================
*/

SELECT
    N'Aseguradoras' AS Catalogo,
    (SELECT COUNT_BIG(*) FROM dbo.ASEGURADORA)
        AS RegistrosOrigen,
    (SELECT COUNT_BIG(*) FROM facturacion.Aseguradoras)
        AS RegistrosDestino

UNION ALL

SELECT
    N'Atenciones',
    (SELECT COUNT_BIG(*) FROM dbo.ATENCION),
    (SELECT COUNT_BIG(*) FROM facturacion.Atenciones)

UNION ALL

SELECT
    N'Costos',
    (SELECT COUNT_BIG(*) FROM dbo.COSTO),
    (SELECT COUNT_BIG(*) FROM facturacion.Costos)

UNION ALL

SELECT
    N'Estados',
    (SELECT COUNT_BIG(*) FROM dbo.ESTADO),
    (SELECT COUNT_BIG(*) FROM facturacion.Estados)

UNION ALL

SELECT
    N'Facturadores',
    (SELECT COUNT_BIG(*) FROM dbo.FACTURADOR),
    (SELECT COUNT_BIG(*) FROM facturacion.Facturadores)

UNION ALL

SELECT
    N'TiposDocumento',
    (SELECT COUNT_BIG(*) FROM dbo.Tipo_Doc),
    (SELECT COUNT_BIG(*) FROM facturacion.TiposDocumento)

UNION ALL

SELECT
    N'TiposMovimiento',
    (SELECT COUNT_BIG(*) FROM dbo.T_MOV),
    (SELECT COUNT_BIG(*) FROM facturacion.TiposMovimiento);
GO

SELECT
    Id,
    Descripcion
FROM facturacion.TiposMovimiento
ORDER BY Id;
GO

SELECT
    (SELECT COUNT_BIG(*)
     FROM facturacion.Facturas) AS Facturas,

    (SELECT COUNT_BIG(*)
     FROM facturacion.Movimientos) AS Movimientos;
GO

PRINT N'Migración de catálogos finalizada correctamente.';
GO