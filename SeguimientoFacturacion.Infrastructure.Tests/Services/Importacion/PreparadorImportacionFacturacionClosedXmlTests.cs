using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Infrastructure.Tests.Services.Importacion;

public sealed class
    PreparadorImportacionFacturacionClosedXmlTests
{
    [Fact]
    public async Task PrepararAsync_FacturaAnulada_DebeMapearCatalogosYDejarRadicacionNula()
    {
        await using var contenido =
            CrearArchivoPrueba();

var preparador =
    new PreparadorImportacionFacturacionClosedXml(
        new LectorControlado(esValido: true),
        new ConsultaCatalogosControlada());

var solicitud =
    new SolicitudAnalisisImportacionDto
    {
        NombreArchivo =
            "Seguimiento prueba.xlsx",

        Contenido = contenido
    };

var resultado =
    await preparador.PrepararAsync(solicitud);

var factura =
    Assert.Single(resultado.Facturas);

Assert.Equal(1, resultado.TotalFacturas);
        Assert.Equal(4, resultado.TotalMovimientos);

        Assert.Equal("Datos", factura.HojaOrigen);
        Assert.Equal(3, factura.FilaOrigen);
        Assert.Equal("FE4250", factura.IdentificadorFe);
        Assert.Equal("FE", factura.Prefijo);
        Assert.Equal("4250", factura.Numero);

        Assert.Equal(
            new DateOnly(2024, 7, 10),
            factura.FechaFactura);

        Assert.Equal(1, factura.AseguradoraId);
        Assert.Equal(1_000_000m, factura.Valor);
        Assert.Null(factura.FechaRadicacion);
        Assert.Equal(1, factura.TipoDocumentoId);
        Assert.Equal("0012345678", factura.NumeroDocumento);
        Assert.Equal("Paciente de prueba", factura.NombreCompleto);
        Assert.Equal(1, factura.AtencionId);
        Assert.Equal(1, factura.CostoId);
        Assert.Equal("ADM-100", factura.NumeroAdmision);

        Assert.Equal(
            new DateOnly(2024, 7, 9),
            factura.FechaAdmision);

        Assert.Equal(5, factura.EstadoId);
        Assert.Equal(1, factura.FacturadorId);
        Assert.Equal(4, factura.Movimientos.Count);

        var notaCredito =
            Assert.Single(
                factura.Movimientos,
                movimiento =>
                    movimiento.TipoMovimientoId ==
                    TipoMovimientoCodigo.NotaCredito);

        Assert.Equal(2024, notaCredito.Anio);
        Assert.Equal("NC-5001", notaCredito.NumeroNotaCredito);
        Assert.Equal(100000m, notaCredito.Valor);

        var abono =
            Assert.Single(
                factura.Movimientos,
                movimiento =>
                    movimiento.TipoMovimientoId ==
                    TipoMovimientoCodigo.Abono);

        Assert.Equal(2025, abono.Anio);
        Assert.Null(abono.Fecha);
        Assert.Null(abono.NumeroNotaCredito);
        Assert.Equal(200000m, abono.Valor);

        var glosa =
            Assert.Single(
                factura.Movimientos,
                movimiento =>
                    movimiento.TipoMovimientoId ==
                    TipoMovimientoCodigo.GlosaODevolucion);

        Assert.Equal(
            new DateOnly(2024, 9, 1),
            glosa.Fecha);

        Assert.Equal(50000m, glosa.Valor);
        Assert.Null(glosa.NumeroNotaCredito);

        var conciliacion =
            Assert.Single(
                factura.Movimientos,
                movimiento =>
                    movimiento.TipoMovimientoId ==
                    TipoMovimientoCodigo.Conciliacion);

        Assert.Equal(
            new DateOnly(2024, 10, 1),
            conciliacion.Fecha);

        Assert.Equal(25000m, conciliacion.Valor);
        Assert.Null(conciliacion.NumeroNotaCredito);
        
    }

    [Fact]
public async Task PrepararAsync_ConAnalisisInvalido_DebeLanzarExcepcion()
    {
        await using var contenido =
            new MemoryStream([1, 2, 3]);

var preparador =
    new PreparadorImportacionFacturacionClosedXml(
        new LectorControlado(esValido: false),
        new ConsultaCatalogosControlada());

var solicitud =
    new SolicitudAnalisisImportacionDto
    {
        NombreArchivo = "Invalido.xlsx",
        Contenido = contenido
    };

var accion = async () =>
    await preparador.PrepararAsync(solicitud);

var excepcion =
    await Assert.ThrowsAsync<
        InvalidOperationException>(accion);

Assert.Contains(
    "1 error",
    excepcion.Message,
    StringComparison.OrdinalIgnoreCase);
    }

    private static MemoryStream CrearArchivoPrueba()
{
    var contenido = new MemoryStream();

    using (var libro = new XLWorkbook())
    {
        var hoja =
            libro.Worksheets.Add("Datos");

        hoja.Cell(1, 1).Value = "FE";
        hoja.Cell(1, 2).Value = "PREFIJO";
        hoja.Cell(1, 3).Value = "FACTURA";
        hoja.Cell(1, 4).Value = "FECHA FRA";
        hoja.Cell(1, 5).Value = "ASEGURADORA";
        hoja.Cell(1, 6).Value = "VALOR";

        hoja.Cell(1, 7).Value =
            "FECHA DE RADICACIÓN";

        hoja.Cell(1, 8).Value = "TIPO DTO";
        hoja.Cell(1, 9).Value = "NÚMERO DTO";

        hoja.Cell(1, 10).Value =
            "NOMBRE COMPLETO";

        hoja.Cell(1, 11).Value = "ATENCIÓN";
        hoja.Cell(1, 12).Value = "COSTO";
        hoja.Cell(1, 13).Value = "No ADMISIÓN";
        hoja.Cell(1, 14).Value = "FECHA ADMISIÓN";

        hoja.Cell(1, 15).Value =
            "ESTADO DE DTO";

        hoja.Cell(1, 16).Value = "FACTURADOR";

        hoja.Cell(1, 17).Value = "AÑO 2024";
        hoja.Cell(2, 17).Value = "No de NOTA CREDITO";
        hoja.Cell(2, 18).Value = "FECHA DE NOTA CREDITO";
        hoja.Cell(2, 19).Value = "VALOR NOTA CREDITO";

        hoja.Cell(1, 20).Value = "AÑO 2025";
        hoja.Cell(2, 20).Value = "ABONOS";
        hoja.Cell(2, 21).Value = "FECHA DE ABONO";

        hoja.Cell(1, 22).Value =
            "FECHA DE GLOSA Y/O DEVOLUCIÓN";

        hoja.Cell(1, 23).Value =
            "VALOR DE LA GLOSA Y/O DEVOLUCIÓN";

        hoja.Cell(1, 24).Value = "VALOR CONCILIADO";
        hoja.Cell(1, 25).Value = "FECHA CONCILIACIÓN";

        hoja.Cell(3, 1).Value = "FE4250";
        hoja.Cell(3, 2).Value = "FE";
        hoja.Cell(3, 3).Value = "4250";

        hoja.Cell(3, 4).Value =
            new DateTime(2024, 7, 10);

        hoja.Cell(3, 5).Value =
            "Aseguradora prueba";

        hoja.Cell(3, 6).Value = 1_000_000m;

        /*
         * Una factura anulada debe conservar
         * vacía la fecha de radicación.
         */
        hoja.Cell(3, 7).Value = string.Empty;

        hoja.Cell(3, 8).Value = "CC";
        hoja.Cell(3, 9).Value = "0012345678";

        hoja.Cell(3, 10).Value =
            "Paciente de prueba";

        hoja.Cell(3, 11).Value =
            "Consulta externa";

        hoja.Cell(3, 12).Value =
            "Costo prueba";

        hoja.Cell(3, 13).Value = "ADM-100";

        hoja.Cell(3, 14).Value =
            new DateTime(2024, 7, 9);

        hoja.Cell(3, 15).Value = "5";

        hoja.Cell(3, 16).Value =
            "Facturador prueba";

            hoja.Cell(3, 17).Value = "nc-5001";

            hoja.Cell(3, 18).Value =
                new DateTime(2024, 8, 15);

            hoja.Cell(3, 19).Value = 100000m;

            hoja.Cell(3, 20).Value = 200000m;

            /*
             * El abono no tiene fecha exacta,
             * pero pertenece al año 2025.
             */
            hoja.Cell(3, 21).Value = string.Empty;

            hoja.Cell(3, 22).Value =
                new DateTime(2024, 9, 1);

            hoja.Cell(3, 23).Value = 50000m;
            hoja.Cell(3, 24).Value = 25000m;

            hoja.Cell(3, 25).Value =
                new DateTime(2024, 10, 1);

            libro.SaveAs(contenido);
    }

    contenido.Position = 0;

    return contenido;
}

private sealed class LectorControlado :
    ILectorArchivoFacturacion
{
    private readonly bool _esValido;

    public LectorControlado(bool esValido)
    {
        _esValido = esValido;
    }

    public Task<ResultadoAnalisisImportacionDto>
        AnalizarAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default)
    {
        var inconsistencias =
            _esValido
                ? Array.Empty<
                    InconsistenciaImportacionDto>()
                :
                [
                    new InconsistenciaImportacionDto
                        {
                            Fila = 3,
                            Columna = "FE",
                            Codigo = "ERROR_CONTROLADO",
                            Mensaje =
                                "Error controlado para la prueba.",

                            Severidad =
                                SeveridadInconsistenciaImportacion
                                    .Error
                        }
                ];

        return Task.FromResult(
            new ResultadoAnalisisImportacionDto
            {
                NombreArchivo =
                    solicitud.NombreArchivo,

                Inconsistencias =
                    inconsistencias
            });
    }
}

private sealed class ConsultaCatalogosControlada :
    IConsultaCatalogosImportacion
{
    public Task<CatalogosImportacionDto>
        ObtenerAsync(
            CancellationToken cancellationToken = default)
    {
        var catalogos =
            new CatalogosImportacionDto
            {
                Aseguradoras =
                [
                    CrearReferencia(
                            1,
                            "Aseguradora prueba")
                ],

                TiposDocumento =
                [
                    CrearReferencia(1, "CC")
                ],

                Atenciones =
                [
                    CrearReferencia(
                            1,
                            "Consulta externa")
                ],

                Costos =
                [
                    CrearReferencia(
                            1,
                            "Costo prueba")
                ],

                Estados =
                [
                    CrearReferencia(5, "Anulada")
                ],

                Facturadores =
                [
                    CrearReferencia(
                            1,
                            "Facturador prueba")
                ]
            };

        return Task.FromResult(catalogos);
    }

    private static ReferenciaCatalogoImportacionDto
        CrearReferencia(
            int id,
            string valor)
    {
        return new ReferenciaCatalogoImportacionDto
        {
            Id = id,
            Valor = valor
        };
    }
}
}