using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AdministrarCatalogoAseguradoras : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "Activo",
                schema: "facturacion",
                table: "Aseguradoras",
                type: "bit",
                nullable: false,
                defaultValue: true);

            migrationBuilder.AddColumn<string>(
                name: "CreadoPor",
                schema: "facturacion",
                table: "Aseguradoras",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "migracion-sistema");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaCreacionUtc",
                schema: "facturacion",
                table: "Aseguradoras",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSUTCDATETIME()");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "FechaModificacionUtc",
                schema: "facturacion",
                table: "Aseguradoras",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ModificadoPor",
                schema: "facturacion",
                table: "Aseguradoras",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_Aseguradoras_Descripcion",
                schema: "facturacion",
                table: "Aseguradoras",
                column: "Descripcion");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Aseguradoras_Descripcion",
                schema: "facturacion",
                table: "Aseguradoras");

            migrationBuilder.DropColumn(
                name: "Activo",
                schema: "facturacion",
                table: "Aseguradoras");

            migrationBuilder.DropColumn(
                name: "CreadoPor",
                schema: "facturacion",
                table: "Aseguradoras");

            migrationBuilder.DropColumn(
                name: "FechaCreacionUtc",
                schema: "facturacion",
                table: "Aseguradoras");

            migrationBuilder.DropColumn(
                name: "FechaModificacionUtc",
                schema: "facturacion",
                table: "Aseguradoras");

            migrationBuilder.DropColumn(
                name: "ModificadoPor",
                schema: "facturacion",
                table: "Aseguradoras");
        }
    }
}
