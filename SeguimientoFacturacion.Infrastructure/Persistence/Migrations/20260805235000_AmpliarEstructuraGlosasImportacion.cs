using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure
    .Persistence.Migrations;

/// <inheritdoc />
public partial class
    AmpliarEstructuraGlosasImportacion : Migration
{
    /// <inheritdoc />
    protected override void Up(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<int>(
            name: "Estado",
            schema: "importacion",
            table: "GlosasTemporales",
            type: "int",
            nullable: false,
            defaultValue: 1);

        migrationBuilder.AddColumn<decimal>(
            name: "ValorAceptado",
            schema: "importacion",
            table: "GlosasTemporales",
            type: "decimal(18,2)",
            precision: 18,
            scale: 2,
            nullable: false,
            defaultValue: 0m);

        migrationBuilder.Sql(
            "UPDATE [importacion].[GlosasTemporales] " +
            "SET [Estado] = 2 " +
            "WHERE [FechaRespuesta] IS NOT NULL;");

        migrationBuilder.AddCheckConstraint(
            name: "CK_GlosasTemporales_Estado",
            schema: "importacion",
            table: "GlosasTemporales",
            sql: "[Estado] IN (1, 2, 3, 4, 5)");

        migrationBuilder.AddCheckConstraint(
            name: "CK_GlosasTemporales_ValorAceptado",
            schema: "importacion",
            table: "GlosasTemporales",
            sql: "[ValorAceptado] >= 0 AND " +
                 "[ValorAceptado] <= [ValorGlosa]");

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

    /// <inheritdoc />
    protected override void Down(
        MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropCheckConstraint(
            name: "CK_GlosasTemporales_Estado",
            schema: "importacion",
            table: "GlosasTemporales");

        migrationBuilder.DropCheckConstraint(
            name: "CK_GlosasTemporales_Resolucion",
            schema: "importacion",
            table: "GlosasTemporales");

        migrationBuilder.DropCheckConstraint(
            name: "CK_GlosasTemporales_ValorAceptado",
            schema: "importacion",
            table: "GlosasTemporales");

        migrationBuilder.DropColumn(
            name: "Estado",
            schema: "importacion",
            table: "GlosasTemporales");

        migrationBuilder.DropColumn(
            name: "ValorAceptado",
            schema: "importacion",
            table: "GlosasTemporales");
    }
}
