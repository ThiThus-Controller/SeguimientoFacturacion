using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Importacion;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Services;

/// <summary>
/// Pruebas del servicio de registro de lotes.
/// </summary>
public sealed class ServicioRegistroLoteImportacionTests
{
    private const string HashArchivo =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task Registrar_ConSolicitudValida_DebeGuardarLotePendiente()
    {
        var fechaUtc = new DateTimeOffset(
            2026,
            7,
            28,
            15,
            30,
            0,
            TimeSpan.Zero);

        var repositorio =
            new RepositorioImportacionesFalso();

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var calculadorHash =
            new CalculadorHashArchivoFalso(
                HashArchivo);

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo,
            calculadorHash,
            fechaUtc);

        using var contenido =
            new MemoryStream([1, 2, 3, 4]);

        var solicitud =
            new SolicitudRegistroLoteImportacionDto
            {
                Tipo = TipoImportacion.Facturas,
                NombreArchivo =
                    " Facturas 2026.xlsx ",
                Contenido = contenido,
                Usuario = " administrador "
            };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var resultado = await servicio.RegistrarAsync(
            solicitud,
            cancellationTokenSource.Token);

        Assert.Equal(1, calculadorHash.NumeroInvocaciones);

        Assert.Equal(
            1,
            repositorio.NumeroConsultasExistencia);

        Assert.NotNull(repositorio.LoteAgregado);

        Assert.Equal(
            EstadoImportacion.Pendiente,
            repositorio.LoteAgregado.Estado);

        Assert.Equal(
            "Facturas 2026.xlsx",
            repositorio.LoteAgregado.NombreArchivo);

        Assert.Equal(
            "administrador",
            repositorio.LoteAgregado.CreadoPor);

        Assert.Equal(
            fechaUtc,
            repositorio.LoteAgregado
                .FechaCreacionUtc);

        Assert.Equal(
            1,
            unidadTrabajo.NumeroInvocaciones);

        Assert.Equal(
            repositorio.LoteAgregado.Id,
            resultado.LoteId);

        Assert.Equal(
            EstadoImportacion.Pendiente,
            resultado.Estado);

        Assert.Equal(
            HashArchivo,
            resultado.HashArchivo);

