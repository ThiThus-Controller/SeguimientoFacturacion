using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Importacion;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Services.Importacion;

/// <summary>
/// Pruebas integradas del procesamiento definitivo
/// de lotes de facturas.
/// </summary>
public sealed class
    ServicioProcesamientoLoteFacturasIntegrationTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task
        Procesar_LoteConfirmado_DebePersistirTodoYEliminarStaging()
    {
        await using var contexto = CrearContexto();

        var lote =
            await CrearLoteConfirmadoAsync(contexto);

        await CrearStagingAsync(
            contexto,
            lote.Id,
            identificadorFe: "FE000001",
            prefijo: "FE",
            numero: "000001");

        var servicio = CrearServicio(contexto);

        var resultado =
            await servicio.ProcesarAsync(
                CrearSolicitud(lote.Id));

        var lotePersistido =
            await contexto.LotesImportacion
                .AsNoTracking()
                .SingleAsync(
                    elemento => elemento.Id == lote.Id);

        var pacientes =
            await contexto.Pacientes
                .AsNoTracking()
                .ToListAsync();

        var facturas =
            await contexto.Facturas
                .AsNoTracking()
                .ToListAsync();

        var staging =
            await contexto.FacturasTemporalesImportacion
                .AsNoTracking()
                .Where(
                    registro =>
                        registro.LoteImportacionId ==
                        lote.Id)
                .ToListAsync();

        Assert.Equal(
            EstadoImportacion.Completada,
            lotePersistido.Estado);

        Assert.Single(pacientes);
        Assert.Single(facturas);
        Assert.Empty(staging);

        Assert.Equal(
            "FE000001",
            facturas[0].Id);

        Assert.Equal(
            1,
            resultado.TotalPacientesNuevos);

        Assert.Equal(
            0,
            resultado.TotalPacientesExistentes);

        Assert.Equal(
            1,
            resultado.TotalFacturasImportadas);

        Assert.Equal(
            EstadoImportacion.Completada,
            resultado.Estado);

        Assert.Equal(
            "usuario-proceso",
            resultado.ProcesadoPor);
    }

    [Fact]
    public async Task
        Procesar_FacturaExistente_DebeRechazarSinModificarLote()
    {
        await using var contexto = CrearContexto();

        var paciente =
            CrearPaciente();

        var factura =
            CrearFactura(
                numero: "000001",
                numeroDocumento:
                    paciente.NumeroDocumento);

        await contexto.Pacientes.AddAsync(paciente);
        await contexto.Facturas.AddAsync(factura);
        await contexto.GuardarCambiosAsync();

        var lote =
            await CrearLoteConfirmadoAsync(contexto);

        await CrearStagingAsync(
            contexto,
            lote.Id,
            identificadorFe: "FE000001",
            prefijo: "FE",
            numero: "000001");

        var servicio = CrearServicio(contexto);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteFacturasNoProcesable>(
                    () => servicio.ProcesarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Contains(
            "ya existen",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "FE000001",
            excepcion.IdentificadoresRelacionados);

        var lotePersistido =
            await contexto.LotesImportacion
                .AsNoTracking()
                .SingleAsync(
                    elemento => elemento.Id == lote.Id);

        var staging =
            await contexto.FacturasTemporalesImportacion
                .AsNoTracking()
                .CountAsync(
                    registro =>
                        registro.LoteImportacionId ==
                        lote.Id);

        Assert.Equal(
            EstadoImportacion.Confirmada,
            lotePersistido.Estado);

        Assert.Equal(1, staging);
    }

    [Fact]
    public async Task
        Procesar_FeDiferenteDePrefijoYNumero_DebeRechazar()
    {
        await using var contexto = CrearContexto();

        var lote =
            await CrearLoteConfirmadoAsync(contexto);

        await CrearStagingAsync(
            contexto,
            lote.Id,
            identificadorFe: "OTRA000001",
            prefijo: "FE",
            numero: "000001");

        var servicio = CrearServicio(contexto);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteFacturasNoProcesable>(
                    () => servicio.ProcesarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Contains(
            "PREFIJO + FACTURA",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Contains(
            "OTRA000001",
            excepcion.IdentificadoresRelacionados);

        Assert.Empty(
            await contexto.Facturas
                .AsNoTracking()
                .ToListAsync());
    }

    [Fact]
    public async Task
        Procesar_LoteNoConfirmado_DebeRechazar()
    {
        await using var contexto = CrearContexto();

        var lote = new LoteImportacion(
            TipoImportacion.Facturas,
            "Facturas.xlsx",
            HashValido);

        lote.RegistrarCreacion(
            CrearFecha(10),
            "usuario-carga");

        await contexto.LotesImportacion.AddAsync(lote);
        await contexto.GuardarCambiosAsync();

        var servicio = CrearServicio(contexto);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteFacturasNoProcesable>(
                    () => servicio.ProcesarAsync(
                        CrearSolicitud(lote.Id)));

        Assert.Contains(
            "debe estar confirmado",
            excepcion.Message,
            StringComparison.OrdinalIgnoreCase);

        Assert.Equal(
            EstadoImportacion.Pendiente,
            lote.Estado);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarServicio()
    {
        ServiceCollection services = new();

        services.AddApplication();

        var descriptor =
            services.Single(
                elemento =>
                    elemento.ServiceType ==
                    typeof(
                        IServicioProcesamientoLoteFacturas));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ServicioProcesamientoLoteFacturas),
            descriptor.ImplementationType);
    }

    private static ServicioProcesamientoLoteFacturas
        CrearServicio(
            SeguimientoDbContext contexto)
    {
        return new ServicioProcesamientoLoteFacturas(
            new RepositorioImportacionesEfCore(
                contexto),
            new
                RepositorioFacturasTemporalesImportacionEfCore(
                    contexto),
            new
                RepositorioPersistenciaFacturasImportacionEfCore(
                    contexto),
            contexto,
            new
                SolicitudProcesamientoLoteFacturasDtoValidator(),
            new ProveedorTiempoFalso(
                CrearFecha(14)));
    }

    private static SolicitudProcesamientoLoteFacturasDto
        CrearSolicitud(Guid loteId)
    {
        return new SolicitudProcesamientoLoteFacturasDto
        {
            LoteId = loteId,
            Usuario = " usuario-proceso "
        };
    }

    private static async Task<LoteImportacion>
        CrearLoteConfirmadoAsync(
            SeguimientoDbContext contexto)
    {
        var lote = new LoteImportacion(
            TipoImportacion.Facturas,
            "Facturas.xlsx",
            HashValido);

        lote.RegistrarCreacion(
            CrearFecha(10),
            "usuario-carga");

        lote.RegistrarAnalisis(
            totalFilas: 1,
            totalFilasValidas: 1,
            totalFilasConError: 0,
            totalAdvertencias: 0,
            fechaAnalisis: CrearFecha(11),
            totalErrores: 0);

        lote.Confirmar(
            CrearFecha(12),
            "supervisor");

        lote.RegistrarModificacion(
            CrearFecha(12),
            "supervisor");

        await contexto.LotesImportacion.AddAsync(lote);
        await contexto.GuardarCambiosAsync();

        return lote;
    }

    private static async Task CrearStagingAsync(
        SeguimientoDbContext contexto,
        Guid loteId,
        string identificadorFe,
        string prefijo,
        string numero)
    {
        var repositorio =
            new
                RepositorioFacturasTemporalesImportacionEfCore(
                    contexto);

        var registro =
            new FacturaImportacionTemporal(
                loteImportacionId: loteId,
                hojaOrigen: "Facturas",
                filaOrigen: 2,
                identificadorFe: identificadorFe,
                prefijo: prefijo,
                numero: numero,
                fechaFactura:
                    new DateOnly(2026, 7, 15),
                aseguradoraId: 1,
                valor: 150000m,
                fechaRadicacion:
                    new DateOnly(2026, 7, 20),
                tipoDocumentoId: 1,
                numeroDocumento: "123456",
                nombreCompleto:
                    "PACIENTE DE PRUEBA",
                atencionId: 1,
                costoId: 1,
                numeroAdmision: "ADM000001",
                fechaAdmision:
                    new DateOnly(2026, 7, 10),
                estadoId: 1,
                facturadorId: 1);

        await repositorio.ReemplazarAsync(
            loteId,
            [registro]);

        await contexto.GuardarCambiosAsync();
    }

    private static Paciente CrearPaciente()
    {
        var paciente = new Paciente(
            tipoDocumentoId: 1,
            numeroDocumento: "123456",
            nombreCompleto: "PACIENTE DE PRUEBA");

        paciente.RegistrarCreacion(
            CrearFecha(9),
            "usuario-pruebas");

        return paciente;
    }

    private static Factura CrearFactura(
        string numero,
        string numeroDocumento)
    {
        var factura = new Factura(
            prefijo: "FE",
            numero: numero,
            fechaFactura:
                new DateOnly(2026, 7, 15),
            aseguradoraId: 1,
            valor: 150000m,
            fechaRadicacion:
                new DateOnly(2026, 7, 20),
            tipoDocumentoId: 1,
            numeroDocumento: numeroDocumento,
            nombreCompleto:
                "PACIENTE DE PRUEBA",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: "ADM000001",
            fechaAdmision:
                new DateOnly(2026, 7, 10),
            estadoId: 1,
            facturadorId: 1);

        factura.RegistrarCreacion(
            CrearFecha(9),
            "usuario-pruebas");

        return factura;
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<
                SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"SeguimientoProcesoFacturas_" +
                    $"{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(options);
    }

    private static DateTimeOffset CrearFecha(int hora)
    {
        return new DateTimeOffset(
            2026,
            7,
            29,
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