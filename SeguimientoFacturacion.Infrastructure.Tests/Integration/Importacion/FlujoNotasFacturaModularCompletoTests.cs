using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Importacion;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Integration.Importacion;

/// <summary>
/// Prueba integral del flujo modular de notas,
/// desde el Excel hasta la tabla definitiva.
/// </summary>
public sealed class
    FlujoNotasFacturaModularCompletoTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        FlujoCompleto_ArchivoValido_DebeImportarNotas()
    {
        await using var contexto = CrearContexto();

        var loteId =
            await CrearDatosBaseAsync(contexto);

        var repositorioImportaciones =
            new RepositorioImportacionesEfCore(contexto);

        var repositorioNotasTemporales =
            new
                RepositorioNotasFacturaTemporalesImportacionEfCore(
                    contexto);

        var consultaFacturas =
            new
                ConsultaReferenciasFacturasImportacionEfCore(
                    contexto);

        var servicioAnalisis =
            CrearServicioAnalisis(
                contexto,
                repositorioImportaciones,
                repositorioNotasTemporales,
                consultaFacturas);

        await using var archivo =
            CrearArchivoValido();

        var resultadoAnalisis =
            await servicioAnalisis.AnalizarYPrepararAsync(
                loteId,
                new SolicitudAnalisisImportacionDto
                {
                    NombreArchivo =
                        "NotasFactura.xlsx",

                    Contenido = archivo
                },
                "usuario-analisis");

        Assert.True(
            resultadoAnalisis.Validacion.EsValido);

        Assert.Equal(
            2,
            resultadoAnalisis.TotalNotasTemporales);

        var servicioConfirmacion =
            new ServicioConfirmacionLoteImportacion(
                repositorioImportaciones,
                new
                    RepositorioFacturasTemporalesImportacionEfCore(
                        contexto),
                repositorioNotasTemporales,
                new
                    RepositorioGlosasTemporalesImportacionEfCore(
                        contexto),
                new
                    RepositorioPagosTemporalesImportacionEfCore(
                        contexto),
                contexto,
                new
                    SolicitudConfirmacionLoteImportacionDtoValidator(),
                new ProveedorTiempoFalso(
                    CrearFecha(13)));

        var resultadoConfirmacion =
            await servicioConfirmacion.ConfirmarAsync(
                new
                    SolicitudConfirmacionLoteImportacionDto
                {
                    LoteId = loteId,
                    Tipo = TipoImportacion.NotasFactura,
                    Usuario = "usuario-confirmacion"
                });

        Assert.Equal(
            EstadoImportacion.Confirmada,
            resultadoConfirmacion.Estado);

        var servicioProcesamiento =
            new ServicioProcesamientoLoteNotasFactura(
                repositorioImportaciones,
                repositorioNotasTemporales,
                new
                    RepositorioPersistenciaNotasFacturaImportacionEfCore(
                        contexto),
                consultaFacturas,
                contexto,
                new
                    SolicitudProcesamientoLoteNotasFacturaDtoValidator(),
                new ProveedorTiempoFalso(
                    CrearFecha(14)));

        var resultadoProcesamiento =
            await servicioProcesamiento.ProcesarAsync(
                new
                    SolicitudProcesamientoLoteNotasFacturaDto
                {
                    LoteId = loteId,
                    Usuario = "usuario-procesamiento"
                });

        contexto.ChangeTracker.Clear();

        var lotePersistido =
            await contexto.LotesImportacion
                .AsNoTracking()
                .SingleAsync(
                    lote => lote.Id == loteId);

        var notasDefinitivas =
            await contexto.NotasFactura
                .AsNoTracking()
                .OrderBy(nota => nota.Numero)
                .ToListAsync();

        var totalStaging =
            await contexto
                .NotasFacturaTemporalesImportacion
                .AsNoTracking()
                .CountAsync(
                    nota =>
                        nota.LoteImportacionId ==
                        loteId);

        Assert.Equal(
            EstadoImportacion.Completada,
            lotePersistido.Estado);

        Assert.Equal(
            "usuario-procesamiento",
            lotePersistido.ModificadoPor);

        Assert.Equal(0, totalStaging);
        Assert.Equal(2, notasDefinitivas.Count);

        var notaCredito =
            Assert.Single(
                notasDefinitivas,
                nota =>
                    nota.Tipo ==
                    TipoNotaFactura.Credito);

        Assert.Equal("NC-001", notaCredito.Numero);
        Assert.Equal(100000m, notaCredito.Valor);
        Assert.Equal(-100000m, notaCredito.ImpactoSaldo);

        var notaDebito =
            Assert.Single(
                notasDefinitivas,
                nota =>
                    nota.Tipo ==
                    TipoNotaFactura.Debito);

        Assert.Equal("ND-001", notaDebito.Numero);
        Assert.Equal(50000m, notaDebito.Valor);
        Assert.Equal(50000m, notaDebito.ImpactoSaldo);

        Assert.All(
            notasDefinitivas,
            nota =>
                Assert.Equal(
                    "usuario-procesamiento",
                    nota.CreadoPor));

        Assert.Equal(
            EstadoImportacion.Completada,
            resultadoProcesamiento.Estado);

        Assert.Equal(
            2,
            resultadoProcesamiento.TotalNotasStaging);

        Assert.Equal(
            2,
            resultadoProcesamiento.TotalNotasImportadas);

        Assert.Equal(
            0,
            resultadoProcesamiento.TotalNotasOmitidas);

        Assert.Equal(
            -50000m,
            resultadoProcesamiento.ImpactoNetoImportado);
    }

    private static
        ServicioAnalisisStagingNotasFactura
        CrearServicioAnalisis(
            SeguimientoDbContext contexto,
            RepositorioImportacionesEfCore
                repositorioImportaciones,
            RepositorioNotasFacturaTemporalesImportacionEfCore
                repositorioTemporal,
            ConsultaReferenciasFacturasImportacionEfCore
                consultaFacturas)
    {
        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var consultaCatalogos =
            new ConsultaCatalogosImportacionEfCore(
                contexto);

        var validador =
            new ValidadorNotasFacturaModularClosedXml(
                inspector,
                consultaCatalogos,
                consultaFacturas);

        var preparador =
            new PreparadorNotasFacturaModularClosedXml(
                validador,
                inspector,
                consultaFacturas);

        var registroAnalisis =
            new ServicioRegistroAnalisisLote(
                repositorioImportaciones,
                contexto,
                new ProveedorTiempoFalso(
                    CrearFecha(12)));

        return new
            ServicioAnalisisStagingNotasFactura(
                validador,
                preparador,
                repositorioImportaciones,
                repositorioTemporal,
                registroAnalisis);
    }

    private static async Task<Guid>
        CrearDatosBaseAsync(
            SeguimientoDbContext contexto)
    {
        var aseguradora =
            new Aseguradora(
                id: 1,
                descripcion: "NUEVA EPS");

        var factura =
            new Factura(
                prefijo: "FE",
                numero: "000001",
                fechaFactura:
                    new DateOnly(2026, 1, 10),
                aseguradoraId: 1,
                valor: 500000m,
                fechaRadicacion:
                    new DateOnly(2026, 1, 15),
                tipoDocumentoId: 1,
                numeroDocumento: "123456",
                nombreCompleto:
                    "PACIENTE DE INTEGRACION",
                atencionId: 1,
                costoId: 1,
                numeroAdmision: "ADM000001",
                fechaAdmision:
                    new DateOnly(2026, 1, 5),
                estadoId: 1,
                facturadorId: 1);

        var lote =
            new LoteImportacion(
                TipoImportacion.NotasFactura,
                "NotasFactura.xlsx",
                HashValido);

        factura.RegistrarCreacion(
            CrearFecha(9),
            "usuario-pruebas");

        lote.RegistrarCreacion(
            CrearFecha(10),
            "usuario-pruebas");

        await contexto.Aseguradoras.AddAsync(
            aseguradora);

        await contexto.Facturas.AddAsync(factura);

        await contexto.LotesImportacion.AddAsync(
            lote);

        await contexto.GuardarCambiosAsync();

        contexto.ChangeTracker.Clear();

        return lote.Id;
    }

    private static MemoryStream CrearArchivoValido()
    {
        var contenido = new MemoryStream();

        using (var libro = new XLWorkbook())
        {
            var hoja =
                libro.Worksheets.Add("Notas");

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

            EscribirFila(
                hoja,
                fila: 2,
                tipo: "NC",
                numeroNota: "NC-001",
                valor: 100000m,
                fecha:
                    new DateTime(2026, 2, 1));

            EscribirFila(
                hoja,
                fila: 3,
                tipo: "ND",
                numeroNota: "ND-001",
                valor: 50000m,
                fecha:
                    new DateTime(2026, 2, 2));

            libro.SaveAs(contenido);
        }

        contenido.Position = 0;

        return contenido;
    }

    private static void EscribirFila(
        IXLWorksheet hoja,
        int fila,
        string tipo,
        string numeroNota,
        decimal valor,
        DateTime fecha)
    {
        hoja.Cell(fila, 1).Value = "FE000001";
        hoja.Cell(fila, 2).Value = "FE";
        hoja.Cell(fila, 3).Value = "000001";
        hoja.Cell(fila, 4).Value = "NUEVA EPS";
        hoja.Cell(fila, 5).Value = tipo;
        hoja.Cell(fila, 6).Value = fecha;
        hoja.Cell(fila, 7).Value = numeroNota;
        hoja.Cell(fila, 8).Value = valor;
    }

    private static SeguimientoDbContext
        CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"FlujoNotasCompleto_" +
                    $"{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static DateTimeOffset CrearFecha(
        int hora)
    {
        return new DateTimeOffset(
            2026,
            7,
            30,
            hora,
            0,
            0,
            TimeSpan.Zero);
    }

    private sealed class ProveedorTiempoFalso :
        TimeProvider
    {
        private readonly DateTimeOffset _fechaUtc;

        public ProveedorTiempoFalso(
            DateTimeOffset fechaUtc)
        {
            _fechaUtc = fechaUtc;
        }

        public override DateTimeOffset GetUtcNow()
        {
            return _fechaUtc;
        }
    }
}
