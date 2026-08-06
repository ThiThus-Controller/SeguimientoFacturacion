using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations;

/// <summary>
/// Sustituye el cuadre informado por una distribución calculada
/// entre cartera y anticipos, conservando el total recibido.
/// </summary>
public partial class RedistribuirPagosConAnticipos : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_Pagos_CuadreFinanciero",
            schema: "cartera",
            table: "Pagos");

        migrationBuilder.DropCheckConstraint(
            name: "CK_Pagos_ValoresNoNegativos",
            schema: "cartera",
            table: "Pagos");

        migrationBuilder.DropCheckConstraint(
            name: "CK_AplicacionesPago_Valores",
            schema: "cartera",
            table: "AplicacionesPago");

        migrationBuilder.DropCheckConstraint(
            name: "CK_PagosTemporales_Cuadre",
            schema: "importacion",
            table: "PagosTemporales");

        migrationBuilder.DropCheckConstraint(
            name: "CK_PagosTemporales_Valores",
            schema: "importacion",
            table: "PagosTemporales");

        migrationBuilder.DropCheckConstraint(
            name: "CK_AplicacionesPagoTemporales_Valores",
            schema: "importacion",
            table: "AplicacionesPagoTemporales");

        migrationBuilder.AddColumn<decimal>(
            name: "ValorRecibido",
            schema: "cartera",
            table: "AplicacionesPago",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ValorAnticipo",
            schema: "cartera",
            table: "AplicacionesPago",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ValorRecibido",
            schema: "importacion",
            table: "AplicacionesPagoTemporales",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.AddColumn<decimal>(
            name: "ValorAnticipo",
            schema: "importacion",
            table: "AplicacionesPagoTemporales",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: true);

        migrationBuilder.Sql(
            """
            IF EXISTS (
                SELECT 1
                FROM cartera.Pagos p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM cartera.AplicacionesPago a
                    WHERE a.PagoId = p.Id))
                THROW 51001, 'Existen pagos definitivos sin distribución; la migración fue cancelada.', 1;

            IF EXISTS (
                SELECT 1
                FROM cartera.Pagos p
                CROSS APPLY (
                    SELECT SUM(a.ValorAplicado) AS TotalAplicado
                    FROM cartera.AplicacionesPago a
                    WHERE a.PagoId = p.Id) t
                WHERE t.TotalAplicado > p.ValorPagado)
                THROW 51002, 'Las aplicaciones definitivas superan el valor recibido.', 1;

            UPDATE cartera.AplicacionesPago
            SET ValorRecibido = ValorAplicado,
                ValorAnticipo = 0;

            ;WITH Distribuciones AS (
                SELECT a.Id,
                       ROW_NUMBER() OVER (
                           PARTITION BY a.PagoId ORDER BY a.Id) AS Posicion,
                       p.ValorPagado -
                           SUM(a.ValorAplicado) OVER (
                               PARTITION BY a.PagoId) AS Excedente
                FROM cartera.AplicacionesPago a
                INNER JOIN cartera.Pagos p ON p.Id = a.PagoId)
            UPDATE a
            SET ValorRecibido = a.ValorRecibido + d.Excedente,
                ValorAnticipo = a.ValorAnticipo + d.Excedente
            FROM cartera.AplicacionesPago a
            INNER JOIN Distribuciones d ON d.Id = a.Id
            WHERE d.Posicion = 1 AND d.Excedente > 0;

            IF EXISTS (
                SELECT 1
                FROM importacion.PagosTemporales p
                WHERE NOT EXISTS (
                    SELECT 1
                    FROM importacion.AplicacionesPagoTemporales a
                    WHERE a.PagoImportacionTemporalId = p.Id))
                THROW 51003, 'Existen pagos temporales sin distribución; la migración fue cancelada.', 1;

            IF EXISTS (
                SELECT 1
                FROM importacion.PagosTemporales p
                CROSS APPLY (
                    SELECT SUM(a.ValorAplicado) AS TotalAplicado
                    FROM importacion.AplicacionesPagoTemporales a
                    WHERE a.PagoImportacionTemporalId = p.Id) t
                WHERE t.TotalAplicado > p.ValorPagado)
                THROW 51004, 'Las aplicaciones temporales superan el valor recibido.', 1;

            UPDATE importacion.AplicacionesPagoTemporales
            SET ValorRecibido = ValorAplicado,
                ValorAnticipo = 0;

            ;WITH Distribuciones AS (
                SELECT a.Id,
                       ROW_NUMBER() OVER (
                           PARTITION BY a.PagoImportacionTemporalId
                           ORDER BY a.FilaOrigen, a.Id) AS Posicion,
                       p.ValorPagado -
                           SUM(a.ValorAplicado) OVER (
                               PARTITION BY a.PagoImportacionTemporalId) AS Excedente
                FROM importacion.AplicacionesPagoTemporales a
                INNER JOIN importacion.PagosTemporales p
                    ON p.Id = a.PagoImportacionTemporalId)
            UPDATE a
            SET ValorRecibido = a.ValorRecibido + d.Excedente,
                ValorAnticipo = a.ValorAnticipo + d.Excedente
            FROM importacion.AplicacionesPagoTemporales a
            INNER JOIN Distribuciones d ON d.Id = a.Id
            WHERE d.Posicion = 1 AND d.Excedente > 0;
            """);

        migrationBuilder.AlterColumn<decimal>(
            name: "ValorRecibido",
            schema: "cartera",
            table: "AplicacionesPago",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldPrecision: 18,
            oldScale: 2,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "ValorAnticipo",
            schema: "cartera",
            table: "AplicacionesPago",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldPrecision: 18,
            oldScale: 2,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "ValorRecibido",
            schema: "importacion",
            table: "AplicacionesPagoTemporales",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldPrecision: 18,
            oldScale: 2,
            oldNullable: true);

        migrationBuilder.AlterColumn<decimal>(
            name: "ValorAnticipo",
            schema: "importacion",
            table: "AplicacionesPagoTemporales",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "decimal(18,2)",
            oldPrecision: 18,
            oldScale: 2,
            oldNullable: true);

        migrationBuilder.DropColumn(
            name: "ValorCruzadoAplicado",
            schema: "cartera",
            table: "AplicacionesPago");

        migrationBuilder.DropColumn(
            name: "ValorCruzadoAplicado",
            schema: "importacion",
            table: "AplicacionesPagoTemporales");

        migrationBuilder.DropColumn(
            name: "ValorCruzado",
            schema: "cartera",
            table: "Pagos");

        migrationBuilder.DropColumn(
            name: "ValorCruzado",
            schema: "importacion",
            table: "PagosTemporales");

        migrationBuilder.DropColumn(
            name: "SaldoFavorReportado",
            schema: "importacion",
            table: "PagosTemporales");

        migrationBuilder.DropColumn(
            name: "SaldoCruzadoPendienteReportado",
            schema: "importacion",
            table: "PagosTemporales");

        migrationBuilder.AddCheckConstraint(
            name: "CK_Pagos_ValoresNoNegativos",
            schema: "cartera",
            table: "Pagos",
            sql: "[Retencion] >= 0 AND [ReteIca] >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AplicacionesPago_Valores",
            schema: "cartera",
            table: "AplicacionesPago",
            sql: "[ValorRecibido] > 0 AND [ValorAplicado] >= 0 AND [ValorAnticipo] >= 0 AND [ValorAplicado] + [ValorAnticipo] = [ValorRecibido]");

        migrationBuilder.AddCheckConstraint(
            name: "CK_PagosTemporales_Valores",
            schema: "importacion",
            table: "PagosTemporales",
            sql: "[Retencion] >= 0 AND [ReteIca] >= 0");

        migrationBuilder.AddCheckConstraint(
            name: "CK_AplicacionesPagoTemporales_Valores",
            schema: "importacion",
            table: "AplicacionesPagoTemporales",
            sql: "[ValorRecibido] > 0 AND [ValorAplicado] >= 0 AND [ValorAnticipo] >= 0 AND [ValorAplicado] + [ValorAnticipo] = [ValorRecibido]");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        throw new NotSupportedException(
            "La distribución histórica entre cartera y anticipos no puede revertirse sin perder su semántica financiera.");
    }
}
