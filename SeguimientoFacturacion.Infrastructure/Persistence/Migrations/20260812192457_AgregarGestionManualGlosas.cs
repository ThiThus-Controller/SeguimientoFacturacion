using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarGestionManualGlosas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Observacion",
                schema: "facturacion",
                table: "Glosas",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true);

            // Normaliza las glosas históricas que ya estaban resueltas
            // antes de que existiera la columna Observacion.
            migrationBuilder.Sql(
                """
                UPDATE [facturacion].[Glosas]
                SET [Observacion] =
                    N'Registro histórico migrado sin observación.'
                WHERE [Estado] IN (3, 4, 5)
                  AND NULLIF(
                      LTRIM(RTRIM([Observacion])),
                      N''
                  ) IS NULL;
                """);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Glosas_Anulacion",
                schema: "facturacion",
                table: "Glosas",
                sql: "[Estado] <> 6 OR [ValorAceptado] = 0");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Glosas_Estado",
                schema: "facturacion",
                table: "Glosas",
                sql: "[Estado] BETWEEN 1 AND 6");

            migrationBuilder.AddCheckConstraint(
                name: "CK_Glosas_ObservacionResolucion",
                schema: "facturacion",
                table: "Glosas",
                sql:
                    "[Estado] IN (1, 2) " +
                    "OR NULLIF(LTRIM(RTRIM([Observacion])), '') " +
                    "IS NOT NULL");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Glosas_Anulacion",
                schema: "facturacion",
                table: "Glosas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Glosas_Estado",
                schema: "facturacion",
                table: "Glosas");

            migrationBuilder.DropCheckConstraint(
                name: "CK_Glosas_ObservacionResolucion",
                schema: "facturacion",
                table: "Glosas");

            migrationBuilder.DropColumn(
                name: "Observacion",
                schema: "facturacion",
                table: "Glosas");
        }
    }
}