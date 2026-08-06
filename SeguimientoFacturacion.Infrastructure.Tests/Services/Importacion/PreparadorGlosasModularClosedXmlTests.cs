using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class
    PreparadorGlosasModularClosedXmlTests
{
    [Fact]
    public async Task
        Preparar_GlosasValidas_DebeCrearDtosYCalcularTotales()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        fechaGlosa:
                            new DateTime(2026, 2, 1),
                        valorGlosa: 100000m,
                        fechaRespuesta:
                            new DateTime(2026, 2, 10),
                        estado: "ACEPTADA",
                        valorAceptado: 60000m,
                        feComoFormula: true);

                    EscribirFila(
                        hoja,
                        fila: 3,
                        numeroFactura: "000002",
                        fechaGlosa:
                            new DateTime(2026, 2, 2),
                        valorGlosa: 50000m,
                        fechaRespuesta: null);
                });

        var consultaFacturas =
            new ConsultaFacturasControlada(
            [
                CrearReferenciaFactura(
                    "FE000001",
                    1,
                    new DateOnly(2026, 1, 10)),

                CrearReferenciaFactura(
                    "FE000002",
                    1,
                    new DateOnly(2026, 1, 11))
            ]);

        var preparador =
            CrearPreparador(
                consultaFacturas);

        var resultado =
            await preparador.PrepararAsync(
                CrearSolicitud(archivo));

        Assert.Equal(
            "PlantillaGlosas.xlsx",
            resultado.NombreArchivo);

        Assert.Equal(2, resultado.TotalGlosas);

        Assert.Equal(
            1,
            resultado.TotalGlosasConRespuesta);

        Assert.Equal(
            1,
            resultado.TotalGlosasSinRespuesta);

        Assert.Equal(
            150000m,
            resultado.ValorTotalGlosado);

        var glosaRespondida =
            Assert.Single(
                resultado.Glosas,
                glosa =>
                    glosa.TieneRespuesta);

        Assert.Equal(
            "Hoja1",
            glosaRespondida.HojaOrigen);

        Assert.Equal(
            2,
            glosaRespondida.FilaOrigen);

        Assert.Equal(
            "FE000001",
            glosaRespondida.IdentificadorFe);

        Assert.Equal(
            "FE",
            glosaRespondida.Prefijo);

        Assert.Equal(
            "000001",
            glosaRespondida.NumeroFactura);

        Assert.Equal(
            1,
            glosaRespondida.AseguradoraId);

        Assert.Equal(
            new DateOnly(2026, 2, 1),
            glosaRespondida.FechaGlosa);

        Assert.Equal(
            100000m,
            glosaRespondida.ValorGlosa);

        Assert.Equal(
            new DateOnly(2026, 2, 10),
            glosaRespondida.FechaRespuesta);

        Assert.Equal(
            EstadoGlosa.Aceptada,
            glosaRespondida.Estado);

        Assert.Equal(
            60000m,
            glosaRespondida.ValorAceptado);

        var glosaAbierta =
            Assert.Single(
                resultado.Glosas,
                glosa =>
                    !glosa.TieneRespuesta);

        Assert.Equal(
            "FE000002",
            glosaAbierta.IdentificadorFe);

        Assert.Null(
            glosaAbierta.FechaRespuesta);

        /*
         * La primera consulta se realiza durante la
         * validación y la segunda durante la preparación.
         * En ambos casos la consulta se ejecuta por lote.
         */
        Assert.Equal(
            2,
            consultaFacturas.CantidadConsultas);
    }

    [Fact]
    public async Task
        Preparar_ArchivoInvalido_DebeImpedirPreparacion()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        fechaGlosa:
                            new DateTime(2026, 2, 1),
                        valorGlosa: 0m,
                        fechaRespuesta: null));

        var consultaFacturas =
            new ConsultaFacturasControlada(
            [
                CrearReferenciaFactura(
                    "FE000001",
                    1,
                    new DateOnly(2026, 1, 10))
            ]);

        var preparador =
            CrearPreparador(
                consultaFacturas);

        var excepcion =
            await Assert.ThrowsAsync<
                InvalidOperationException>(
                () =>
                    preparador.PrepararAsync(
                        CrearSolicitud(archivo)));

        Assert.Contains(
            "error(es) bloqueante(s)",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Preparar_DebeConservarPosicionOriginalDelFlujo()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        fechaGlosa:
                            new DateTime(2026, 2, 1),
                        valorGlosa: 100000m,
                        fechaRespuesta: null));

        var consultaFacturas =
            new ConsultaFacturasControlada(
            [
                CrearReferenciaFactura(
                    "FE000001",
                    1,
                    new DateOnly(2026, 1, 10))
            ]);

        var preparador =
            CrearPreparador(
                consultaFacturas);

        const long posicionOriginal = 13;

        archivo.Position = posicionOriginal;

        var resultado =
            await preparador.PrepararAsync(
                CrearSolicitud(archivo));

        Assert.Single(resultado.Glosas);

        Assert.Equal(
            posicionOriginal,
            archivo.Position);
    }

    private static
        PreparadorGlosasModularClosedXml
        CrearPreparador(
            IConsultaReferenciasFacturasImportacion
                consultaFacturas)
    {
        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var validador =
            new ValidadorGlosasModularClosedXml(
                inspector,
                new ConsultaCatalogosControlada(),
                consultaFacturas);

        return new PreparadorGlosasModularClosedXml(
            validador,
            inspector,
            consultaFacturas);
    }

    private static SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo =
                "PlantillaGlosas.xlsx",

            Contenido = contenido
        };
    }

    private static ReferenciaFacturaImportacionDto
        CrearReferenciaFactura(
            string facturaId,
            int aseguradoraId,
            DateOnly fechaFactura)
    {
        return new ReferenciaFacturaImportacionDto
        {
            FacturaId = facturaId,
            AseguradoraId = aseguradoraId,
            FechaFactura = fechaFactura
        };
    }

    private static MemoryStream CrearArchivo(
        Action<IXLWorksheet> configurar)
    {
        var contenido = new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add("Hoja1");

            var encabezados =
                ContratosPlantillasImportacion
                    .Glosas
                    .EncabezadosRequeridos;

            for (var indice = 0;
                 indice < encabezados.Count;
                 indice++)
            {
                hoja.Cell(1, indice + 1).Value =
                    encabezados[indice];
            }

            configurar(hoja);

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static void EscribirFila(
        IXLWorksheet hoja,
        int fila,
        string numeroFactura,
        DateTime fechaGlosa,
        decimal valorGlosa,
        DateTime? fechaRespuesta,
        string? estado = null,
        decimal? valorAceptado = null,
        bool feComoFormula = false)
    {
        if (feComoFormula)
        {
            hoja.Cell(fila, 1).FormulaA1 =
                $"B{fila}&C{fila}";
        }
        else
        {
            hoja.Cell(fila, 1).Value =
                $"FE{numeroFactura}";
        }

        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = numeroFactura;
        hoja.Cell(fila, 4).Value = "NUEVA EPS";
        hoja.Cell(fila, 5).Value = fechaGlosa;
        hoja.Cell(fila, 6).Value = valorGlosa;

        if (fechaRespuesta.HasValue)
        {
            hoja.Cell(fila, 7).Value =
                fechaRespuesta.Value;
        }

        hoja.Cell(fila, 8).Value =
            estado ??
            (fechaRespuesta.HasValue
                ? "RESPONDIDA"
                : "ABIERTA");

        if (valorAceptado.HasValue)
        {
            hoja.Cell(fila, 9).Value =
                valorAceptado.Value;
        }
    }

    private sealed class
        ConsultaCatalogosControlada :
            IConsultaCatalogosImportacion
    {
        public Task<CatalogosImportacionDto>
            ObtenerAsync(
                CancellationToken cancellationToken =
                    default)
        {
            return Task.FromResult(
                new CatalogosImportacionDto
                {
                    Aseguradoras =
                    [
                        new
                            ReferenciaCatalogoImportacionDto
                            {
                                Id = 1,
                                Valor = "NUEVA EPS"
                            }
                    ]
                });
        }
    }

    private sealed class
        ConsultaFacturasControlada :
            IConsultaReferenciasFacturasImportacion
    {
        private readonly IReadOnlyCollection<
            ReferenciaFacturaImportacionDto>
            _referencias;

        public ConsultaFacturasControlada(
            IReadOnlyCollection<
                ReferenciaFacturaImportacionDto>
                referencias)
        {
            _referencias = referencias;
        }

        public int CantidadConsultas
        {
            get;
            private set;
        }

        public Task<IReadOnlyCollection<
            ReferenciaFacturaImportacionDto>>
            ObtenerPorIdsAsync(
                IReadOnlyCollection<string> facturaIds,
                CancellationToken cancellationToken =
                    default)
        {
            CantidadConsultas++;

            IReadOnlyCollection<
                ReferenciaFacturaImportacionDto>
                resultado =
                    _referencias
                        .Where(
                            referencia =>
                                facturaIds.Contains(
                                    referencia.FacturaId,
                                    StringComparer
                                        .OrdinalIgnoreCase))
                        .ToArray();

            return Task.FromResult(resultado);
        }
    }
}
