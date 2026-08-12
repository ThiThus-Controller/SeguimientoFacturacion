using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Domain.Constants;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class
    ValidadorNotasFacturaModularClosedXmlTests
{
    [Fact]
    public async Task
        Validar_NotasValidas_DebeAceptarArchivo()
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
                        "NC-001",
                        10000m,
                        new DateTime(2026, 7, 20),
                        new DateTime(2026, 7, 15),
                        26535m);

                    EscribirFila(
                        hoja,
                        3,
                        "000002",
                        "ND",
                        "ND-001",
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

        var validador = CrearValidador(
            consultaFacturas,
            new ConsultaGlosasControlada(
                [CrearReferenciaGlosa(0m)]));

        var resultado =
            await validador.ValidarAsync(
                CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);
        Assert.Equal(2, resultado.TotalFilasAnalizadas);
        Assert.Equal(2, resultado.NotasDetectadas);
        Assert.Equal(1, resultado.NotasCreditoDetectadas);
        Assert.Equal(1, resultado.NotasDebitoDetectadas);
        Assert.Empty(resultado.Inconsistencias);
        Assert.Equal(1, consultaFacturas.CantidadConsultas);
    }

    [Fact]
    public async Task Validar_NotaCreditoSinGlosa_DebeRetornarError()
    {
        await using var archivo = CrearArchivo(
            hoja => EscribirFila(
                hoja,
                fila: 2,
                numeroFactura: "000001",
                tipo: "NC",
                numeroNota: "NC-SIN-GLOSA",
                valor: 10000m,
                fecha: new DateTime(2026, 7, 20)));

        var resultado = await CrearValidador(
                new ConsultaFacturasControlada(
                [
                    CrearReferenciaFactura(
                        "FE000001",
                        1,
                        new DateOnly(2026, 7, 10))
                ]))
            .ValidarAsync(CrearSolicitud(archivo));

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia => inconsistencia.Codigo ==
                "NOTA_CREDITO_REQUIERE_GLOSA");
    }

    [Fact]
    public async Task
        Validar_FacturaInexistente_DebeRetornarError()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        2,
                        "999999",
                        "NC",
                        "NC-001",
                        100000m,
                        new DateTime(2026, 2, 1)));

        var validador =
            CrearValidador(
                new ConsultaFacturasControlada([]));

        var resultado =
            await validador.ValidarAsync(
                CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FACTURA_NO_EXISTE");
    }

    [Theory]
    [InlineData(
        CodigosEstadoFactura.AnuladaHistorica,
        "NC")]
    [InlineData(
        CodigosEstadoFactura.AnuladaHistorica,
        "ND")]
    [InlineData(
        CodigosEstadoFactura.Anulada,
        "NC")]
    [InlineData(
        CodigosEstadoFactura.Anulada,
        "ND")]
    public async Task
        Validar_FacturaAnulada_DebeRechazarNota(
            int estadoId,
            string tipoNota)
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        2,
                        "000001",
                        tipoNota,
                        $"{tipoNota}-001",
                        100000m,
                        new DateTime(2026, 2, 1)));

        var validador =
            CrearValidador(
                new ConsultaFacturasControlada(
                [
                    CrearReferenciaFactura(
                        "FE000001",
                        1,
                        new DateOnly(2026, 1, 10),
                        estadoId)
                ]));

        var resultado =
            await validador.ValidarAsync(
                CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FACTURA_ANULADA_NO_PERMITE_NOTA" &&
                inconsistencia.Fila == 2 &&
                inconsistencia.Columna == "FE");
    }

    [Fact]
    public async Task
        Validar_AseguradoraYFechaIncoherentes_DebeRetornarErrores()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        2,
                        "000001",
                        "NC",
                        "NC-001",
                        100000m,
                        new DateTime(2025, 12, 1)));

        var validador =
            CrearValidador(
                new ConsultaFacturasControlada(
                [
                    CrearReferenciaFactura(
                        "FE000001",
                        2,
                        new DateOnly(2026, 1, 10))
                ]));

        var resultado =
            await validador.ValidarAsync(
                CrearSolicitud(archivo));

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "ASEGURADORA_NO_COINCIDE_FACTURA");

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FECHA_NOTA_ANTERIOR_FACTURA");
    }

    [Fact]
    public async Task
        Validar_NotaDuplicada_DebeRetornarError()
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
                        "NC-001",
                        100000m,
                        new DateTime(2026, 2, 1));

                    EscribirFila(
                        hoja,
                        3,
                        "000001",
                        "CREDITO",
                        "NC-001",
                        100000m,
                        new DateTime(2026, 2, 1));
                });

        var validador =
            CrearValidador(
                new ConsultaFacturasControlada(
                [
                    CrearReferenciaFactura(
                        "FE000001",
                        1,
                        new DateOnly(2026, 1, 10))
                ]));

        var resultado =
            await validador.ValidarAsync(
                CrearSolicitud(archivo));

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "NOTA_DUPLICADA_ARCHIVO" &&
                inconsistencia.Fila == 3);
    }

    [Fact]
    public async Task
        Validar_NotaAsociadaNoExcedeAceptado_DebeSerValida()
    {
        await using var archivo = CrearArchivo(
            hoja => EscribirFila(
                hoja,
                fila: 2,
                numeroFactura: "000001",
                tipo: "NC",
                numeroNota: "NC-GLOSA-001",
                valor: 10000m,
                fecha: new DateTime(2026, 7, 20),
                fechaGlosa: new DateTime(2026, 7, 15),
                valorGlosa: 26535m));

        var validador = CrearValidador(
            new ConsultaFacturasControlada(
                [
                    CrearReferenciaFactura(
                        "FE000001",
                        1,
                        new DateOnly(2026, 7, 10))
                ]),
            new ConsultaGlosasControlada(
                [CrearReferenciaGlosa(5000m)]));

        var resultado = await validador.ValidarAsync(
            CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);
    }

    [Fact]
    public async Task
        Validar_NotaAsociadaExcedeAceptado_DebeReportarError()
    {
        await using var archivo = CrearArchivo(
            hoja => EscribirFila(
                hoja,
                fila: 2,
                numeroFactura: "000001",
                tipo: "NC",
                numeroNota: "NC-GLOSA-002",
                valor: 11000m,
                fecha: new DateTime(2026, 7, 20),
                fechaGlosa: new DateTime(2026, 7, 15),
                valorGlosa: 26535m));

        var validador = CrearValidador(
            new ConsultaFacturasControlada(
                [
                    CrearReferenciaFactura(
                        "FE000001",
                        1,
                        new DateOnly(2026, 7, 10))
                ]),
            new ConsultaGlosasControlada(
                [CrearReferenciaGlosa(6000m)]));

        var resultado = await validador.ValidarAsync(
            CrearSolicitud(archivo));

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "NC_EXCEDE_VALOR_ACEPTADO_GLOSA");
    }

    private static
        ValidadorNotasFacturaModularClosedXml
        CrearValidador(
            IConsultaReferenciasFacturasImportacion
                consultaFacturas,
            IConsultaGlosasNotasCredito?
                consultaGlosas = null)
    {
        return new
            ValidadorNotasFacturaModularClosedXml(
                new
                    InspectorEstructuraPlantillaClosedXml(),

                new ConsultaCatalogosControlada(),

                consultaFacturas,

                consultaGlosas ??
                    new ConsultaGlosasControlada());
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
            DateOnly fechaFactura,
            int estadoId =
                CodigosEstadoFactura.Activa)
    {
        return new ReferenciaFacturaImportacionDto
        {
            FacturaId = facturaId,
            AseguradoraId = aseguradoraId,
            FechaFactura = fechaFactura,
            EstadoId = estadoId
        };
    }

    private static ReferenciaGlosaNotaCreditoDto
        CrearReferenciaGlosa(decimal notasPrevias)
    {
        return new ReferenciaGlosaNotaCreditoDto
        {
            GlosaId = Guid.NewGuid(),
            FacturaId = "FE000001",
            FechaGlosa = new DateOnly(2026, 7, 15),
            ValorGlosa = 26535m,
            ValorAceptado = 15921m,
            TotalNotasCreditoVigentes = notasPrevias
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
        DateTime fecha,
        DateTime? fechaGlosa = null,
        decimal? valorGlosa = null)
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

        if (fechaGlosa.HasValue)
        {
            hoja.Cell(fila, 9).Value =
                fechaGlosa.Value;
        }

        if (valorGlosa.HasValue)
        {
            hoja.Cell(fila, 10).Value =
                valorGlosa.Value;
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

    private sealed class ConsultaGlosasControlada :
        IConsultaGlosasNotasCredito
    {
        private readonly IReadOnlyCollection<
            ReferenciaGlosaNotaCreditoDto> _referencias;

        public ConsultaGlosasControlada(
            IReadOnlyCollection<
                ReferenciaGlosaNotaCreditoDto>? referencias = null)
        {
            _referencias = referencias ?? [];
        }

        public Task<IReadOnlyCollection<
            ReferenciaGlosaNotaCreditoDto>>
            ObtenerPorFacturasAsync(
                IReadOnlyCollection<string> facturaIds,
                CancellationToken cancellationToken = default)
        {
            var solicitados = facturaIds.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

            return Task.FromResult<IReadOnlyCollection<
                ReferenciaGlosaNotaCreditoDto>>(
                    _referencias
                        .Where(referencia =>
                            solicitados.Contains(
                                referencia.FacturaId))
                        .ToArray());
        }

        public Task<int> PrepararControlConcurrenciaAsync(
            IReadOnlyCollection<Guid> glosaIds,
            DateTimeOffset fecha,
            string actor,
            CancellationToken cancellationToken = default)
        {
            var disponibles = _referencias
                .Select(referencia => referencia.GlosaId)
                .ToHashSet();

            return Task.FromResult(
                glosaIds.Distinct().Count(disponibles.Contains));
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

            return Task.FromResult(
                _referencias);
        }
    }
}
