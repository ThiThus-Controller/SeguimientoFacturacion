using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class
    PreparadorFacturasModularClosedXmlTests
{
    [Fact]
    public async Task
        Preparar_ArchivoValido_DebeConvertirLasDieciseisColumnas()
    {
        await using var archivo =
            CrearArchivoValido();

        var consultaCatalogos =
            new ConsultaCatalogosControlada(
                CrearCatalogos());

        var preparador =
            CrearPreparador(
                consultaCatalogos);

        var resultado =
            await preparador.PrepararAsync(
                CrearSolicitud(archivo));

        var factura =
            Assert.Single(resultado.Facturas);

        Assert.Equal(
            "PlantillaFacturas.xlsx",
            resultado.NombreArchivo);

        Assert.Equal(1, resultado.TotalFacturas);
        Assert.Equal(0, resultado.TotalMovimientos);

        Assert.Equal("Hoja1", factura.HojaOrigen);
        Assert.Equal(2, factura.FilaOrigen);
        Assert.Equal("FE000001", factura.IdentificadorFe);
        Assert.Equal("FE", factura.Prefijo);
        Assert.Equal("000001", factura.Numero);

        Assert.Equal(
            new DateOnly(2026, 1, 10),
            factura.FechaFactura);

        Assert.Equal(1, factura.AseguradoraId);
        Assert.Equal(150000m, factura.Valor);

        Assert.Equal<DateOnly?>(
            new DateOnly(2026, 1, 12),
            factura.FechaRadicacion);

        Assert.Equal(1, factura.TipoDocumentoId);
        Assert.Equal("123456", factura.NumeroDocumento);
        Assert.Equal("PACIENTE PRUEBA", factura.NombreCompleto);
        Assert.Equal(1, factura.AtencionId);
        Assert.Equal(1, factura.CostoId);
        Assert.Equal("ADM-001", factura.NumeroAdmision);

        Assert.Equal<DateOnly?>(
            new DateOnly(2026, 1, 9),
            factura.FechaAdmision);

        Assert.Equal(1, factura.EstadoId);
        Assert.Equal(1, factura.FacturadorId);
        Assert.Empty(factura.Movimientos);

        Assert.Equal(
            1,
            consultaCatalogos.CantidadConsultas);
    }

    [Fact]
    public async Task
        Preparar_DebeProcesarLaFilaDos()
    {
        await using var archivo =
            CrearArchivoValido();

        var preparador =
            CrearPreparador(
                new ConsultaCatalogosControlada(
                    CrearCatalogos()));

        var resultado =
            await preparador.PrepararAsync(
                CrearSolicitud(archivo));

        var factura =
            Assert.Single(resultado.Facturas);

        Assert.Equal(2, factura.FilaOrigen);
    }

    [Fact]
    public async Task
        Preparar_FacturaAnulada_DebeDejarRadicacionNula()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFilaValida(
                        hoja,
                        fila: 2);

                    hoja.Cell(2, 7).Clear();
                    hoja.Cell(2, 15).Value = "5";
                });

        var preparador =
            CrearPreparador(
                new ConsultaCatalogosControlada(
                    CrearCatalogos()));

        var resultado =
            await preparador.PrepararAsync(
                CrearSolicitud(archivo));

        var factura =
            Assert.Single(resultado.Facturas);

        Assert.Equal(5, factura.EstadoId);
        Assert.Null(factura.FechaRadicacion);
    }

    [Fact]
    public async Task
        Preparar_ArchivoConErrores_DebeBloquearPreparacion()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFilaValida(
                        hoja,
                        fila: 2);

                    hoja.Cell(2, 6).Value =
                        decimal.Zero;
                });

        var preparador =
            CrearPreparador(
                new ConsultaCatalogosControlada(
                    CrearCatalogos()));

        var excepcion =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                    () =>
                        preparador.PrepararAsync(
                            CrearSolicitud(archivo)));

        Assert.Contains(
            "error(es) bloqueante(s)",
            excepcion.Message);
    }

    [Fact]
    public async Task
        Preparar_DebeConservarPosicionOriginalDelStream()
    {
        await using var archivo =
            CrearArchivoValido();

        archivo.Position = 5;

        var posicionOriginal =
            archivo.Position;

        var preparador =
            CrearPreparador(
                new ConsultaCatalogosControlada(
                    CrearCatalogos()));

        var resultado =
            await preparador.PrepararAsync(
                CrearSolicitud(archivo));

        Assert.Single(resultado.Facturas);

        Assert.Equal(
            posicionOriginal,
            archivo.Position);
    }

    private static
        PreparadorFacturasModularClosedXml
        CrearPreparador(
            IConsultaCatalogosImportacion
                consultaCatalogos)
    {
        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var lectorEstructural =
            new
                LectorEstructuralFacturasModularClosedXml(
                    inspector);

        var validador =
            new
                ValidadorFilasFacturasModularClosedXml();

        var lectorValidado =
            new
                LectorFacturasModularValidadoClosedXml(
                    lectorEstructural,
                    validador,
                    consultaCatalogos);

        return new
            PreparadorFacturasModularClosedXml(
                lectorValidado);
    }

    private static SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo =
                "PlantillaFacturas.xlsx",

            Contenido =
                contenido
        };
    }

    private static CatalogosImportacionDto
        CrearCatalogos()
    {
        return new CatalogosImportacionDto
        {
            Aseguradoras =
            [
                CrearReferencia(
                    1,
                    "NUEVA EPS")
            ],

            TiposDocumento =
            [
                CrearReferencia(
                    1,
                    "CC")
            ],

            Atenciones =
            [
                CrearReferencia(
                    1,
                    "AMBULATORIA")
            ],

            Costos =
            [
                CrearReferencia(
                    1,
                    "CONSULTA EXTERNA")
            ],

            Estados =
            [
                CrearReferencia(
                    1,
                    "PENDIENTE"),

                CrearReferencia(
                    5,
                    "ANULADA")
            ],

            Facturadores =
            [
                CrearReferencia(
                    1,
                    "ANA PEREZ")
            ]
        };
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

    private static MemoryStream
        CrearArchivoValido()
    {
        return CrearArchivo(
            hoja =>
                EscribirFilaValida(
                    hoja,
                    fila: 2));
    }

    private static MemoryStream CrearArchivo(
        Action<IXLWorksheet> configurar)
    {
        var contenido =
            new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add("Hoja1");

            EscribirEncabezados(hoja);
            configurar(hoja);

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static void EscribirEncabezados(
        IXLWorksheet hoja)
    {
        var encabezados =
            ContratosPlantillasImportacion
                .Facturas
                .EncabezadosRequeridos;

        for (var indice = 0;
             indice < encabezados.Count;
             indice++)
        {
            hoja.Cell(1, indice + 1).Value =
                encabezados[indice];
        }
    }

    private static void EscribirFilaValida(
        IXLWorksheet hoja,
        int fila)
    {
        hoja.Cell(fila, 1).Value = "FE000001";
        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = "000001";

        hoja.Cell(fila, 4).Value =
            new DateTime(2026, 1, 10);

        hoja.Cell(fila, 5).Value =
            "NUEVA EPS";

        hoja.Cell(fila, 6).Value =
            150000m;

        hoja.Cell(fila, 7).Value =
            new DateTime(2026, 1, 12);

        hoja.Cell(fila, 8).Value = "CC";
        hoja.Cell(fila, 9).Value = "123456";
        hoja.Cell(fila, 10).Value = "PACIENTE PRUEBA";
        hoja.Cell(fila, 11).Value = "AMBULATORIA";

        hoja.Cell(fila, 12).Value =
            "CONSULTA EXTERNA";

        hoja.Cell(fila, 13).Value = "ADM-001";

        hoja.Cell(fila, 14).Value =
            new DateTime(2026, 1, 9);

        hoja.Cell(fila, 15).Value = "PENDIENTE";
        hoja.Cell(fila, 16).Value = "ANA PEREZ";
    }

    private sealed class
        ConsultaCatalogosControlada :
        IConsultaCatalogosImportacion
    {
        private readonly CatalogosImportacionDto
            _catalogos;

        public ConsultaCatalogosControlada(
            CatalogosImportacionDto catalogos)
        {
            _catalogos = catalogos;
        }

        public int CantidadConsultas
        {
            get;
            private set;
        }

        public Task<CatalogosImportacionDto>
            ObtenerAsync(
                CancellationToken cancellationToken =
                    default)
        {
            cancellationToken
                .ThrowIfCancellationRequested();

            CantidadConsultas++;

            return Task.FromResult(
                _catalogos);
        }
    }
}