IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    IF SCHEMA_ID(N'facturacion') IS NULL EXEC(N'CREATE SCHEMA [facturacion];');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[Aseguradoras] (
        [Id] int NOT NULL,
        [Descripcion] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Aseguradoras] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[Atenciones] (
        [Id] int NOT NULL,
        [Descripcion] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Atenciones] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[Costos] (
        [Id] int NOT NULL,
        [Descripcion] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Costos] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[Estados] (
        [Id] int NOT NULL,
        [Descripcion] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_Estados] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[Facturadores] (
        [Id] int NOT NULL,
        [Nombre] nvarchar(500) NOT NULL,
        CONSTRAINT [PK_Facturadores] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[TiposDocumento] (
        [Id] int NOT NULL,
        [Sigla] nvarchar(20) NOT NULL,
        [Descripcion] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_TiposDocumento] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[TiposMovimiento] (
        [Id] int NOT NULL,
        [Descripcion] nvarchar(100) NOT NULL,
        CONSTRAINT [PK_TiposMovimiento] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[Facturas] (
        [Id] varchar(50) NOT NULL,
        [Prefijo] varchar(50) NOT NULL,
        [Numero] varchar(50) NOT NULL,
        [FechaFactura] date NOT NULL,
        [AseguradoraId] int NOT NULL,
        [Valor] decimal(18,2) NOT NULL,
        [FechaRadicacion] date NULL,
        [TipoDocumentoId] int NOT NULL,
        [NumeroDocumento] varchar(50) NOT NULL,
        [NombreCompleto] nvarchar(255) NOT NULL,
        [AtencionId] int NOT NULL,
        [CostoId] int NOT NULL,
        [NumeroAdmision] varchar(50) NULL,
        [FechaAdmision] date NULL,
        [EstadoId] int NOT NULL,
        [FacturadorId] int NOT NULL,
        [FechaCreacionUtc] datetimeoffset(0) NOT NULL,
        [CreadoPor] varchar(100) NOT NULL,
        [FechaModificacionUtc] datetimeoffset(0) NULL,
        [ModificadoPor] varchar(100) NULL,
        CONSTRAINT [PK_Facturas] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Facturas_Aseguradoras_AseguradoraId] FOREIGN KEY ([AseguradoraId]) REFERENCES [facturacion].[Aseguradoras] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Facturas_Atenciones_AtencionId] FOREIGN KEY ([AtencionId]) REFERENCES [facturacion].[Atenciones] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Facturas_Costos_CostoId] FOREIGN KEY ([CostoId]) REFERENCES [facturacion].[Costos] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Facturas_Estados_EstadoId] FOREIGN KEY ([EstadoId]) REFERENCES [facturacion].[Estados] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Facturas_Facturadores_FacturadorId] FOREIGN KEY ([FacturadorId]) REFERENCES [facturacion].[Facturadores] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Facturas_TiposDocumento_TipoDocumentoId] FOREIGN KEY ([TipoDocumentoId]) REFERENCES [facturacion].[TiposDocumento] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE TABLE [facturacion].[Movimientos] (
        [Id] bigint NOT NULL IDENTITY,
        [FacturaId] varchar(50) NOT NULL,
        [TipoMovimientoId] int NOT NULL,
        [Fecha] date NOT NULL,
        [Valor] decimal(18,2) NOT NULL,
        [NumeroNotaCredito] int NULL,
        [Observacion] nvarchar(500) NULL,
        [FechaCreacionUtc] datetimeoffset(0) NOT NULL,
        [CreadoPor] varchar(100) NOT NULL,
        [FechaModificacionUtc] datetimeoffset(0) NULL,
        [ModificadoPor] varchar(100) NULL,
        CONSTRAINT [PK_Movimientos] PRIMARY KEY ([Id]),
        CONSTRAINT [CK_Movimientos_NumeroNotaCredito] CHECK (([TipoMovimientoId] = 1 AND [NumeroNotaCredito] IS NOT NULL AND [NumeroNotaCredito] > 0) OR ([TipoMovimientoId] <> 1 AND [NumeroNotaCredito] IS NULL)),
        CONSTRAINT [CK_Movimientos_Valor] CHECK ([Valor] >= 0),
        CONSTRAINT [FK_Movimientos_Facturas_FacturaId] FOREIGN KEY ([FacturaId]) REFERENCES [facturacion].[Facturas] ([Id]) ON DELETE NO ACTION,
        CONSTRAINT [FK_Movimientos_TiposMovimiento_TipoMovimientoId] FOREIGN KEY ([TipoMovimientoId]) REFERENCES [facturacion].[TiposMovimiento] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Descripcion') AND [object_id] = OBJECT_ID(N'[facturacion].[TiposMovimiento]'))
        SET IDENTITY_INSERT [facturacion].[TiposMovimiento] ON;
    EXEC(N'INSERT INTO [facturacion].[TiposMovimiento] ([Id], [Descripcion])
    VALUES (1, N''NOTA CREDITO''),
    (2, N''ABONOS''),
    (3, N''GLOSA Y/O DEVOLUCION''),
    (4, N''CONCILIACION'')');
    IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'Id', N'Descripcion') AND [object_id] = OBJECT_ID(N'[facturacion].[TiposMovimiento]'))
        SET IDENTITY_INSERT [facturacion].[TiposMovimiento] OFF;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Facturas_AseguradoraId] ON [facturacion].[Facturas] ([AseguradoraId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Facturas_AtencionId] ON [facturacion].[Facturas] ([AtencionId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Facturas_CostoId] ON [facturacion].[Facturas] ([CostoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Facturas_EstadoId] ON [facturacion].[Facturas] ([EstadoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Facturas_FacturadorId] ON [facturacion].[Facturas] ([FacturadorId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Facturas_FechaFactura] ON [facturacion].[Facturas] ([FechaFactura]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Facturas_TipoDocumentoId] ON [facturacion].[Facturas] ([TipoDocumentoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Movimientos_FacturaId_Fecha] ON [facturacion].[Movimientos] ([FacturaId], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE INDEX [IX_Movimientos_TipoMovimientoId] ON [facturacion].[Movimientos] ([TipoMovimientoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    CREATE UNIQUE INDEX [UX_TiposDocumento_Sigla] ON [facturacion].[TiposDocumento] ([Sigla]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260724161026_InicialEsquemaFacturacion'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724161026_InicialEsquemaFacturacion', N'10.0.10');
END;

COMMIT;
GO

