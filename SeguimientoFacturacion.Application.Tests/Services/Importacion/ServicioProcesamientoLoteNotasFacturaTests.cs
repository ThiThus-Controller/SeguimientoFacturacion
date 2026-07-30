using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Importacion;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Services.Importacion;

/// <summary>
/// Pruebas del procesamiento definitivo de notas.
/// </summary>
public sealed class
    ServicioProcesamientoLoteNotasFacturaTests
{
    private static readonly DateTimeOffset FechaCreacion =
        new(
            2026,
            7,
            29,
            8,
            0,
            0,
            TimeSpan.Zero);

    private static readonly DateTimeOffset FechaProceso =
        new(
            2026,
            7,
            30,
            12,
            0,
            0,
            TimeSpan.Zero);

    [Fact]
    public async Task
        Procesar_ConNotasNuevas_DebeCompletarLote()
    {
        var lote = CrearLoteConfirmado(totalFilas: 2);

        NotaFacturaImportacionTemporal[] registros =
        [
            CrearRegistro(
                lote.Id,
                fila: 2,
                facturaId: "FV000001",
                numeroFactura: "000001",
                tipo: TipoNotaFactura.Credito,
                numeroNota: "NC-001",
                valor: 50000m),

            CrearRegistro(
                lote.Id,
                fila: 3,
                facturaId: "FV000002",
                numeroFactura: "000002",
                tipo: TipoNotaFactura.Debito,
                numeroNota: "ND-001",
                valor: 20000m)
        ];

        ReferenciaFacturaImportacionDto[] referencias =
        [
            CrearReferencia("FV000001"),
            CrearReferencia("FV000002")
        ];

        var repositorioImportaciones =
            new RepositorioImportacionesPrueba(lote);

        var repositorioTemporal =
            new RepositorioTemporalPrueba(registros);

        var repositorioDefinitivo =
            new RepositorioDefinitivoPrueba();

        var consultaFacturas =
            new ConsultaFacturasPrueba(referencias);

        var unidadTrabajo =
            new UnidadTrabajoPrueba();

        var servicio = CrearServicio(
            repositorioImportaciones,
            repositorioTemporal,
            repositorioDefinitivo,
            consultaFacturas,
            unidadTrabajo);

        var resultado =
            await servicio.ProcesarAsync(
                new
                    SolicitudProcesamientoLoteNotasFacturaDto
                {
                    LoteId = lote.Id,
                    Usuario = " usuario-pruebas "
                });

        Assert.Equal(
            EstadoImportacion.Completada,
            lote.Estado);

        Assert.True(repositorioTemporal.Eliminado);
        Assert.Equal(2, repositorioDefinitivo.Agregadas.Count);
        Assert.Equal(1, unidadTrabajo.TotalGuardados);

        Assert.Equal(2, resultado.TotalNotasStaging);
        Assert.Equal(2, resultado.TotalNotasImportadas);
        Assert.Equal(0, resultado.TotalNotasOmitidas);

        Assert.Equal(
            1,
            resultado.TotalNotasCreditoImportadas);

        Assert.Equal(
            1,
            resultado.TotalNotasDebitoImportadas);

        Assert.Equal(
            -30000m,
            resultado.ImpactoNetoImportado);

        Assert.Equal(
            "usuario-pruebas",
            resultado.ProcesadoPor);

        Assert.All(
            repositorioDefinitivo.Agregadas,
            nota =>
                Assert.Equal(
                    "usuario-pruebas",
                    nota.CreadoPor));
    }

    [Fact]
    public async Task
        Procesar_ConNotaExistente_DebeOmitirla()
    {
        var lote = CrearLoteConfirmado(totalFilas: 2);

        NotaFacturaImportacionTemporal[] registros =
        [
            CrearRegistro(
                lote.Id,
                fila: 2,
                facturaId: "FV000001",
                numeroFactura: "000001",
                tipo: TipoNotaFactura.Credito,
                numeroNota: "NC-001",
                valor: 50000m),

            CrearRegistro(
                lote.Id,
                fila: 3,
                facturaId: "FV000002",
                numeroFactura: "000002",
                tipo: TipoNotaFactura.Debito,
                numeroNota: "ND-001",
                valor: 20000m)
        ];

        var claveExistente =
            new ClaveNotaFacturaImportacionDto(
                "FV000001",
                TipoNotaFactura.Credito,
                "NC-001");

        var repositorioDefinitivo =
            new RepositorioDefinitivoPrueba(
                [claveExistente]);

        var servicio = CrearServicio(
            new RepositorioImportacionesPrueba(lote),
            new RepositorioTemporalPrueba(registros),
            repositorioDefinitivo,
            new ConsultaFacturasPrueba(
                [
                    CrearReferencia("FV000001"),
                    CrearReferencia("FV000002")
                ]),
            new UnidadTrabajoPrueba());

        var resultado =
            await servicio.ProcesarAsync(
                new
                    SolicitudProcesamientoLoteNotasFacturaDto
                {
                    LoteId = lote.Id,
                    Usuario = "usuario-pruebas"
                });

        var notaAgregada =
            Assert.Single(
                repositorioDefinitivo.Agregadas);

        Assert.Equal("ND-001", notaAgregada.Numero);
        Assert.Equal(1, resultado.TotalNotasImportadas);
        Assert.Equal(1, resultado.TotalNotasOmitidas);
        Assert.Equal(20000m, resultado.ImpactoNetoImportado);
    }

    [Fact]
    public async Task
        Procesar_ConFacturaInexistente_DebeRechazar()
    {
        var lote = CrearLoteConfirmado(totalFilas: 1);

        var repositorioTemporal =
            new RepositorioTemporalPrueba(
                [
                    CrearRegistro(
                        lote.Id,
                        fila: 2,
                        facturaId: "FV999999",
                        numeroFactura: "999999")
                ]);

        var unidadTrabajo =
            new UnidadTrabajoPrueba();

        var servicio = CrearServicio(
            new RepositorioImportacionesPrueba(lote),
            repositorioTemporal,
            new RepositorioDefinitivoPrueba(),
            new ConsultaFacturasPrueba([]),
            unidadTrabajo);

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteNotasFacturaNoProcesable>(
                () => servicio.ProcesarAsync(
                    CrearSolicitud(lote.Id)));

        Assert.Contains(
            "no existen",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);

        Assert.False(repositorioTemporal.Eliminado);
        Assert.Equal(0, unidadTrabajo.TotalGuardados);
    }

    [Fact]
    public async Task
        Procesar_ConAseguradoraDiferente_DebeRechazar()
    {
        var lote = CrearLoteConfirmado(totalFilas: 1);

        var servicio = CrearServicio(
            new RepositorioImportacionesPrueba(lote),
            new RepositorioTemporalPrueba(
                [
                    CrearRegistro(
                        lote.Id,
                        fila: 2,
                        facturaId: "FV000001",
                        numeroFactura: "000001",
                        aseguradoraId: 1)
                ]),
            new RepositorioDefinitivoPrueba(),
            new ConsultaFacturasPrueba(
                [
                    CrearReferencia(
                        "FV000001",
                        aseguradoraId: 2)
                ]),
            new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteNotasFacturaNoProcesable>(
                () => servicio.ProcesarAsync(
                    CrearSolicitud(lote.Id)));

        Assert.Contains(
            "aseguradora",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Procesar_ConFechaAnteriorFactura_DebeRechazar()
    {
        var lote = CrearLoteConfirmado(totalFilas: 1);

        var servicio = CrearServicio(
            new RepositorioImportacionesPrueba(lote),
            new RepositorioTemporalPrueba(
                [
                    CrearRegistro(
                        lote.Id,
                        fila: 2,
                        facturaId: "FV000001",
                        numeroFactura: "000001",
                        fechaNota:
                            new DateOnly(2026, 7, 5))
                ]),
            new RepositorioDefinitivoPrueba(),
            new ConsultaFacturasPrueba(
                [
                    CrearReferencia(
                        "FV000001",
                        fechaFactura:
                            new DateOnly(2026, 7, 10))
                ]),
            new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteNotasFacturaNoProcesable>(
                () => servicio.ProcesarAsync(
                    CrearSolicitud(lote.Id)));

        Assert.Contains(
            "anterior",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Procesar_ConTotalStagingInconsistente_DebeRechazar()
    {
        var lote = CrearLoteConfirmado(totalFilas: 2);

        var servicio = CrearServicio(
            new RepositorioImportacionesPrueba(lote),
            new RepositorioTemporalPrueba(
                [
                    CrearRegistro(
                        lote.Id,
                        fila: 2,
                        facturaId: "FV000001",
                        numeroFactura: "000001")
                ]),
            new RepositorioDefinitivoPrueba(),
            new ConsultaFacturasPrueba([]),
            new UnidadTrabajoPrueba());

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteNotasFacturaNoProcesable>(
                () => servicio.ProcesarAsync(
                    CrearSolicitud(lote.Id)));

        Assert.Contains(
            "análisis reportó",
            excepcion.Motivo,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task
        Procesar_ConSolicitudInvalida_DebeRechazar()
    {
        var lote = CrearLoteConfirmado(totalFilas: 1);

        var servicio = CrearServicio(
            new RepositorioImportacionesPrueba(lote),
            new RepositorioTemporalPrueba([]),
            new RepositorioDefinitivoPrueba(),
            new ConsultaFacturasPrueba([]),
            new UnidadTrabajoPrueba());

        await Assert.ThrowsAsync<
            ExcepcionValidacionAplicacion>(
                () => servicio.ProcesarAsync(
                    new
                        SolicitudProcesamientoLoteNotasFacturaDto
                    {
                        LoteId = Guid.Empty,
                        Usuario = " "
                    }));
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
                        IServicioProcesamientoLoteNotasFactura));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(
                ServicioProcesamientoLoteNotasFactura),
            descriptor.ImplementationType);
    }

    private static
        ServicioProcesamientoLoteNotasFactura
        CrearServicio(
            IRepositorioImportaciones
                repositorioImportaciones,
            IRepositorioNotasFacturaTemporalesImportacion
                repositorioTemporal,
            IRepositorioPersistenciaNotasFacturaImportacion
                repositorioDefinitivo,
            IConsultaReferenciasFacturasImportacion
                consultaFacturas,
            IUnidadTrabajo unidadTrabajo)
    {
        return new
            ServicioProcesamientoLoteNotasFactura(
                repositorioImportaciones,
                repositorioTemporal,
                repositorioDefinitivo,
                consultaFacturas,
                unidadTrabajo,
                new
                    SolicitudProcesamientoLoteNotasFacturaDtoValidator(),
                new TimeProviderPrueba(FechaProceso));
    }

    private static
        SolicitudProcesamientoLoteNotasFacturaDto
        CrearSolicitud(Guid loteId)
    {
        return new
            SolicitudProcesamientoLoteNotasFacturaDto
        {
            LoteId = loteId,
            Usuario = "usuario-pruebas"
        };
    }

    private static LoteImportacion
        CrearLoteConfirmado(int totalFilas)
    {
        var lote =
            new LoteImportacion(
                TipoImportacion.NotasFactura,
                "PlantillaNotasFactura.xlsx",
                new string('A', 64));

        lote.RegistrarCreacion(
            FechaCreacion,
            "usuario-pruebas");

        lote.RegistrarAnalisis(
            totalFilas: totalFilas,
            totalFilasValidas: totalFilas,
            totalFilasConError: 0,
            totalAdvertencias: 0,
            fechaAnalisis:
                FechaCreacion.AddMinutes(1),
            totalErrores: 0);

        lote.Confirmar(
            FechaCreacion.AddMinutes(2),
            "supervisor-pruebas");

        lote.RegistrarModificacion(
            FechaCreacion.AddMinutes(2),
            "supervisor-pruebas");

        return lote;
    }

    private static
        NotaFacturaImportacionTemporal
        CrearRegistro(
            Guid loteId,
            int fila,
            string facturaId,
            string numeroFactura,
            int aseguradoraId = 1,
            TipoNotaFactura tipo =
                TipoNotaFactura.Credito,
            string numeroNota = "NC-001",
            decimal valor = 50000m,
            DateOnly? fechaNota = null)
    {
        return new
            NotaFacturaImportacionTemporal(
                loteImportacionId: loteId,
                hojaOrigen: "NOTAS",
                filaOrigen: fila,
                identificadorFe: facturaId,
                prefijo: "FV",
                numeroFactura: numeroFactura,
                aseguradoraId: aseguradoraId,
                tipo: tipo,
                fechaNota:
                    fechaNota ??
                    new DateOnly(2026, 7, 20),
                numeroNota: numeroNota,
                valorNota: valor);
    }

    private static ReferenciaFacturaImportacionDto
        CrearReferencia(
            string facturaId,
            int aseguradoraId = 1,
            DateOnly? fechaFactura = null)
    {
        return new ReferenciaFacturaImportacionDto
        {
            FacturaId = facturaId,
            AseguradoraId = aseguradoraId,

            FechaFactura =
                fechaFactura ??
                new DateOnly(2026, 7, 10)
        };
    }

    private sealed class
        RepositorioImportacionesPrueba :
            IRepositorioImportaciones
    {
        private readonly LoteImportacion? _lote;

        public RepositorioImportacionesPrueba(
            LoteImportacion? lote)
        {
            _lote = lote;
        }

        public Task AgregarLoteAsync(
            LoteImportacion lote,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<LoteImportacion?> ObtenerLoteAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_lote);
        }

        public Task<bool> ExisteArchivoAsync(
            TipoImportacion tipo,
            string hashArchivo,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(false);
        }

        public Task AgregarInconsistenciasAsync(
            IReadOnlyCollection<
                InconsistenciaImportacion>
                inconsistencias,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            InconsistenciaImportacion>>
            ListarInconsistenciasAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult<
                IReadOnlyList<
                    InconsistenciaImportacion>>([]);
        }
    }

    private sealed class
        RepositorioTemporalPrueba :
            IRepositorioNotasFacturaTemporalesImportacion
    {
        private readonly IReadOnlyList<
            NotaFacturaImportacionTemporal> _registros;

        public RepositorioTemporalPrueba(
            IReadOnlyList<
                NotaFacturaImportacionTemporal> registros)
        {
            _registros = registros;
        }

        public bool Eliminado { get; private set; }

        public Task ReemplazarAsync(
            Guid loteId,
            IReadOnlyCollection<
                NotaFacturaImportacionTemporal> notas,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<IReadOnlyList<
            NotaFacturaImportacionTemporal>>
            ListarAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_registros);
        }

        public Task EliminarAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
        {
            Eliminado = true;

            return Task.CompletedTask;
        }
    }

    private sealed class
        RepositorioDefinitivoPrueba :
            IRepositorioPersistenciaNotasFacturaImportacion
    {
        private readonly IReadOnlyList<
            ClaveNotaFacturaImportacionDto> _existentes;

        public RepositorioDefinitivoPrueba(
            IReadOnlyList<
                ClaveNotaFacturaImportacionDto>? existentes =
                null)
        {
            _existentes = existentes ?? [];
        }

        public List<NotaFactura> Agregadas { get; } = [];

        public Task<IReadOnlyList<
            ClaveNotaFacturaImportacionDto>>
            ListarClavesExistentesAsync(
                IReadOnlyCollection<
                    ClaveNotaFacturaImportacionDto> claves,
                CancellationToken cancellationToken = default)
        {
            var solicitadas =
                claves.ToHashSet();

            IReadOnlyList<
                ClaveNotaFacturaImportacionDto> resultado =
                _existentes
                    .Where(solicitadas.Contains)
                    .ToArray();

            return Task.FromResult(resultado);
        }

        public Task AgregarNotasAsync(
            IReadOnlyCollection<NotaFactura> notas,
            CancellationToken cancellationToken = default)
        {
            Agregadas.AddRange(notas);

            return Task.CompletedTask;
        }
    }

    private sealed class ConsultaFacturasPrueba :
        IConsultaReferenciasFacturasImportacion
    {
        private readonly IReadOnlyCollection<
            ReferenciaFacturaImportacionDto> _referencias;

        public ConsultaFacturasPrueba(
            IReadOnlyCollection<
                ReferenciaFacturaImportacionDto>
                referencias)
        {
            _referencias = referencias;
        }

        public Task<IReadOnlyCollection<
            ReferenciaFacturaImportacionDto>>
            ObtenerPorIdsAsync(
                IReadOnlyCollection<string> facturaIds,
                CancellationToken cancellationToken = default)
        {
            var solicitados =
                facturaIds.ToHashSet(
                    StringComparer.OrdinalIgnoreCase);

            IReadOnlyCollection<
                ReferenciaFacturaImportacionDto> resultado =
                _referencias
                    .Where(
                        referencia =>
                            solicitados.Contains(
                                referencia.FacturaId))
                    .ToArray();

            return Task.FromResult(resultado);
        }
    }

    private sealed class UnidadTrabajoPrueba :
        IUnidadTrabajo
    {
        public int TotalGuardados { get; private set; }

        public Task<int> GuardarCambiosAsync(
            CancellationToken cancellationToken = default)
        {
            TotalGuardados++;

            return Task.FromResult(1);
        }
    }

    private sealed class TimeProviderPrueba :
        TimeProvider
    {
        private readonly DateTimeOffset _fechaUtc;

        public TimeProviderPrueba(
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