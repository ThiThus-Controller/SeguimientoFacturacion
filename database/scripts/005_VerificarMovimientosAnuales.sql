USE [Seguimiento];
GO

SET NOCOUNT ON;
SET XACT_ABORT ON;
GO

IF DB_NAME() <> N'Seguimiento'
BEGIN
    THROW 50001,
        'La verificación debe ejecutarse en la base Seguimiento.',
        1;
END;
GO

/*
    ============================================================
    1. VALIDAR HISTORIAL DE MIGRACIONES
    ============================================================
*/

IF OBJECT_ID(
       N'dbo.__SeguimientoFacturacionMigrationsHistory',
       N'U') IS NULL
BEGIN
    THROW 50002,
        'No existe la tabla de historial de migraciones.',
        1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM dbo.__SeguimientoFacturacionMigrationsHistory
    WHERE MigrationId LIKE
          N'%PermitirMovimientosAnualesSinFecha'
)
BEGIN
    THROW 50003,
        'La migración de movimientos anuales no está registrada.',
        1;
END;
GO

/*
    ============================================================
    2. VALIDAR COLUMNA ANIO
    ============================================================
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns AS columna
    INNER JOIN sys.tables AS tabla
        ON tabla.object_id = columna.object_id
    INNER JOIN sys.schemas AS esquema
        ON esquema.schema_id = tabla.schema_id
    INNER JOIN sys.types AS tipo
        ON tipo.user_type_id = columna.user_type_id
    WHERE esquema.name = N'facturacion'
      AND tabla.name = N'Movimientos'
      AND columna.name = N'Anio'
      AND tipo.name = N'int'
      AND columna.is_nullable = 0
)
BEGIN
    THROW 50004,
        'La columna Anio no existe, no es int o permite NULL.',
        1;
END;
GO

IF EXISTS
(
    SELECT 1
    FROM sys.columns AS columna
    INNER JOIN sys.tables AS tabla
        ON tabla.object_id = columna.object_id
    INNER JOIN sys.schemas AS esquema
        ON esquema.schema_id = tabla.schema_id
    WHERE esquema.name = N'facturacion'
      AND tabla.name = N'Movimientos'
      AND columna.name = N'Anio'
      AND columna.default_object_id <> 0
)
BEGIN
    THROW 50005,
        'La columna Anio conserva un valor predeterminado no permitido.',
        1;
END;
GO

/*
    ============================================================
    3. VALIDAR COLUMNA FECHA
    ============================================================
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.columns AS columna
    INNER JOIN sys.tables AS tabla
        ON tabla.object_id = columna.object_id
    INNER JOIN sys.schemas AS esquema
        ON esquema.schema_id = tabla.schema_id
    INNER JOIN sys.types AS tipo
        ON tipo.user_type_id = columna.user_type_id
    WHERE esquema.name = N'facturacion'
      AND tabla.name = N'Movimientos'
      AND columna.name = N'Fecha'
      AND tipo.name = N'date'
      AND columna.is_nullable = 1
)
BEGIN
    THROW 50006,
        'La columna Fecha no existe, no es date o no permite NULL.',
        1;
END;
GO

/*
    ============================================================
    4. VALIDAR RESTRICCIÓN DEL AÑO
    ============================================================
*/

IF NOT EXISTS
(
    SELECT 1
    FROM sys.check_constraints
    WHERE parent_object_id =
          OBJECT_ID(N'facturacion.Movimientos')
      AND name = N'CK_Movimientos_Anio'
      AND is_disabled = 0
)
BEGIN
    THROW 50007,
        'No existe la restricción CK_Movimientos_Anio.',
        1;
END;
GO

/*
    ============================================================
    5. VALIDAR ÍNDICE NUEVO
    ============================================================
*/

IF EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id =
          OBJECT_ID(N'facturacion.Movimientos')
      AND name = N'IX_Movimientos_FacturaId_Fecha'
)
BEGIN
    THROW 50008,
        'El índice anterior de movimientos todavía existe.',
        1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes
    WHERE object_id =
          OBJECT_ID(N'facturacion.Movimientos')
      AND name =
          N'IX_Movimientos_FacturaId_Anio_Fecha'
      AND is_disabled = 0
)
BEGIN
    THROW 50009,
        'No existe el nuevo índice de movimientos.',
        1;
END;
GO

