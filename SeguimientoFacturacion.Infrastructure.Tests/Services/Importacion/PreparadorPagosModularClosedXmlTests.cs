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

public sealed class PreparadorPagosModularClosedXmlTests
{
    [Fact]
    public async Task Preparar_PagoValido_DebeAgruparAplicaciones()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        valorAplicado: 500m,
                        valorCruzadoAplicado: 500m);

                    EscribirFila(
                        hoja,
                        fila: 3,
                        numeroFactura: "000002",
                        valorAplicado: 300m,
                        valorCruzadoAplicado: 300m);
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
            await CrearPreparador(
                    consultaFacturas)
                .PrepararAsync(
                    CrearSolicitud(archivo));

        Assert.Equal(
            "PlantillaPagos.xlsx",
            resultado.NombreArchivo);

        Assert.Equal(1, resultado.TotalPagos);
        Assert.Equal(2, resultado.TotalAplicaciones);
        Assert.Equal(1000m, resultado.ValorTotalPagado);
        Assert.Equal(930m, resultado.ValorTotalCruzado);
        Assert.Equal(800m, resultado.ValorTotalAplicado);

        Assert.Equal(
            800m,
            resultado.ValorTotalCruzadoAplicado);

        Assert.Equal(
            200m,
            resultado.SaldoFavorTotalCalculado);

        Assert.Equal(
            130m,
            resultado
                .SaldoCruzadoPendienteTotalCalculado);

        Assert.Equal(
            0,
            resultado.TotalPagosDescuadrados);

        var pago =
            Assert.Single(resultado.Pagos);

        Assert.Equal(1, pago.AseguradoraId);
        Assert.Equal("RC-001", pago.Recibo);

        Assert.Equal(
            new DateOnly(2026, 3, 1),
            pago.FechaPago);

        Assert.Equal(2, pago.Aplicaciones.Count);

        Assert.Contains(
            pago.Aplicaciones,
            aplicacion =>
                aplicacion.FilaOrigen == 2 &&
                aplicacion.IdentificadorFe ==
                "FE000001" &&
                aplicacion.ValorAplicado == 500m);

        Assert.Contains(
            pago.Aplicaciones,
            aplicacion =>
                aplicacion.FilaOrigen == 3 &&
                aplicacion.IdentificadorFe ==
                "FE000002" &&
                aplicacion.ValorAplicado == 300m);

        Assert.Equal(
            2,
            consultaFacturas.CantidadConsultas);
    }

    [Fact]
    public async Task Preparar_DosRecibos_DebeCrearDosPagos()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                {
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        recibo: "RC-001",
                        valorAplicado: 800m,
                        valorCruzadoAplicado: 800m);

                    EscribirFila(
                        hoja,
                        fila: 3,
                        numeroFactura: "000002",
                        recibo: "RC-002",
                        valorPagado: 500m,
                        valorCruzado: 470m,
                        retencion: 20m,
                        reteIca: 10m,
                        saldoFavor: 0m,
                        saldoRetencion: 0m,
                        valorAplicado: 500m,
                        valorCruzadoAplicado: 470m);
                });

        var resultado =
            await CrearPreparador(
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
                    ]))
                .PrepararAsync(
                    CrearSolicitud(archivo));

        Assert.Equal(2, resultado.TotalPagos);
        Assert.Equal(2, resultado.TotalAplicaciones);
        Assert.Equal(1500m, resultado.ValorTotalPagado);
        Assert.Equal(1400m, resultado.ValorTotalCruzado);
        Assert.Equal(1300m, resultado.ValorTotalAplicado);

        Assert.Contains(
            resultado.Pagos,
            pago =>
                pago.Recibo == "RC-001");

        Assert.Contains(
            resultado.Pagos,
            pago =>
                pago.Recibo == "RC-002");
    }

    [Fact]
    public async Task Preparar_ArchivoInvalido_DebeRechazarlo()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "000001",
                        valorPagado: 1000m,
                        valorCruzado: 900m,
                        retencion: 50m,
                        reteIca: 20m,
                        valorAplicado: 800m,
                        valorCruzadoAplicado: 800m));

        var preparador =
            CrearPreparador(
                new ConsultaFacturasControlada(
                [
                    CrearReferenciaFactura(
                        "FE000001",
                        1,
                        new DateOnly(2026, 1, 10))
                ]));

        async Task Accion()
        {
            await preparador.PrepararAsync(
                CrearSolicitud(archivo));
        }

        var excepcion =
            await Assert.ThrowsAsync<
                InvalidOperationException>(Accion);

        Assert.Contains(
            "error(es) bloqueante(s)",
            excepcion.Message);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarPreparador()
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
                    typeof(IPreparadorPagosModular));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(PreparadorPagosModularClosedXml),
            descriptor.ImplementationType);
    }

    private static PreparadorPagosModularClosedXml
        CrearPreparador(
            IConsultaReferenciasFacturasImportacion
                consultaFacturas)
    {
        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var validador =
            new ValidadorPagosModularClosedXml(
                inspector,
                new ConsultaCatalogosControlada(),
                consultaFacturas);

        return new PreparadorPagosModularClosedXml(
            validador,
            inspector,
            consultaFacturas);
    }

    private static SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo = "PlantillaPagos.xlsx",
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
                    .Pagos
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
        string recibo = "RC-001",
        decimal valorPagado = 1000m,
        decimal valorCruzado = 930m,
        decimal retencion = 50m,
        decimal reteIca = 20m,
        decimal saldoFavor = 200m,
        decimal saldoRetencion = 130m,
        decimal valorAplicado = 800m,
        decimal valorCruzadoAplicado = 800m)
    {
        hoja.Cell(fila, 1).Value =
            $"FE{numeroFactura}";

        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = numeroFactura;
        hoja.Cell(fila, 4).Value = "NUEVA EPS";
        hoja.Cell(fila, 5).Value = valorPagado;
        hoja.Cell(fila, 6).Value = valorCruzado;
        hoja.Cell(fila, 7).Value = retencion;
        hoja.Cell(fila, 8).Value = reteIca;
        hoja.Cell(fila, 9).Value = saldoFavor;
        hoja.Cell(fila, 10).Value = saldoRetencion;
        hoja.Cell(fila, 11).Value = valorAplicado;
        hoja.Cell(fila, 12).Value =
            valorCruzadoAplicado;

        hoja.Cell(fila, 13).Value =
            new DateTime(2026, 3, 1);

        hoja.Cell(fila, 14).Value = recibo;
        hoja.Cell(fila, 15).Value = "Pago de prueba.";
    }

    private sealed class ConsultaCatalogosControlada :
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
                        new ReferenciaCatalogoImportacionDto
                        {
                            Id = 1,
                            Valor = "NUEVA EPS"
                        }
                    ]
                });
        }
    }

    private sealed class ConsultaFacturasControlada :
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

            var solicitados =
                facturaIds.ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

            IReadOnlyCollection<
                ReferenciaFacturaImportacionDto>
                resultado =
                    _referencias
                        .Where(
                            referencia =>
                                solicitados.Contains(
                                    referencia.FacturaId))
                        .ToArray();

            return Task.FromResult(resultado);
        }
    }
}