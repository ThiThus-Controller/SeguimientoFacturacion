using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;
using SeguimientoFacturacion.Infrastructure.Services.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Tests
    .Integration.Importacion;

/// <summary>
/// Pruebas integradas del análisis y staging
/// modular de notas crédito y débito.
/// </summary>
public sealed class
    FlujoNotasFacturaModularStagingTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        Analizar_ArchivoValido_DebePersistirStagingYLote()
    {
        await using var contexto =
            CrearContexto();

        var loteId =
            await CrearDatosBaseAsync(contexto);

        var servicio =
            CrearServicio(contexto);

        await using var archivo =
            CrearArchivoValido();

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                loteId,
                CrearSolicitud(archivo),
                " usuario-integracion ");

        var lotePersistido =
            await contexto.LotesImportacion
                .AsNoTracking()
                .SingleAsync(
                    lote =>
                        lote.Id == loteId);

        var notasTemporales =
            await contexto
                .NotasFacturaTemporalesImportacion
                .AsNoTracking()
                .Where(
                    nota =>
                        nota.LoteImportacionId ==
                        loteId)
                .OrderBy(nota => nota.FilaOrigen)
                .ToListAsync();

        var inconsistencias =
            await contexto.InconsistenciasImportacion
                .AsNoTracking()
                .Where(
                    inconsistencia =>
                        inconsistencia
                            .LoteImportacionId ==
                        loteId)
                .ToListAsync();

        Assert.Equal(
            EstadoImportacion.Analizada,
            lotePersistido.Estado);

        Assert.Equal(
            2,
            lotePersistido.TotalFilas);

        Assert.Equal(
            2,
            lotePersistido.TotalFilasValidas);

        Assert.Equal(
            0,
            lotePersistido.TotalFilasConError);

        Assert.Equal(
            0,
            lotePersistido.TotalErrores);

        Assert.True(
            lotePersistido.PuedeConfirmarse);

        Assert.Equal(
            2,
            notasTemporales.Count);

        Assert.Empty(inconsistencias);

        var notaCredito =
            Assert.Single(
                notasTemporales,
                nota =>
                    nota.Tipo ==
                    TipoNotaFactura.Credito);

        Assert.Equal(
            "FE000001",
            notaCredito.IdentificadorFe);

        Assert.Equal(
            "NC-001",
            notaCredito.NumeroNota);

        Assert.Equal(
            100000m,
            notaCredito.ValorNota);

        Assert.Equal(
            -100000m,
            notaCredito.ImpactoSaldo);

        var notaDebito =
            Assert.Single(
                notasTemporales,
                nota =>
                    nota.Tipo ==
                    TipoNotaFactura.Debito);

        Assert.Equal(
            "ND-001",
            notaDebito.NumeroNota);

        Assert.Equal(
            50000m,
            notaDebito.ValorNota);

        Assert.Equal(
            50000m,
            notaDebito.ImpactoSaldo);

        Assert.True(resultado.Validacion.EsValido);

        Assert.Equal(
            2,
            resultado.TotalNotasTemporales);

        Assert.Equal(
            1,
            resultado.TotalNotasCreditoTemporales);

        Assert.Equal(
            1,
            resultado.TotalNotasDebitoTemporales);

        Assert.Equal(
            -50000m,
            resultado.ImpactoNetoSaldo);

        Assert.True(
            resultado.Lote.PuedeConfirmarse);
    }

    [Fact]
    public async Task
        Analizar_ArchivoInvalido_DebeEliminarStagingYGuardarError()
    {
        await using var contexto =
            CrearContexto();

        var loteId =
            await CrearDatosBaseAsync(contexto);

        var registroAnterior =
            CrearNotaTemporalAnterior(
                loteId);

        await contexto
            .NotasFacturaTemporalesImportacion
            .AddAsync(registroAnterior);

        await contexto.GuardarCambiosAsync();

        contexto.ChangeTracker.Clear();

        var servicio =
            CrearServicio(contexto);

        await using var archivo =
            CrearArchivoConTipoInvalido();

        var resultado =
            await servicio.AnalizarYPrepararAsync(
                loteId,
                CrearSolicitud(archivo),
                "usuario-integracion");

        var lotePersistido =
            await contexto.LotesImportacion
                .AsNoTracking()
                .SingleAsync(
                    lote =>
                        lote.Id == loteId);

        var totalStaging =
            await contexto
                .NotasFacturaTemporalesImportacion
                .AsNoTracking()
                .CountAsync(
                    nota =>
                        nota.LoteImportacionId ==
                        loteId);

        var inconsistencias =
            await contexto.InconsistenciasImportacion
                .AsNoTracking()
                .Where(
                    inconsistencia =>
                        inconsistencia
                            .LoteImportacionId ==
                        loteId)
                .ToListAsync();

        Assert.Equal(
            EstadoImportacion.Analizada,
            lotePersistido.Estado);

        Assert.Equal(
            1,
            lotePersistido.TotalFilas);

        Assert.Equal(
            1,
            lotePersistido.TotalFilasConError);

        Assert.False(
            lotePersistido.PuedeConfirmarse);

        Assert.Equal(0, totalStaging);

        Assert.Contains(
            inconsistencias,
            inconsistencia =>
                inconsistencia.Codigo ==
                "TIPO_NOTA_INVALIDO");

        Assert.False(
            resultado.Validacion.EsValido);

        Assert.Equal(
            0,
            resultado.TotalNotasTemporales);

        Assert.False(
            resultado.Lote.PuedeConfirmarse);
    }

    private static
        ServicioAnalisisStagingNotasFactura
        CrearServicio(
            SeguimientoDbContext contexto)
    {
        var repositorioImportaciones =
            new RepositorioImportacionesEfCore(
                contexto);

        var inspector =
            new InspectorEstructuraPlantillaClosedXml();

        var consultaCatalogos =
            new ConsultaCatalogosImportacionEfCore(
                contexto);

        var consultaFacturas =
            new
                ConsultaReferenciasFacturasImportacionEfCore(
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

        var repositorioTemporal =
            new
                RepositorioNotasFacturaTemporalesImportacionEfCore(
                    contexto);

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
            CrearFactura();

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

        await contexto.Aseguradoras
            .AddAsync(aseguradora);

        await contexto.Facturas
            .AddAsync(factura);

        await contexto.LotesImportacion
            .AddAsync(lote);

        await contexto.GuardarCambiosAsync();

        contexto.ChangeTracker.Clear();

        return lote.Id;
    }

    private static Factura CrearFactura()
    {
        return new Factura(
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
    }

    private static
        NotaFacturaImportacionTemporal
        CrearNotaTemporalAnterior(
            Guid loteId)
    {
        return new
            NotaFacturaImportacionTemporal(
                loteImportacionId: loteId,
                hojaOrigen: "Notas",
                filaOrigen: 2,
                identificadorFe: "FE000001",
                prefijo: "FE",
                numeroFactura: "000001",
                aseguradoraId: 1,
                tipo: TipoNotaFactura.Credito,
                fechaNota:
                    new DateOnly(2026, 2, 1),
                numeroNota: "NC-ANTERIOR",
                valorNota: 25000m);
    }

    private static
        SolicitudAnalisisImportacionDto
        CrearSolicitud(Stream contenido)
    {
        return new SolicitudAnalisisImportacionDto
        {
            NombreArchivo =
                "NotasFactura.xlsx",

            Contenido =
                contenido
        };
    }

    private static MemoryStream
        CrearArchivoValido()
    {
        return CrearArchivo(
            hoja =>
            {
                EscribirFila(
                    hoja,
                    fila: 2,
                    tipo: "NC",
                    numeroNota: "NC-001",
                    valor: 100000m,
                    fecha:
                        new DateTime(
                            2026,
                            2,
                            1));

                EscribirFila(
                    hoja,
                    fila: 3,
                    tipo: "ND",
                    numeroNota: "ND-001",
                    valor: 50000m,
                    fecha:
                        new DateTime(
                            2026,
                            2,
                            2));
            });
    }

    private static MemoryStream
        CrearArchivoConTipoInvalido()
    {
        return CrearArchivo(
            hoja =>
                EscribirFila(
                    hoja,
                    fila: 2,
                    tipo: "TIPO DESCONOCIDO",
                    numeroNota: "NT-001",
                    valor: 100000m,
                    fecha:
                        new DateTime(
                            2026,
                            2,
                            1)));
    }

    private static MemoryStream CrearArchivo(
        Action<IXLWorksheet> configurar)
    {
        var contenido =
            new MemoryStream();

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

            configurar(hoja);

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
        hoja.Cell(fila, 1).Value =
            "FE000001";

        hoja.Cell(fila, 2).Value =
            "FE";

        hoja.Cell(fila, 3).Value =
            "000001";

        hoja.Cell(fila, 4).Value =
            "NUEVA EPS";

        hoja.Cell(fila, 5).Value =
            tipo;

        hoja.Cell(fila, 6).Value =
            fecha;

        hoja.Cell(fila, 7).Value =
            numeroNota;

        hoja.Cell(fila, 8).Value =
            valor;
    }

    private static SeguimientoDbContext
        CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"FlujoNotasStaging_" +
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