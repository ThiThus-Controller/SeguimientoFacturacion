using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AplicarReglasNotasGlosasYAnulacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "GlosaId",
                schema: "importacion",
                table: "NotasFacturaTemporales",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "GlosaId",
                schema: "facturacion",
                table: "NotasFactura",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<byte[]>(
                name: "VersionFila",
                schema: "facturacion",
                table: "Glosas",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateIndex(
                name: "IX_NotasFacturaTemporales_GlosaId",
                schema: "importacion",
                table: "NotasFacturaTemporales",
                column: "GlosaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotasFacturaTemporales_Glosa",
                schema: "importacion",
                table: "NotasFacturaTemporales",
                sql: "([Tipo] = 1 AND [GlosaId] IS NOT NULL) OR ([Tipo] = 2 AND [GlosaId] IS NULL)");

            migrationBuilder.CreateIndex(
                name: "IX_NotasFactura_GlosaId",
                schema: "facturacion",
                table: "NotasFactura",
                column: "GlosaId");

            migrationBuilder.AddCheckConstraint(
                name: "CK_NotasFactura_Glosa",
                schema: "facturacion",
                table: "NotasFactura",
                sql: "([Tipo] = 1 AND [GlosaId] IS NOT NULL) OR ([Tipo] = 2 AND [GlosaId] IS NULL)");

            migrationBuilder.AddForeignKey(
                name: "FK_NotasFactura_Glosas_GlosaId",
                schema: "facturacion",
                table: "NotasFactura",
                column: "GlosaId",
                principalSchema: "facturacion",
                principalTable: "Glosas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_NotasFacturaTemporales_Glosas_GlosaId",
                schema: "importacion",
                table: "NotasFacturaTemporales",
                column: "GlosaId",
                principalSchema: "facturacion",
                principalTable: "Glosas",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_NotasFactura_Glosas_GlosaId",
                schema: "facturacion",
                table: "NotasFactura");

            migrationBuilder.DropForeignKey(
                name: "FK_NotasFacturaTemporales_Glosas_GlosaId",
                schema: "importacion",
                table: "NotasFacturaTemporales");

            migrationBuilder.DropIndex(
                name: "IX_NotasFacturaTemporales_GlosaId",
                schema: "importacion",
                table: "NotasFacturaTemporales");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotasFacturaTemporales_Glosa",
                schema: "importacion",
                table: "NotasFacturaTemporales");

            migrationBuilder.DropIndex(
                name: "IX_NotasFactura_GlosaId",
                schema: "facturacion",
                table: "NotasFactura");

            migrationBuilder.DropCheckConstraint(
                name: "CK_NotasFactura_Glosa",
                schema: "facturacion",
                table: "NotasFactura");

            migrationBuilder.DropColumn(
                name: "GlosaId",
                schema: "importacion",
                table: "NotasFacturaTemporales");

            migrationBuilder.DropColumn(
                name: "GlosaId",
                schema: "facturacion",
                table: "NotasFactura");

            migrationBuilder.DropColumn(
                name: "VersionFila",
                schema: "facturacion",
                table: "Glosas");
        }
    }
}
