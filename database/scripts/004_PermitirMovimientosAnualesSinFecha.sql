BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__SeguimientoFacturacionMigrationsHistory]
    WHERE [MigrationId] = N'20260724195327_PermitirMovimientosAnualesSinFecha'
)
BEGIN
    DROP INDEX [IX_Movimientos_FacturaId_Fecha] ON [facturacion].[Movimientos];
END;

IF NOT EXISTS (
    SELECT * FROM [__SeguimientoFacturacionMigrationsHistory]
    WHERE [MigrationId] = N'20260724195327_PermitirMovimientosAnualesSinFecha'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[facturacion].[Movimientos]') AND [c].[name] = N'Fecha');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [facturacion].[Movimientos] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [facturacion].[Movimientos] ALTER COLUMN [Fecha] date NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__SeguimientoFacturacionMigrationsHistory]
    WHERE [MigrationId] = N'20260724195327_PermitirMovimientosAnualesSinFecha'
)
BEGIN
    ALTER TABLE [facturacion].[Movimientos] ADD [Anio] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__SeguimientoFacturacionMigrationsHistory]
    WHERE [MigrationId] = N'20260724195327_PermitirMovimientosAnualesSinFecha'
)
BEGIN
    EXEC(N'UPDATE [facturacion].[Movimientos] SET [Anio] = YEAR([Fecha]) WHERE [Anio] IS NULL;');
END;

IF NOT EXISTS (
    SELECT * FROM [__SeguimientoFacturacionMigrationsHistory]
    WHERE [MigrationId] = N'20260724195327_PermitirMovimientosAnualesSinFecha'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[facturacion].[Movimientos]') AND [c].[name] = N'Anio');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [facturacion].[Movimientos] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [facturacion].[Movimientos] ALTER COLUMN [Anio] int NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__SeguimientoFacturacionMigrationsHistory]
    WHERE [MigrationId] = N'20260724195327_PermitirMovimientosAnualesSinFecha'
)
BEGIN
    CREATE INDEX [IX_Movimientos_FacturaId_Anio_Fecha] ON [facturacion].[Movimientos] ([FacturaId], [Anio], [Fecha]);
END;

IF NOT EXISTS (
    SELECT * FROM [__SeguimientoFacturacionMigrationsHistory]
    WHERE [MigrationId] = N'20260724195327_PermitirMovimientosAnualesSinFecha'
)
BEGIN
    EXEC(N'ALTER TABLE [facturacion].[Movimientos] ADD CONSTRAINT [CK_Movimientos_Anio] CHECK ([Anio] BETWEEN 2000 AND 9999)');
END;

IF NOT EXISTS (
    SELECT * FROM [__SeguimientoFacturacionMigrationsHistory]
    WHERE [MigrationId] = N'20260724195327_PermitirMovimientosAnualesSinFecha'
)
BEGIN
    INSERT INTO [__SeguimientoFacturacionMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260724195327_PermitirMovimientosAnualesSinFecha', N'10.0.10');
END;

COMMIT;
GO

