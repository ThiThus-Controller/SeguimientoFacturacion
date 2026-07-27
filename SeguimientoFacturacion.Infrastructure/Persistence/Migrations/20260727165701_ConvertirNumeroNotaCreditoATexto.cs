using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ConvertirNumeroNotaCreditoATexto : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Movimientos_NumeroNotaCredito",
                schema: "facturacion",
                table: "Movimientos");

            migrationBuilder.AlterColumn<string>(
                name: "NumeroNotaCredito",
                schema: "facturacion",
                table: "Movimientos",
                type: "varchar(50)",
                unicode: false,
                maxLength: 50,
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movimientos_NumeroNotaCredito",
                schema: "facturacion",
                table: "Movimientos",
                sql: "([TipoMovimientoId] = 1 AND NULLIF(LTRIM(RTRIM([NumeroNotaCredito])), '') IS NOT NULL) OR ([TipoMovimientoId] <> 1 AND [NumeroNotaCredito] IS NULL)");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropCheckConstraint(
                name: "CK_Movimientos_NumeroNotaCredito",
                schema: "facturacion",
                table: "Movimientos");

            migrationBuilder.AlterColumn<int>(
                name: "NumeroNotaCredito",
                schema: "facturacion",
                table: "Movimientos",
                type: "int",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "varchar(50)",
                oldUnicode: false,
                oldMaxLength: 50,
                oldNullable: true);

            migrationBuilder.AddCheckConstraint(
                name: "CK_Movimientos_NumeroNotaCredito",
                schema: "facturacion",
                table: "Movimientos",
                sql: "([TipoMovimientoId] = 1 AND [NumeroNotaCredito] IS NOT NULL AND [NumeroNotaCredito] > 0) OR ([TipoMovimientoId] <> 1 AND [NumeroNotaCredito] IS NULL)");
        }
    }
}
