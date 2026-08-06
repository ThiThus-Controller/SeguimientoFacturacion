using ClosedXML.Excel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Infrastructure.Configuration;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Services.Importacion;

public sealed class
    ValidadorGlosasModularClosedXmlTests
{
    [Fact]
    public async Task
        Validar_GlosasValidas_DebeAceptarArchivo()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        valor: 100000m,
                        fechaGlosa:
                            new DateTime(2026, 2, 1),
                        fechaRespuesta: null);

                    EscribirFila(
                        hoja,
                        fila: 3,
                        numeroFactura: "000002",
                        valor: 50000m,
                        fechaGlosa:
                            new DateTime(2026, 2, 2),
                        fechaRespuesta:
                            new DateTime(2026, 2, 5));
                });

        var consultaFacturas =
            new ConsultaFacturasControlada(
            [
                CrearReferenciaFactura(
                    "FE000001",
                    aseguradoraId: 1,
                    fechaFactura:
                        new DateOnly(2026, 1, 10)),

                CrearReferenciaFactura(
                    "FE000002",
                    aseguradoraId: 1,
                    fechaFactura:
                        new DateOnly(2026, 1, 11))
            ]);

        var resultado =
            await CrearValidador(
                    consultaFacturas)
                .ValidarAsync(
                    CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);
        Assert.Equal(2, resultado.TotalFilasAnalizadas);
        Assert.Equal(2, resultado.GlosasDetectadas);

        Assert.Equal(
            1,
            resultado.GlosasConRespuestaDetectadas);

        Assert.Empty(resultado.Inconsistencias);
        Assert.Equal(1, consultaFacturas.CantidadConsultas);
    }

    [Fact]
    public async Task
        Validar_GlosaAceptadaConFeFormula_DebeAceptarArchivo()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        valor: 100000m,
                        fechaGlosa:
                            new DateTime(2026, 2, 1),
                        fechaRespuesta:
                            new DateTime(2026, 2, 5),
                        estado: "ACEPTADA",
                        valorAceptado: 60000m,
                        feComoFormula: true));

        var resultado =
            await CrearValidador(
                    new ConsultaFacturasControlada(
                    [
                        CrearReferenciaFactura(
                            "FE000001",
                            aseguradoraId: 1,
                            fechaFactura:
                                new DateOnly(2026, 1, 10))
                    ]))
                .ValidarAsync(
                    CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);
        Assert.Equal(1, resultado.GlosasDetectadas);
        Assert.Empty(resultado.Inconsistencias);
    }

    [Fact]
    public async Task
        Validar_AceptadaSinValorAceptado_DebeRetornarError()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        valor: 100000m,
                        fechaGlosa:
                            new DateTime(2026, 2, 1),
                        fechaRespuesta:
                            new DateTime(2026, 2, 5),
                        estado: "ACEPTADA"));

        var resultado =
            await CrearValidador(
                    new ConsultaFacturasControlada(
                    [
                        CrearReferenciaFactura(
                            "FE000001",
                            aseguradoraId: 1,
                            fechaFactura:
                                new DateOnly(2026, 1, 10))
                    ]))
                .ValidarAsync(
                    CrearSolicitud(archivo));

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "VALOR_ACEPTADO_REQUERIDO");
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
                        fila: 2,
                        numeroFactura: "999999",
                        valor: 100000m,
                        fechaGlosa:
                            new DateTime(2026, 2, 1)));

        var resultado =
            await CrearValidador(
                    new ConsultaFacturasControlada([]))
                .ValidarAsync(
                    CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FACTURA_NO_EXISTE");
    }

    [Fact]
    public async Task
        Validar_AseguradoraYFechaGlosaIncoherentes_DebeRetornarErrores()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        valor: 100000m,
                        fechaGlosa:
                            new DateTime(2025, 12, 1)));

        var resultado =
            await CrearValidador(
                    new ConsultaFacturasControlada(
                    [
                        CrearReferenciaFactura(
                            "FE000001",
                            aseguradoraId: 2,
                            fechaFactura:
                                new DateOnly(2026, 1, 10))
                    ]))
                .ValidarAsync(
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
                "FECHA_GLOSA_ANTERIOR_FACTURA");
    }

    [Fact]
    public async Task
        Validar_RespuestaAnteriorGlosa_DebeRetornarError()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        valor: 100000m,
                        fechaGlosa:
                            new DateTime(2026, 2, 10),
                        fechaRespuesta:
                            new DateTime(2026, 2, 5)));

        var resultado =
            await CrearValidador(
                    new ConsultaFacturasControlada(
                    [
                        CrearReferenciaFactura(
                            "FE000001",
                            aseguradoraId: 1,
                            fechaFactura:
                                new DateOnly(2026, 1, 10))
                    ]))
                .ValidarAsync(
                    CrearSolicitud(archivo));

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "FECHA_RESPUESTA_ANTERIOR_GLOSA");
    }

    [Fact]
    public async Task
        Validar_GlosaDuplicada_DebeRetornarError()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        valor: 100000m,
                        fechaGlosa:
                            new DateTime(2026, 2, 1));

                    EscribirFila(
                        hoja,
                        fila: 3,
                        numeroFactura: "000001",
                        valor: 100000m,
                        fechaGlosa:
                            new DateTime(2026, 2, 1));
                });

        var resultado =
            await CrearValidador(
                    new ConsultaFacturasControlada(
                    [
                        CrearReferenciaFactura(
                            "FE000001",
                            aseguradoraId: 1,
                            fechaFactura:
                                new DateOnly(2026, 1, 10))
                    ]))
                .ValidarAsync(
                    CrearSolicitud(archivo));

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "GLOSA_DUPLICADA_ARCHIVO" &&
                inconsistencia.Fila == 3);
    }

    [Fact]
    public void
        DependencyInjection_DebeRegistrarValidador()
    {
        ServiceCollection services = new();

        var valoresConfiguracion =
            new Dictionary<string, string?>
            {
                [
                    $"ConnectionStrings:" +
                    $"{NombresConexion.Seguimiento}"
                ] =
                    @"Server=(localdb)\MSSQLLocalDB;" +
                    "Database=SeguimientoPruebas;" +
                    "Trusted_Connection=True;" +
                    "TrustServerCertificate=True;"
            };

        var configuration =
            new ConfigurationBuilder()
                .AddInMemoryCollection(
                    valoresConfiguracion)
                .Build();

        services.AddInfrastructure(configuration);

        var descriptor =
            services.Single(
                elemento =>
                    elemento.ServiceType ==
                    typeof(IValidadorGlosasModular));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ValidadorGlosasModularClosedXml),
            descriptor.ImplementationType);
    }

    private static
        ValidadorGlosasModularClosedXml
        CrearValidador(
            IConsultaReferenciasFacturasImportacion
                consultaFacturas)
    {
        return new
            ValidadorGlosasModularClosedXml(
                new
                    InspectorEstructuraPlantillaClosedXml(),
                new ConsultaCatalogosControlada(),
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
        decimal valor,
        DateTime fechaGlosa,
        DateTime? fechaRespuesta = null,
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
        hoja.Cell(fila, 6).Value = valor;

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

            return Task.FromResult(_referencias);
        }
    }
}
