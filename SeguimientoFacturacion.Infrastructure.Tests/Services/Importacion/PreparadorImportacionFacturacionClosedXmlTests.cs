using ClosedXML.Excel;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

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
        Assert.Equal(0, resultado.TotalMovimientos);

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
        Assert.Empty(factura.Movimientos);
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