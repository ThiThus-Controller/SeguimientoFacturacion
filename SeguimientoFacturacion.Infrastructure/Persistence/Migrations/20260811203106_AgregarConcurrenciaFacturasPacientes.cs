using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AgregarConcurrenciaFacturasPacientes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<byte[]>(
                name: "VersionFila",
                schema: "facturacion",
                table: "Pacientes",
                type: "rowversion",
                rowVersion: true,
                nullable: false);

            migrationBuilder.AddColumn<byte[]>(
                name: "VersionFila",
                schema: "facturacion",
                table: "Facturas",
                type: "rowversion",
                rowVersion: true,
                nullable: false);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VersionFila",
                schema: "facturacion",
                table: "Pacientes");

            migrationBuilder.DropColumn(
                name: "VersionFila",
                schema: "facturacion",
                table: "Facturas");
        }
    }
}