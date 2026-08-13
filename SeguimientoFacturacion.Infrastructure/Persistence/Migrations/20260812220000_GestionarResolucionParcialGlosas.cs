using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class GestionarResolucionParcialGlosas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Glosas_Estado",
                schema: "facturacion",
                table: "Glosas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GlosasTemporales_Estado",
                schema: "importacion",
                table: "GlosasTemporales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GlosasTemporales_Resolucion",
                schema: "importacion",
                table: "GlosasTemporales");

            migrationBuilder.Sql(
                """
                UPDATE [facturacion].[Glosas]
                SET [Estado] = 7
                WHERE [Estado] = 3
                  AND [ValorAceptado] > 0
                  AND [ValorAceptado] < [ValorGlosa];

                UPDATE [importacion].[GlosasTemporales]
                SET [Estado] = 7
                WHERE [Estado] = 3
                  AND [ValorAceptado] > 0
                  AND [ValorAceptado] < [ValorGlosa];
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Glosas_Estado",
                schema: "facturacion",
                table: "Glosas",
                sql: "[Estado] BETWEEN 1 AND 7");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Glosas_Resolucion",
                schema: "facturacion",
                table: "Glosas",
                sql:
                    "([Estado] IN (1, 2) AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 3 AND " +
                    "[ValorAceptado] = [ValorGlosa]) OR " +
                    "([Estado] = 4 AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 5) OR " +
                    "([Estado] = 6 AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 7 AND " +
                    "[ValorAceptado] > 0 AND " +
                    "[ValorAceptado] < [ValorGlosa])");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GlosasTemporales_Estado",
                schema: "importacion",
                table: "GlosasTemporales",
                sql: "[Estado] IN (1, 2, 3, 4, 5, 7)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GlosasTemporales_Resolucion",
                schema: "importacion",
                table: "GlosasTemporales",
                sql:
                    "([Estado] = 1 AND " +
                    "[FechaRespuesta] IS NULL AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 2 AND " +
                    "[FechaRespuesta] IS NOT NULL AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 3 AND " +
                    "[FechaRespuesta] IS NOT NULL AND " +
                    "[ValorAceptado] = [ValorGlosa]) OR " +
                    "([Estado] = 4 AND " +
                    "[FechaRespuesta] IS NOT NULL AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 5 AND " +
                    "[FechaRespuesta] IS NOT NULL) OR " +
                    "([Estado] = 7 AND " +
                    "[FechaRespuesta] IS NOT NULL AND " +
                    "[ValorAceptado] > 0 AND " +
                    "[ValorAceptado] < [ValorGlosa])");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Glosas_Estado",
                schema: "facturacion",
                table: "Glosas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Glosas_Resolucion",
                schema: "facturacion",
                table: "Glosas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GlosasTemporales_Estado",
                schema: "importacion",
                table: "GlosasTemporales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_GlosasTemporales_Resolucion",
                schema: "importacion",
                table: "GlosasTemporales");

            migrationBuilder.Sql(
                """
                UPDATE [facturacion].[Glosas]
                SET [Estado] = 3
                WHERE [Estado] = 7;

                UPDATE [importacion].[GlosasTemporales]
                SET [Estado] = 3
                WHERE [Estado] = 7;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Glosas_Estado",
                schema: "facturacion",
                table: "Glosas",
                sql: "[Estado] BETWEEN 1 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GlosasTemporales_Estado",
                schema: "importacion",
                table: "GlosasTemporales",
                sql: "[Estado] IN (1, 2, 3, 4, 5)");

            migrationBuilder.AddCheckConstraint(
                name: "CK_GlosasTemporales_Resolucion",
                schema: "importacion",
                table: "GlosasTemporales",
                sql:
                    "([Estado] = 1 AND " +
                    "[FechaRespuesta] IS NULL AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 2 AND " +
                    "[FechaRespuesta] IS NOT NULL AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 3 AND " +
                    "[FechaRespuesta] IS NOT NULL AND " +
                    "[ValorAceptado] > 0) OR " +
                    "([Estado] = 4 AND " +
                    "[FechaRespuesta] IS NOT NULL AND " +
                    "[ValorAceptado] = 0) OR " +
                    "([Estado] = 5 AND " +
                    "[FechaRespuesta] IS NOT NULL)");
        }
    }
}