IF NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS indice
    INNER JOIN sys.index_columns AS columnaIndice
        ON columnaIndice.object_id = indice.object_id
       AND columnaIndice.index_id = indice.index_id
    INNER JOIN sys.columns AS columna
        ON columna.object_id = columnaIndice.object_id
       AND columna.column_id = columnaIndice.column_id
    WHERE indice.object_id =
          OBJECT_ID(N'facturacion.Movimientos')
      AND indice.name =
          N'IX_Movimientos_FacturaId_Anio_Fecha'
      AND columnaIndice.key_ordinal = 1
      AND columna.name = N'FacturaId'
)
OR NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS indice
    INNER JOIN sys.index_columns AS columnaIndice
        ON columnaIndice.object_id = indice.object_id
       AND columnaIndice.index_id = indice.index_id
    INNER JOIN sys.columns AS columna
        ON columna.object_id = columnaIndice.object_id
       AND columna.column_id = columnaIndice.column_id
    WHERE indice.object_id =
          OBJECT_ID(N'facturacion.Movimientos')
      AND indice.name =
          N'IX_Movimientos_FacturaId_Anio_Fecha'
      AND columnaIndice.key_ordinal = 2
      AND columna.name = N'Anio'
)
OR NOT EXISTS
(
    SELECT 1
    FROM sys.indexes AS indice
    INNER JOIN sys.index_columns AS columnaIndice
        ON columnaIndice.object_id = indice.object_id
       AND columnaIndice.index_id = indice.index_id
    INNER JOIN sys.columns AS columna
        ON columna.object_id = columnaIndice.object_id
       AND columna.column_id = columnaIndice.column_id
    WHERE indice.object_id =
          OBJECT_ID(N'facturacion.Movimientos')
      AND indice.name =
          N'IX_Movimientos_FacturaId_Anio_Fecha'
      AND columnaIndice.key_ordinal = 3
      AND columna.name = N'Fecha'
)
BEGIN
    THROW 50010,
        'Las columnas del nuevo índice no tienen el orden esperado.',
        1;
END;
GO

/*
    ============================================================
    6. RESULTADOS DE VERIFICACIÓN
    ============================================================
*/

SELECT
    MigrationId,
    ProductVersion
FROM dbo.__SeguimientoFacturacionMigrationsHistory
WHERE MigrationId LIKE
      N'%PermitirMovimientosAnualesSinFecha'
ORDER BY MigrationId;
GO

SELECT
    columna.name AS Columna,
    tipo.name AS Tipo,
    columna.is_nullable AS PermiteNulos,
    restriccionPredeterminada.definition
        AS ValorPredeterminado
FROM sys.columns AS columna
INNER JOIN sys.tables AS tabla
    ON tabla.object_id = columna.object_id
INNER JOIN sys.schemas AS esquema
    ON esquema.schema_id = tabla.schema_id
INNER JOIN sys.types AS tipo
    ON tipo.user_type_id = columna.user_type_id
LEFT JOIN sys.default_constraints AS restriccionPredeterminada
    ON restriccionPredeterminada.object_id =
       columna.default_object_id
WHERE esquema.name = N'facturacion'
  AND tabla.name = N'Movimientos'
  AND columna.name IN
      (
          N'Anio',
          N'Fecha'
      )
ORDER BY columna.column_id;
GO

SELECT
    name AS Restriccion,
    definition AS Definicion,
    is_disabled AS Deshabilitada,
    is_not_trusted AS NoConfiable
FROM sys.check_constraints
WHERE parent_object_id =
      OBJECT_ID(N'facturacion.Movimientos')
  AND name = N'CK_Movimientos_Anio';
GO

SELECT
    indice.name AS Indice,
    columnaIndice.key_ordinal AS Orden,
    columna.name AS Columna
FROM sys.indexes AS indice
INNER JOIN sys.index_columns AS columnaIndice
    ON columnaIndice.object_id = indice.object_id
   AND columnaIndice.index_id = indice.index_id
INNER JOIN sys.columns AS columna
    ON columna.object_id = columnaIndice.object_id
   AND columna.column_id = columnaIndice.column_id
WHERE indice.object_id =
      OBJECT_ID(N'facturacion.Movimientos')
  AND indice.name =
      N'IX_Movimientos_FacturaId_Anio_Fecha'
ORDER BY columnaIndice.key_ordinal;
GO

SELECT
    (SELECT COUNT_BIG(*)
     FROM facturacion.Facturas) AS Facturas,

    (SELECT COUNT_BIG(*)
     FROM facturacion.Movimientos) AS Movimientos;
GO

PRINT N'Verificación de movimientos anuales finalizada correctamente.';
GO