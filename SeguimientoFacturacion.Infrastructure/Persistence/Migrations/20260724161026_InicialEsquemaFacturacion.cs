using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InicialEsquemaFacturacion : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "facturacion");

            migrationBuilder.CreateTable(
                name: "Aseguradoras",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Aseguradoras", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Atenciones",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Atenciones", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Costos",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Costos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Estados",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Estados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facturadores",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Nombre = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturadores", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposDocumento",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Sigla = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposDocumento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TiposMovimiento",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Descripcion = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TiposMovimiento", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Facturas",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Prefijo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Numero = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    FechaFactura = table.Column<DateOnly>(type: "date", nullable: false),
                    AseguradoraId = table.Column<int>(type: "int", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaRadicacion = table.Column<DateOnly>(type: "date", nullable: true),
                    TipoDocumentoId = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    AtencionId = table.Column<int>(type: "int", nullable: false),
                    CostoId = table.Column<int>(type: "int", nullable: false),
                    NumeroAdmision = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: true),
                    FechaAdmision = table.Column<DateOnly>(type: "date", nullable: true),
                    EstadoId = table.Column<int>(type: "int", nullable: false),
                    FacturadorId = table.Column<int>(type: "int", nullable: false),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaModificacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ModificadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Facturas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Facturas_Aseguradoras_AseguradoraId",
                        column: x => x.AseguradoraId,
                        principalSchema: "facturacion",
                        principalTable: "Aseguradoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Facturas_Atenciones_AtencionId",
                        column: x => x.AtencionId,
                        principalSchema: "facturacion",
                        principalTable: "Atenciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Facturas_Costos_CostoId",
                        column: x => x.CostoId,
                        principalSchema: "facturacion",
                        principalTable: "Costos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Facturas_Estados_EstadoId",
                        column: x => x.EstadoId,
                        principalSchema: "facturacion",
                        principalTable: "Estados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Facturas_Facturadores_FacturadorId",
                        column: x => x.FacturadorId,
                        principalSchema: "facturacion",
                        principalTable: "Facturadores",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Facturas_TiposDocumento_TipoDocumentoId",
                        column: x => x.TipoDocumentoId,
                        principalSchema: "facturacion",
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Movimientos",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    FacturaId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    TipoMovimientoId = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    NumeroNotaCredito = table.Column<int>(type: "int", nullable: true),
                    Observacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaModificacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ModificadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Movimientos", x => x.Id);
                    table.CheckConstraint("CK_Movimientos_NumeroNotaCredito", "([TipoMovimientoId] = 1 AND [NumeroNotaCredito] IS NOT NULL AND [NumeroNotaCredito] > 0) OR ([TipoMovimientoId] <> 1 AND [NumeroNotaCredito] IS NULL)");
                    table.CheckConstraint("CK_Movimientos_Valor", "[Valor] >= 0");
                    table.ForeignKey(
                        name: "FK_Movimientos_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalSchema: "facturacion",
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Movimientos_TiposMovimiento_TipoMovimientoId",
                        column: x => x.TipoMovimientoId,
                        principalSchema: "facturacion",
                        principalTable: "TiposMovimiento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.InsertData(
                schema: "facturacion",
                table: "TiposMovimiento",
                columns: new[] { "Id", "Descripcion" },
                values: new object[,]
                {
                    { 1, "NOTA CREDITO" },
                    { 2, "ABONOS" },
                    { 3, "GLOSA Y/O DEVOLUCION" },
                    { 4, "CONCILIACION" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_AseguradoraId",
                schema: "facturacion",
                table: "Facturas",
                column: "AseguradoraId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_AtencionId",
                schema: "facturacion",
                table: "Facturas",
                column: "AtencionId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_CostoId",
                schema: "facturacion",
                table: "Facturas",
                column: "CostoId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_EstadoId",
                schema: "facturacion",
                table: "Facturas",
                column: "EstadoId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_FacturadorId",
                schema: "facturacion",
                table: "Facturas",
                column: "FacturadorId");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_FechaFactura",
                schema: "facturacion",
                table: "Facturas",
                column: "FechaFactura");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_TipoDocumentoId",
                schema: "facturacion",
                table: "Facturas",
                column: "TipoDocumentoId");

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_FacturaId_Fecha",
                schema: "facturacion",
                table: "Movimientos",
                columns: new[] { "FacturaId", "Fecha" });

            migrationBuilder.CreateIndex(
                name: "IX_Movimientos_TipoMovimientoId",
                schema: "facturacion",
                table: "Movimientos",
                column: "TipoMovimientoId");

            migrationBuilder.CreateIndex(
                name: "UX_TiposDocumento_Sigla",
                schema: "facturacion",
                table: "TiposDocumento",
                column: "Sigla",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Movimientos",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "Facturas",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "TiposMovimiento",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "Aseguradoras",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "Atenciones",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "Costos",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "Estados",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "Facturadores",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "TiposDocumento",
                schema: "facturacion");
        }
    }
}
