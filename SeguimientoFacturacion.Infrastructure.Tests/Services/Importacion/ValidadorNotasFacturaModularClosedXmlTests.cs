using ClosedXML.Excel;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
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
                        100000m,
                        new DateTime(2026, 2, 1));

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

        var validador =
            CrearValidador(
                consultaFacturas);

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

    private static
        ValidadorNotasFacturaModularClosedXml
        CrearValidador(
            IConsultaReferenciasFacturasImportacion
                consultaFacturas)
    {
        return new
            ValidadorNotasFacturaModularClosedXml(
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