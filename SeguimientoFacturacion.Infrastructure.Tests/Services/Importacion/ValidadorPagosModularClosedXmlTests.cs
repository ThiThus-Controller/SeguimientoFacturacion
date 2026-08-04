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

public sealed class ValidadorPagosModularClosedXmlTests
{
    [Fact]
    public async Task Validar_PagoValido_DebeAceptarArchivo()
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
            await CrearValidador(
                    consultaFacturas)
                .ValidarAsync(
                    CrearSolicitud(archivo));

        Assert.True(resultado.EsValido);
        Assert.Equal(2, resultado.TotalFilasAnalizadas);
        Assert.Equal(1, resultado.PagosDetectados);
        Assert.Equal(2, resultado.AplicacionesDetectadas);
        Assert.Equal(0, resultado.CatalogosNoMapeados);
        Assert.Empty(resultado.Inconsistencias);
        Assert.Equal(1, consultaFacturas.CantidadConsultas);
    }

    [Fact]
    public async Task Validar_Descuadres_DebeRetornarErrores()
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
                        saldoFavor: 150m,
                        saldoRetencion: 100m,
                        valorAplicado: 800m,
                        valorCruzadoAplicado: 700m));

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

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "PAGO_DESCUADRADO");

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "SALDO_FAVOR_NO_COINCIDE");

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "SALDO_RETENCION_NO_COINCIDE");
    }

    [Fact]
    public async Task Validar_FacturaInexistente_DebeRetornarError()
    {
        await using var archivo =
            CrearArchivo(
                hoja =>
                    EscribirFila(
                        hoja,
                        fila: 2,
                        numeroFactura: "999999",
                        valorAplicado: 800m,
                        valorCruzadoAplicado: 800m));

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
    public async Task Validar_DatosReciboDiferentes_DebeRetornarError()
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
                        fechaPago:
                            new DateTime(2026, 3, 2),
                        valorPagado: 1100m,
                        valorAplicado: 300m,
                        valorCruzadoAplicado: 300m);
                });

        var resultado =
            await CrearValidador(
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
                    ]))
                .ValidarAsync(
                    CrearSolicitud(archivo));

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "DATOS_PAGO_INCONSISTENTES" &&
                inconsistencia.Columna ==
                "FECHA DE PAGO");

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "DATOS_PAGO_INCONSISTENTES" &&
                inconsistencia.Columna ==
                "VALOR PAGADO");
    }

    [Fact]
    public async Task Validar_AplicacionDuplicada_DebeRetornarError()
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
                        numeroFactura: "000001",
                        valorAplicado: 300m,
                        valorCruzadoAplicado: 300m);
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

        Assert.False(resultado.EsValido);

        Assert.Contains(
            resultado.Inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "APLICACION_PAGO_DUPLICADA" &&
                inconsistencia.Fila == 3);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarValidador()
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
                    typeof(IValidadorPagosModular));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ValidadorPagosModularClosedXml),
            descriptor.ImplementationType);
    }

    private static ValidadorPagosModularClosedXml
        CrearValidador(
            IConsultaReferenciasFacturasImportacion
                consultaFacturas)
    {
        return new ValidadorPagosModularClosedXml(
            new InspectorEstructuraPlantillaClosedXml(),
            new ConsultaCatalogosControlada(),
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
        DateTime? fechaPago = null,
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
            fechaPago ??
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