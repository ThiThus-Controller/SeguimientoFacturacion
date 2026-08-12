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
    PreparadorNotasFacturaModularClosedXmlTests
{
    [Fact]
    public async Task
        Preparar_NotasValidas_DebeCrearDtosYCalcularImpacto()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFila(
                        hoja,
                        2,
                        "000001",
                        "NC",
                        "nc-001",
                        100000m,
                        new DateTime(2026, 2, 1));

                    EscribirFila(
                        hoja,
                        3,
                        "000002",
                        "ND",
                        "nd-001",
                        50000m,
                        new DateTime(2026, 2, 2));
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
            "PlantillaNotasFactura.xlsx",
            resultado.NombreArchivo);

        Assert.Equal(2, resultado.TotalNotas);
        Assert.Equal(1, resultado.TotalNotasCredito);
        Assert.Equal(1, resultado.TotalNotasDebito);

        Assert.Equal(
            100000m,
            resultado.ValorTotalCredito);

        Assert.Equal(
            50000m,
            resultado.ValorTotalDebito);

        Assert.Equal(
            -50000m,
            resultado.ImpactoNetoSaldo);

        var notaCredito =
            Assert.Single(
                resultado.Notas,
                nota =>
                    nota.Tipo ==
                    TipoNotaFactura.Credito);

        Assert.Equal("Hoja1", notaCredito.HojaOrigen);
        Assert.Equal(2, notaCredito.FilaOrigen);

        Assert.Equal(
            "FE000001",
            notaCredito.IdentificadorFe);

        Assert.Equal("FE", notaCredito.Prefijo);

        Assert.Equal(
            "000001",
            notaCredito.NumeroFactura);

        Assert.Equal(
            1,
            notaCredito.AseguradoraId);

        Assert.Equal(
            new DateOnly(2026, 2, 1),
            notaCredito.FechaNota);

        Assert.Equal(
            "NC-001",
            notaCredito.NumeroNota);

        Assert.Equal(
            Guid.Parse(
                "11111111-1111-1111-1111-111111111111"),
            notaCredito.GlosaId);

        Assert.Equal(
            100000m,
            notaCredito.ValorNota);

        /*
         * Una consulta ocurre durante la validación y otra
         * durante la preparación. Ambas consultas se hacen
         * por lote, no una vez por cada fila.
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
                        2,
                        "000001",
                        "TIPO DESCONOCIDO",
                        "NT-001",
                        100000m,
                        new DateTime(2026, 2, 1)));

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
                        2,
                        "000001",
                        "CREDITO",
                        "NC-001",
                        100000m,
                        new DateTime(2026, 2, 1)));

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

        Assert.Single(resultado.Notas);

        Assert.Equal(
            posicionOriginal,
            archivo.Position);
    }

    private static
        PreparadorNotasFacturaModularClosedXml
        CrearPreparador(
            IConsultaReferenciasFacturasImportacion
                consultaFacturas)
    {
        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var consultaGlosas = new ConsultaGlosasControlada();

        var validador =
            new ValidadorNotasFacturaModularClosedXml(
                inspector,
                new ConsultaCatalogosControlada(),
                consultaFacturas,
                consultaGlosas);

        return new
            PreparadorNotasFacturaModularClosedXml(
                validador,
                inspector,
                consultaFacturas,
                consultaGlosas);
    }

    private static SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo =
                "PlantillaNotasFactura.xlsx",

            Contenido = contenido
        };
    }

    private static
        ReferenciaFacturaImportacionDto
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
        var contenido =
            new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add("Hoja1");

            var encabezados =
                ContratosPlantillasImportacion
                    .NotasFactura
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
        string tipo,
        string numeroNota,
        decimal valor,
        DateTime fecha)
    {
        hoja.Cell(fila, 1).Value =
            $"FE{numeroFactura}";

        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = numeroFactura;
        hoja.Cell(fila, 4).Value = "NUEVA EPS";
        hoja.Cell(fila, 5).Value = tipo;
        hoja.Cell(fila, 6).Value = fecha;
        hoja.Cell(fila, 7).Value = numeroNota;
        hoja.Cell(fila, 8).Value = valor;
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

    private sealed class ConsultaGlosasControlada :
        IConsultaGlosasNotasCredito
    {
        private static readonly Guid GlosaId =
            Guid.Parse("11111111-1111-1111-1111-111111111111");

        public Task<IReadOnlyCollection<
            ReferenciaGlosaNotaCreditoDto>>
            ObtenerPorFacturasAsync(
                IReadOnlyCollection<string> facturaIds,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyCollection<
                ReferenciaGlosaNotaCreditoDto>>(
            [
                new ReferenciaGlosaNotaCreditoDto
                {
                    GlosaId = GlosaId,
                    FacturaId = "FE000001",
                    FechaGlosa = new DateOnly(2026, 1, 20),
                    ValorGlosa = 150000m,
                    ValorAceptado = 150000m,
                    TotalNotasCreditoVigentes = 0m
                }
            ]);
        }

        public Task<int> PrepararControlConcurrenciaAsync(
            IReadOnlyCollection<Guid> glosaIds,
            DateTimeOffset fecha,
            string actor,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(0);
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
