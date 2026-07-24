using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class PermitirMovimientosAnualesSinFecha :
        Migration
    {
        /// <inheritdoc />
        protected override void Up(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Movimientos_FacturaId_Fecha",
                schema: "facturacion",
                table: "Movimientos");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Fecha",
                schema: "facturacion",
                table: "Movimientos",
                type: "date",
                nullable: true,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<int>(
                name: "Anio",
                schema: "facturacion",
                table: "Movimientos",
                type: "int",
                nullable: true);

            /*
                La actualización se ejecuta mediante SQL dinámico.

                Esto obliga a SQL Server a compilar la instrucción
                después de que la columna Anio haya sido creada.
            */
            migrationBuilder.Sql(
                "EXEC(N'UPDATE " +
                "[facturacion].[Movimientos] " +
                "SET [Anio] = YEAR([Fecha]) " +
                "WHERE [Anio] IS NULL;');");

            migrationBuilder.AlterColumn<int>(
                name: "Anio",
                schema: "facturacion",
                table: "Movimientos",
                type: "int",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name:
                    "IX_Movimientos_FacturaId_Anio_Fecha",
                schema: "facturacion",
                table: "Movimientos",
                columns:
                    new[]
                    {
                        "FacturaId",
                        "Anio",
                        "Fecha"
                    });

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movimientos_Anio",
                schema: "facturacion",
                table: "Movimientos",
                sql: "[Anio] BETWEEN 2000 AND 9999");
        }

        /// <inheritdoc />
        protected override void Down(
            MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                IF EXISTS
                (
                    SELECT 1
                    FROM [facturacion].[Movimientos]
                    WHERE [Fecha] IS NULL
                )
                BEGIN
                    THROW 51001,
                        'No se puede revertir la migración porque existen movimientos sin fecha exacta.',
                        1;
                END;
                """);

            migrationBuilder.DropIndex(
                name:
                    "IX_Movimientos_FacturaId_Anio_Fecha",
                schema: "facturacion",
                table: "Movimientos");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Movimientos_Anio",
                schema: "facturacion",
                table: "Movimientos");

            migrationBuilder.DropColumn(
                name: "Anio",
                schema: "facturacion",
                table: "Movimientos");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "Fecha",
                schema: "facturacion",
                table: "Movimientos",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date",
                oldNullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_FacturaId_Fecha",
                schema: "facturacion",
                table: "Movimientos",
                columns:
                    new[]
                    {
                        "FacturaId",
                        "Fecha"
                    });
        }
    }
}