        Assert.Equal(
            fechaUtc,
            resultado.FechaRegistroUtc);
    }

    [Fact]
    public async Task Registrar_ConArchivoDuplicado_NoDebeGuardarCambios()
    {
        var repositorio =
            new RepositorioImportacionesFalso
            {
                ArchivoExiste = true
            };

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo,
            new CalculadorHashArchivoFalso(
                HashArchivo),
            DateTimeOffset.UtcNow);

        using var contenido =
            new MemoryStream([1, 2, 3]);

        var solicitud =
            new SolicitudRegistroLoteImportacionDto
            {
                Tipo = TipoImportacion.Pagos,
                NombreArchivo = "Pagos.xlsx",
                Contenido = contenido,
                Usuario = "administrador"
            };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionArchivoImportacionDuplicado>(
                    () => servicio.RegistrarAsync(
                        solicitud));

        Assert.Equal(
            TipoImportacion.Pagos,
            excepcion.Tipo);

        Assert.Equal(
            HashArchivo,
            excepcion.HashArchivo);

        Assert.NotNull(excepcion.LoteExistente);
        Assert.Equal(
            repositorio.LoteDuplicadoId,
            excepcion.LoteExistente.LoteId);
        Assert.True(
            excepcion.LoteExistente
                .PuedeContinuarConfirmacion);

        Assert.Null(repositorio.LoteAgregado);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
    }

    [Fact]
    public async Task Registrar_ConSolicitudInvalida_NoDebeCalcularHash()
    {
        var repositorio =
            new RepositorioImportacionesFalso();

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var calculadorHash =
            new CalculadorHashArchivoFalso(
                HashArchivo);

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo,
            calculadorHash,
            DateTimeOffset.UtcNow);

        using var contenido =
            new MemoryStream([1, 2, 3]);

        var solicitud =
            new SolicitudRegistroLoteImportacionDto
            {
                Tipo = TipoImportacion.Facturas,
                NombreArchivo = "Facturas.csv",
                Contenido = contenido,
                Usuario = "administrador"
            };

        await Assert.ThrowsAsync<
            ExcepcionValidacionAplicacion>(
                () => servicio.RegistrarAsync(
                    solicitud));

        Assert.Equal(
            0,
            calculadorHash.NumeroInvocaciones);

        Assert.Equal(
            0,
            repositorio.NumeroConsultasExistencia);

        Assert.Equal(
            0,
            unidadTrabajo.NumeroInvocaciones);
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
                        IServicioRegistroLoteImportacion));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ServicioRegistroLoteImportacion),
            descriptor.ImplementationType);

        Assert.Contains(
            services,
            elemento =>
                elemento.ServiceType ==
                typeof(TimeProvider));
    }

    private static ServicioRegistroLoteImportacion
        CrearServicio(
            RepositorioImportacionesFalso repositorio,
            UnidadTrabajoFalsa unidadTrabajo,
            CalculadorHashArchivoFalso calculadorHash,
            DateTimeOffset fechaUtc)
    {
        return new ServicioRegistroLoteImportacion(
            repositorio,
            repositorio,
            unidadTrabajo,
            calculadorHash,
            new
                SolicitudRegistroLoteImportacionDtoValidator(),
            new ProveedorTiempoFalso(fechaUtc));
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

    private sealed class CalculadorHashArchivoFalso :
        ICalculadorHashArchivo
    {
        private readonly string _hash;

        public CalculadorHashArchivoFalso(string hash)
        {
            _hash = hash;
        }

        public int NumeroInvocaciones { get; private set; }

        public Task<string> CalcularSha256Async(
            Stream contenido,
            CancellationToken cancellationToken = default)
        {
            NumeroInvocaciones++;

            return Task.FromResult(_hash);
        }
    }

    private sealed class UnidadTrabajoFalsa :
        IUnidadTrabajo
    {
        public int NumeroInvocaciones { get; private set; }

        public Task<int> GuardarCambiosAsync(
            CancellationToken cancellationToken = default)
        {
            NumeroInvocaciones++;

            return Task.FromResult(1);
        }
    }

    private sealed class RepositorioImportacionesFalso :
        IRepositorioImportaciones,
        IConsultaLoteImportacionDuplicado
    {
        public bool ArchivoExiste { get; init; }

        public Guid LoteDuplicadoId { get; } = Guid.NewGuid();

        public int NumeroConsultasExistencia
        {
            get;
            private set;
        }

        public LoteImportacion? LoteAgregado
        {
            get;
            private set;
        }

        public Task AgregarLoteAsync(
            LoteImportacion lote,
            CancellationToken cancellationToken = default)
        {
            LoteAgregado = lote;

            return Task.CompletedTask;
        }

        public Task<LoteImportacion?> ObtenerLoteAsync(
            Guid loteId,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(LoteAgregado);
        }

        public Task<bool> ExisteArchivoAsync(
            TipoImportacion tipo,
            string hashArchivo,
            CancellationToken cancellationToken = default)
        {
            NumeroConsultasExistencia++;

            return Task.FromResult(ArchivoExiste);
        }

        public Task<LoteImportacionDuplicadoDto?> ObtenerAsync(
            TipoImportacion tipo,
            string hashArchivo,
            CancellationToken cancellationToken = default)
        {
            NumeroConsultasExistencia++;

            if (!ArchivoExiste)
            {
                return Task.FromResult<
                    LoteImportacionDuplicadoDto?>(null);
            }

            return Task.FromResult<
                LoteImportacionDuplicadoDto?>(
                    new LoteImportacionDuplicadoDto
                    {
                        LoteId = LoteDuplicadoId,
                        Tipo = tipo,
                        Estado = EstadoImportacion.Analizada,
                        NombreArchivo = "Pagos.xlsx",
                        TotalFilas = 20,
                        TotalErrores = 0,
                        FechaCreacionUtc =
                            DateTimeOffset.UtcNow
                    });
        }

        public Task AgregarInconsistenciasAsync(
            IReadOnlyCollection<
                InconsistenciaImportacion>
                inconsistencias,
            CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<
            IReadOnlyList<InconsistenciaImportacion>>
            ListarInconsistenciasAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<InconsistenciaImportacion>
                resultado =
                    Array.Empty<
                        InconsistenciaImportacion>();

            return Task.FromResult(resultado);
        }
    }
}
