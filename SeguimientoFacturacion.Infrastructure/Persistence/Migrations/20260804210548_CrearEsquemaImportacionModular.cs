using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SeguimientoFacturacion.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class CrearEsquemaImportacionModular : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Facturas_TipoDocumentoId",
                schema: "facturacion",
                table: "Facturas");

            migrationBuilder.EnsureSchema(
                name: "cartera");

            migrationBuilder.EnsureSchema(
                name: "importacion");

            migrationBuilder.EnsureSchema(
                name: "auditoria");

            migrationBuilder.CreateTable(
                name: "Glosas",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacturaId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    FechaGlosa = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorGlosa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaRespuesta = table.Column<DateOnly>(type: "date", nullable: true),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    ValorAceptado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaModificacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ModificadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Glosas", x => x.Id);
                    table.CheckConstraint("CK_Glosas_FechaRespuesta", "[FechaRespuesta] IS NULL OR [FechaRespuesta] >= [FechaGlosa]");
                    table.CheckConstraint("CK_Glosas_ValorAceptado", "[ValorAceptado] >= 0 AND [ValorAceptado] <= [ValorGlosa]");
                    table.CheckConstraint("CK_Glosas_ValorGlosa", "[ValorGlosa] > 0");
                    table.ForeignKey(
                        name: "FK_Glosas_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalSchema: "facturacion",
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LotesImportacion",
                schema: "importacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    NombreArchivo = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    HashArchivo = table.Column<string>(type: "char(64)", unicode: false, fixedLength: true, maxLength: 64, nullable: false),
                    Estado = table.Column<int>(type: "int", nullable: false),
                    TotalFilas = table.Column<int>(type: "int", nullable: false),
                    TotalFilasValidas = table.Column<int>(type: "int", nullable: false),
                    TotalFilasConError = table.Column<int>(type: "int", nullable: false),
                    TotalErrores = table.Column<int>(type: "int", nullable: false),
                    TotalAdvertencias = table.Column<int>(type: "int", nullable: false),
                    FechaAnalisisUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    FechaConfirmacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ConfirmadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true),
                    FechaInicioProcesamientoUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    FechaFinalizacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    DetalleResultado = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    VersionFila = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: true),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaModificacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ModificadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LotesImportacion", x => x.Id);
                    table.CheckConstraint("CK_LotesImportacion_Totales", "[TotalFilas] >= 0 AND [TotalFilasValidas] >= 0 AND [TotalFilasConError] >= 0 AND [TotalErrores] >= 0 AND [TotalAdvertencias] >= 0 AND [TotalFilasValidas] + [TotalFilasConError] = [TotalFilas] AND [TotalErrores] >= [TotalFilasConError]");
                });

            migrationBuilder.CreateTable(
                name: "NotasFactura",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacturaId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Numero = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Valor = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Anulada = table.Column<bool>(type: "bit", nullable: false),
                    MotivoAnulacion = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaModificacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ModificadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFactura", x => x.Id);
                    table.CheckConstraint("CK_NotasFactura_Anulacion", "([Anulada] = 0 AND [MotivoAnulacion] IS NULL) OR ([Anulada] = 1 AND NULLIF(LTRIM(RTRIM([MotivoAnulacion])), '') IS NOT NULL)");
                    table.CheckConstraint("CK_NotasFactura_Valor", "[Valor] > 0");
                    table.ForeignKey(
                        name: "FK_NotasFactura_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalSchema: "facturacion",
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pacientes",
                schema: "facturacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoDocumentoId = table.Column<int>(type: "int", nullable: false),
                    NumeroDocumento = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    NombreCompleto = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: false),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaModificacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ModificadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pacientes", x => x.Id);
                    table.UniqueConstraint("AK_Pacientes_TipoDocumento_NumeroDocumento", x => new { x.TipoDocumentoId, x.NumeroDocumento });
                    table.ForeignKey(
                        name: "FK_Pacientes_TiposDocumento_TipoDocumentoId",
                        column: x => x.TipoDocumentoId,
                        principalSchema: "facturacion",
                        principalTable: "TiposDocumento",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Pagos",
                schema: "cartera",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AseguradoraId = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateOnly>(type: "date", nullable: false),
                    Recibo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ValorPagado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorCruzado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Retencion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReteIca = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaModificacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ModificadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Pagos", x => x.Id);
                    table.CheckConstraint("CK_Pagos_CuadreFinanciero", "[ValorPagado] = [ValorCruzado] + [Retencion] + [ReteIca]");
                    table.CheckConstraint("CK_Pagos_ValoresNoNegativos", "[ValorCruzado] >= 0 AND [Retencion] >= 0 AND [ReteIca] >= 0");
                    table.CheckConstraint("CK_Pagos_ValorPagado", "[ValorPagado] > 0");
                    table.ForeignKey(
                        name: "FK_Pagos_Aseguradoras_AseguradoraId",
                        column: x => x.AseguradoraId,
                        principalSchema: "facturacion",
                        principalTable: "Aseguradoras",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "RegistrosAuditoria",
                schema: "auditoria",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TipoOperacion = table.Column<int>(type: "int", nullable: false),
                    NombreEntidad = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    EntidadId = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Usuario = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    DatosAnterioresJson = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: true),
                    DatosNuevosJson = table.Column<string>(type: "nvarchar(max)", maxLength: 20000, nullable: true),
                    Motivo = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CorrelacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RegistrosAuditoria", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FacturasTemporales",
                schema: "importacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteImportacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HojaOrigen = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    FilaOrigen = table.Column<int>(type: "int", nullable: false),
                    IdentificadorFe = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
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
                    FacturadorId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FacturasTemporales", x => x.Id);
                    table.CheckConstraint("CK_FacturasTemporales_Catalogos", "[AseguradoraId] > 0 AND [TipoDocumentoId] > 0 AND [AtencionId] > 0 AND [CostoId] > 0 AND [EstadoId] > 0 AND [FacturadorId] > 0");
                    table.CheckConstraint("CK_FacturasTemporales_Fechas", "([FechaRadicacion] IS NULL OR [FechaRadicacion] >= [FechaFactura]) AND ([FechaAdmision] IS NULL OR [FechaAdmision] <= [FechaFactura])");
                    table.CheckConstraint("CK_FacturasTemporales_FilaOrigen", "[FilaOrigen] > 0");
                    table.CheckConstraint("CK_FacturasTemporales_Valor", "[Valor] > 0");
                    table.ForeignKey(
                        name: "FK_FacturasTemporales_LotesImportacion_LoteImportacionId",
                        column: x => x.LoteImportacionId,
                        principalSchema: "importacion",
                        principalTable: "LotesImportacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GlosasTemporales",
                schema: "importacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteImportacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HojaOrigen = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    FilaOrigen = table.Column<int>(type: "int", nullable: false),
                    IdentificadorFe = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Prefijo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    NumeroFactura = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AseguradoraId = table.Column<int>(type: "int", nullable: false),
                    FechaGlosa = table.Column<DateOnly>(type: "date", nullable: false),
                    ValorGlosa = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaRespuesta = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GlosasTemporales", x => x.Id);
                    table.CheckConstraint("CK_GlosasTemporales_Aseguradora", "[AseguradoraId] > 0");
                    table.CheckConstraint("CK_GlosasTemporales_FE", "[IdentificadorFe] = [Prefijo] + [NumeroFactura]");
                    table.CheckConstraint("CK_GlosasTemporales_Fechas", "[FechaRespuesta] IS NULL OR [FechaRespuesta] >= [FechaGlosa]");
                    table.CheckConstraint("CK_GlosasTemporales_FilaOrigen", "[FilaOrigen] > 0");
                    table.CheckConstraint("CK_GlosasTemporales_Valor", "[ValorGlosa] > 0");
                    table.ForeignKey(
                        name: "FK_GlosasTemporales_LotesImportacion_LoteImportacionId",
                        column: x => x.LoteImportacionId,
                        principalSchema: "importacion",
                        principalTable: "LotesImportacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InconsistenciasImportacion",
                schema: "importacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteImportacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Severidad = table.Column<int>(type: "int", nullable: false),
                    NumeroFila = table.Column<int>(type: "int", nullable: true),
                    Columna = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Codigo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    Mensaje = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    ValorPresentado = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EsDatoSensible = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InconsistenciasImportacion", x => x.Id);
                    table.CheckConstraint("CK_InconsistenciasImportacion_NumeroFila", "[NumeroFila] IS NULL OR [NumeroFila] > 0");
                    table.ForeignKey(
                        name: "FK_InconsistenciasImportacion_LotesImportacion_LoteImportacionId",
                        column: x => x.LoteImportacionId,
                        principalSchema: "importacion",
                        principalTable: "LotesImportacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "NotasFacturaTemporales",
                schema: "importacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteImportacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HojaOrigen = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    FilaOrigen = table.Column<int>(type: "int", nullable: false),
                    IdentificadorFe = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Prefijo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    NumeroFactura = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    AseguradoraId = table.Column<int>(type: "int", nullable: false),
                    Tipo = table.Column<int>(type: "int", nullable: false),
                    FechaNota = table.Column<DateOnly>(type: "date", nullable: false),
                    NumeroNota = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ValorNota = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NotasFacturaTemporales", x => x.Id);
                    table.CheckConstraint("CK_NotasFacturaTemporales_Aseguradora", "[AseguradoraId] > 0");
                    table.CheckConstraint("CK_NotasFacturaTemporales_FE", "[IdentificadorFe] = [Prefijo] + [NumeroFactura]");
                    table.CheckConstraint("CK_NotasFacturaTemporales_FilaOrigen", "[FilaOrigen] > 0");
                    table.CheckConstraint("CK_NotasFacturaTemporales_Tipo", "[Tipo] IN (1, 2)");
                    table.CheckConstraint("CK_NotasFacturaTemporales_Valor", "[ValorNota] > 0");
                    table.ForeignKey(
                        name: "FK_NotasFacturaTemporales_LotesImportacion_LoteImportacionId",
                        column: x => x.LoteImportacionId,
                        principalSchema: "importacion",
                        principalTable: "LotesImportacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PagosTemporales",
                schema: "importacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LoteImportacionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AseguradoraId = table.Column<int>(type: "int", nullable: false),
                    FechaPago = table.Column<DateOnly>(type: "date", nullable: false),
                    Recibo = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    ValorPagado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorCruzado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Retencion = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ReteIca = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoFavorReportado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    SaldoCruzadoPendienteReportado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Notas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PagosTemporales", x => x.Id);
                    table.CheckConstraint("CK_PagosTemporales_Aseguradora", "[AseguradoraId] > 0");
                    table.CheckConstraint("CK_PagosTemporales_Cuadre", "[ValorPagado] = [ValorCruzado] + [Retencion] + [ReteIca]");
                    table.CheckConstraint("CK_PagosTemporales_Valores", "[ValorCruzado] >= 0 AND [Retencion] >= 0 AND [ReteIca] >= 0 AND [SaldoFavorReportado] >= 0 AND [SaldoCruzadoPendienteReportado] >= 0");
                    table.CheckConstraint("CK_PagosTemporales_ValorPagado", "[ValorPagado] > 0");
                    table.ForeignKey(
                        name: "FK_PagosTemporales_LotesImportacion_LoteImportacionId",
                        column: x => x.LoteImportacionId,
                        principalSchema: "importacion",
                        principalTable: "LotesImportacion",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AplicacionesPago",
                schema: "cartera",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PagoId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FacturaId = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ValorAplicado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorCruzadoAplicado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    FechaCreacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: false),
                    CreadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: false),
                    FechaModificacionUtc = table.Column<DateTimeOffset>(type: "datetimeoffset(0)", precision: 0, nullable: true),
                    ModificadoPor = table.Column<string>(type: "varchar(100)", unicode: false, maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacionesPago", x => x.Id);
                    table.CheckConstraint("CK_AplicacionesPago_Valores", "[ValorAplicado] > 0 AND [ValorCruzadoAplicado] >= 0 AND [ValorCruzadoAplicado] <= [ValorAplicado]");
                    table.ForeignKey(
                        name: "FK_AplicacionesPago_Facturas_FacturaId",
                        column: x => x.FacturaId,
                        principalSchema: "facturacion",
                        principalTable: "Facturas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AplicacionesPago_Pagos_PagoId",
                        column: x => x.PagoId,
                        principalSchema: "cartera",
                        principalTable: "Pagos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AplicacionesPagoTemporales",
                schema: "importacion",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PagoImportacionTemporalId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    HojaOrigen = table.Column<string>(type: "nvarchar(31)", maxLength: 31, nullable: false),
                    FilaOrigen = table.Column<int>(type: "int", nullable: false),
                    IdentificadorFe = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    Prefijo = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    NumeroFactura = table.Column<string>(type: "varchar(50)", unicode: false, maxLength: 50, nullable: false),
                    ValorAplicado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ValorCruzadoAplicado = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AplicacionesPagoTemporales", x => x.Id);
                    table.CheckConstraint("CK_AplicacionesPagoTemporales_FE", "[IdentificadorFe] = [Prefijo] + [NumeroFactura]");
                    table.CheckConstraint("CK_AplicacionesPagoTemporales_Fila", "[FilaOrigen] > 0");
                    table.CheckConstraint("CK_AplicacionesPagoTemporales_Valores", "[ValorAplicado] > 0 AND [ValorCruzadoAplicado] >= 0 AND [ValorCruzadoAplicado] <= [ValorAplicado]");
                    table.ForeignKey(
                        name: "FK_AplicacionesPagoTemporales_PagosTemporales_PagoImportacionTemporalId",
                        column: x => x.PagoImportacionTemporalId,
                        principalSchema: "importacion",
                        principalTable: "PagosTemporales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_TipoDocumentoId_NumeroDocumento",
                schema: "facturacion",
                table: "Facturas",
                columns: new[] { "TipoDocumentoId", "NumeroDocumento" });

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesPago_FacturaId",
                schema: "cartera",
                table: "AplicacionesPago",
                column: "FacturaId");

            migrationBuilder.CreateIndex(
                name: "UX_AplicacionesPago_Pago_Factura",
                schema: "cartera",
                table: "AplicacionesPago",
                columns: new[] { "PagoId", "FacturaId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AplicacionesPagoTemporales_FE",
                schema: "importacion",
                table: "AplicacionesPagoTemporales",
                column: "IdentificadorFe");

            migrationBuilder.CreateIndex(
                name: "UX_AplicacionesPagoTemporales_Pago_FE",
                schema: "importacion",
                table: "AplicacionesPagoTemporales",
                columns: new[] { "PagoImportacionTemporalId", "IdentificadorFe" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_AplicacionesPagoTemporales_Pago_Hoja_Fila",
                schema: "importacion",
                table: "AplicacionesPagoTemporales",
                columns: new[] { "PagoImportacionTemporalId", "HojaOrigen", "FilaOrigen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_FacturasTemporales_Lote_FE",
                schema: "importacion",
                table: "FacturasTemporales",
                columns: new[] { "LoteImportacionId", "IdentificadorFe" });

            migrationBuilder.CreateIndex(
                name: "IX_FacturasTemporales_Lote_Prefijo_Numero",
                schema: "importacion",
                table: "FacturasTemporales",
                columns: new[] { "LoteImportacionId", "Prefijo", "Numero" });

            migrationBuilder.CreateIndex(
                name: "UX_FacturasTemporales_Lote_Hoja_Fila",
                schema: "importacion",
                table: "FacturasTemporales",
                columns: new[] { "LoteImportacionId", "HojaOrigen", "FilaOrigen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Glosas_Factura_Estado_Fecha",
                schema: "facturacion",
                table: "Glosas",
                columns: new[] { "FacturaId", "Estado", "FechaGlosa" });

            migrationBuilder.CreateIndex(
                name: "IX_Glosas_FechaGlosa",
                schema: "facturacion",
                table: "Glosas",
                column: "FechaGlosa");

            migrationBuilder.CreateIndex(
                name: "UX_Glosas_Factura_Fecha_Valor",
                schema: "facturacion",
                table: "Glosas",
                columns: new[] { "FacturaId", "FechaGlosa", "ValorGlosa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GlosasTemporales_Lote_FE",
                schema: "importacion",
                table: "GlosasTemporales",
                columns: new[] { "LoteImportacionId", "IdentificadorFe" });

            migrationBuilder.CreateIndex(
                name: "UX_GlosasTemporales_Lote_Factura_Fecha_Valor",
                schema: "importacion",
                table: "GlosasTemporales",
                columns: new[] { "LoteImportacionId", "IdentificadorFe", "FechaGlosa", "ValorGlosa" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_GlosasTemporales_Lote_Hoja_Fila",
                schema: "importacion",
                table: "GlosasTemporales",
                columns: new[] { "LoteImportacionId", "HojaOrigen", "FilaOrigen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_InconsistenciasImportacion_Lote_Codigo",
                schema: "importacion",
                table: "InconsistenciasImportacion",
                columns: new[] { "LoteImportacionId", "Codigo" });

            migrationBuilder.CreateIndex(
                name: "IX_InconsistenciasImportacion_Lote_Severidad_Fila",
                schema: "importacion",
                table: "InconsistenciasImportacion",
                columns: new[] { "LoteImportacionId", "Severidad", "NumeroFila" });

            migrationBuilder.CreateIndex(
                name: "IX_LotesImportacion_Estado_FechaCreacionUtc",
                schema: "importacion",
                table: "LotesImportacion",
                columns: new[] { "Estado", "FechaCreacionUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_LotesImportacion_Tipo_HashArchivo",
                schema: "importacion",
                table: "LotesImportacion",
                columns: new[] { "Tipo", "HashArchivo" });

            migrationBuilder.CreateIndex(
                name: "IX_NotasFactura_Fecha",
                schema: "facturacion",
                table: "NotasFactura",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "UX_NotasFactura_Factura_Tipo_Numero",
                schema: "facturacion",
                table: "NotasFactura",
                columns: new[] { "FacturaId", "Tipo", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_NotasFacturaTemporales_Lote_FE",
                schema: "importacion",
                table: "NotasFacturaTemporales",
                columns: new[] { "LoteImportacionId", "IdentificadorFe" });

            migrationBuilder.CreateIndex(
                name: "UX_NotasFacturaTemporales_Lote_Factura_Tipo_Numero",
                schema: "importacion",
                table: "NotasFacturaTemporales",
                columns: new[] { "LoteImportacionId", "IdentificadorFe", "Tipo", "NumeroNota" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "UX_NotasFacturaTemporales_Lote_Hoja_Fila",
                schema: "importacion",
                table: "NotasFacturaTemporales",
                columns: new[] { "LoteImportacionId", "HojaOrigen", "FilaOrigen" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Pacientes_NombreCompleto",
                schema: "facturacion",
                table: "Pacientes",
                column: "NombreCompleto");

            migrationBuilder.CreateIndex(
                name: "IX_Pagos_Aseguradora_FechaPago",
                schema: "cartera",
                table: "Pagos",
                columns: new[] { "AseguradoraId", "FechaPago" });

            migrationBuilder.CreateIndex(
                name: "UX_Pagos_Aseguradora_Recibo",
                schema: "cartera",
                table: "Pagos",
                columns: new[] { "AseguradoraId", "Recibo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_PagosTemporales_Lote_Fecha",
                schema: "importacion",
                table: "PagosTemporales",
                columns: new[] { "LoteImportacionId", "FechaPago" });

            migrationBuilder.CreateIndex(
                name: "UX_PagosTemporales_Lote_Aseguradora_Recibo",
                schema: "importacion",
                table: "PagosTemporales",
                columns: new[] { "LoteImportacionId", "AseguradoraId", "Recibo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_CorrelacionId",
                schema: "auditoria",
                table: "RegistrosAuditoria",
                column: "CorrelacionId",
                filter: "[CorrelacionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Entidad_FechaUtc",
                schema: "auditoria",
                table: "RegistrosAuditoria",
                columns: new[] { "NombreEntidad", "EntidadId", "FechaUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_RegistrosAuditoria_Usuario_FechaUtc",
                schema: "auditoria",
                table: "RegistrosAuditoria",
                columns: new[] { "Usuario", "FechaUtc" });

            migrationBuilder.AddForeignKey(
                name: "FK_Facturas_Pacientes_Identificacion",
                schema: "facturacion",
                table: "Facturas",
                columns: new[] { "TipoDocumentoId", "NumeroDocumento" },
                principalSchema: "facturacion",
                principalTable: "Pacientes",
                principalColumns: new[] { "TipoDocumentoId", "NumeroDocumento" },
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Facturas_Pacientes_Identificacion",
                schema: "facturacion",
                table: "Facturas");

            migrationBuilder.DropTable(
                name: "AplicacionesPago",
                schema: "cartera");

            migrationBuilder.DropTable(
                name: "AplicacionesPagoTemporales",
                schema: "importacion");

            migrationBuilder.DropTable(
                name: "FacturasTemporales",
                schema: "importacion");

            migrationBuilder.DropTable(
                name: "Glosas",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "GlosasTemporales",
                schema: "importacion");

            migrationBuilder.DropTable(
                name: "InconsistenciasImportacion",
                schema: "importacion");

            migrationBuilder.DropTable(
                name: "NotasFactura",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "NotasFacturaTemporales",
                schema: "importacion");

            migrationBuilder.DropTable(
                name: "Pacientes",
                schema: "facturacion");

            migrationBuilder.DropTable(
                name: "RegistrosAuditoria",
                schema: "auditoria");

            migrationBuilder.DropTable(
                name: "Pagos",
                schema: "cartera");

            migrationBuilder.DropTable(
                name: "PagosTemporales",
                schema: "importacion");

            migrationBuilder.DropTable(
                name: "LotesImportacion",
                schema: "importacion");

            migrationBuilder.DropIndex(
                name: "IX_Facturas_TipoDocumentoId_NumeroDocumento",
                schema: "facturacion",
                table: "Facturas");

            migrationBuilder.CreateIndex(
                name: "IX_Facturas_TipoDocumentoId",
                schema: "facturacion",
                table: "Facturas",
                column: "TipoDocumentoId");
        }
    }
}
