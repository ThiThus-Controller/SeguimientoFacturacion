using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeveridadDto =
    SeguimientoFacturacion.Application.DTOs.Importacion
        .SeveridadInconsistenciaImportacion;

namespace SeguimientoFacturacion.Application.Tests.Services;

/// <summary>
/// Pruebas del registro del análisis de lotes.
/// </summary>
public sealed class ServicioRegistroAnalisisLoteTests
{
    private const string HashValido =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task Registrar_ConErrores_DebeCalcularTotales()
    {
        var lote = CrearLote();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        var resultadoAnalisis =
            new ResultadoAnalisisImportacionDto
            {
                NombreArchivo = "Facturas.xlsx",
                TotalFilasAnalizadas = 10,
                Inconsistencias =
                [
                    CrearInconsistencia(
                        fila: 2,
                        codigo: "FACTURA_REQUERIDA",
                        severidad: SeveridadDto.Error),
                    CrearInconsistencia(
                        fila: 2,
                        codigo: "VALOR_INVALIDO",
                        severidad: SeveridadDto.Error),
                    CrearInconsistencia(
                        fila: 3,
                        codigo: "RADICACION_VACIA",
                        severidad:
                            SeveridadDto.Advertencia)
                ]
            };

        var resultado = await servicio.RegistrarAsync(
            lote.Id,
            resultadoAnalisis,
            "analista");

        Assert.Equal(
            EstadoImportacion.Analizada,
            lote.Estado);

        Assert.Equal(10, lote.TotalFilas);
        Assert.Equal(9, lote.TotalFilasValidas);
        Assert.Equal(1, lote.TotalFilasConError);
        Assert.Equal(2, lote.TotalErrores);
        Assert.Equal(1, lote.TotalAdvertencias);
        Assert.False(lote.PuedeConfirmarse);

        Assert.Equal(
            3,
            repositorio.InconsistenciasAgregadas.Count);

        Assert.Equal(
            SeveridadImportacion.Error,
            repositorio.InconsistenciasAgregadas[0]
                .Severidad);

        Assert.Equal(
            SeveridadImportacion.Advertencia,
            repositorio.InconsistenciasAgregadas[2]
                .Severidad);

        Assert.Equal(
            1,
            unidadTrabajo.NumeroInvocaciones);

        Assert.Equal(2, resultado.TotalErrores);
        Assert.False(resultado.PuedeConfirmarse);
    }

    [Fact]
    public async Task Registrar_SinErrores_DebePermitirConfirmacion()
    {
        var lote = CrearLote();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultadoAnalisis =
            new ResultadoAnalisisImportacionDto
            {
                NombreArchivo = "Facturas.xlsx",
                TotalFilasAnalizadas = 25
            };

        var resultado = await servicio.RegistrarAsync(
            lote.Id,
            resultadoAnalisis,
            "analista");

        Assert.Equal(25, lote.TotalFilasValidas);
        Assert.Equal(0, lote.TotalFilasConError);
        Assert.Equal(0, lote.TotalErrores);
        Assert.True(lote.PuedeConfirmarse);
        Assert.True(resultado.PuedeConfirmarse);
    }

    [Fact]
    public async Task Registrar_ConErrorGeneral_DebeBloquearLote()
    {
        var lote = CrearLote();

        var repositorio =
            new RepositorioImportacionesFalso(lote);

        var servicio = CrearServicio(
            repositorio,
            new UnidadTrabajoFalsa());

        var resultadoAnalisis =
            new ResultadoAnalisisImportacionDto
            {
                NombreArchivo = "Facturas.xlsx",
                TotalFilasAnalizadas = 0,
                Inconsistencias =
                [
                    CrearInconsistencia(
                        fila: null,
                        codigo: "HOJA_REQUERIDA",
                        severidad: SeveridadDto.Error)
                ]
            };

        await servicio.RegistrarAsync(
            lote.Id,
            resultadoAnalisis,
            "analista");

        Assert.Equal(0, lote.TotalFilasConError);
        Assert.Equal(1, lote.TotalErrores);
        Assert.False(lote.PuedeConfirmarse);
    }

    [Fact]
    public async Task Registrar_ConLoteInexistente_DebeLanzarExcepcion()
    {
        var repositorio =
            new RepositorioImportacionesFalso(null);

        var unidadTrabajo =
            new UnidadTrabajoFalsa();

        var servicio = CrearServicio(
            repositorio,
            unidadTrabajo);

        var loteId = Guid.NewGuid();

        var resultado =
            new ResultadoAnalisisImportacionDto
            {
                NombreArchivo = "Facturas.xlsx"
            };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionLoteImportacionNoEncontrado>(
                    () => servicio.RegistrarAsync(
                        loteId,
                        resultado,
                        "analista"));

        Assert.Equal(loteId, excepcion.LoteId);

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
                    typeof(IServicioRegistroAnalisisLote));

        Assert.Equal(
            ServiceLifetime.Transient,
            descriptor.Lifetime);

        Assert.Equal(
            typeof(ServicioRegistroAnalisisLote),
            descriptor.ImplementationType);
    }

    private static InconsistenciaImportacionDto
        CrearInconsistencia(
            int? fila,
            string codigo,
            SeveridadDto severidad)
    {
        return new InconsistenciaImportacionDto
        {
            Fila = fila,
            Columna = "FACTURA",
            Codigo = codigo,
            Mensaje = "Inconsistencia de prueba.",
            Severidad = severidad
        };
    }

    private static LoteImportacion CrearLote()
    {
        var lote = new LoteImportacion(
            TipoImportacion.Facturas,
            "Facturas.xlsx",
            HashValido);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026,
                7,
                28,
                12,
                0,
                0,
                TimeSpan.Zero),
            "usuario-carga");

        return lote;
    }

    private static ServicioRegistroAnalisisLote
        CrearServicio(
            RepositorioImportacionesFalso repositorio,
            UnidadTrabajoFalsa unidadTrabajo)
    {
        return new ServicioRegistroAnalisisLote(
            repositorio,
            unidadTrabajo,
            new ProveedorTiempoFalso(
                new DateTimeOffset(
                    2026,
                    7,
                    28,
                    13,
                    0,
                    0,
                    TimeSpan.Zero)));
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
        IRepositorioImportaciones
    {
        private readonly LoteImportacion? _lote;

        public RepositorioImportacionesFalso(
            LoteImportacion? lote)
        {
            _lote = lote;
        }

        public List<InconsistenciaImportacion>
            InconsistenciasAgregadas
        { get; } = [];

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
            InconsistenciasAgregadas.AddRange(
                inconsistencias);

            return Task.CompletedTask;
        }

        public Task<
            IReadOnlyList<InconsistenciaImportacion>>
            ListarInconsistenciasAsync(
                Guid loteId,
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<InconsistenciaImportacion>
                resultado = InconsistenciasAgregadas;

            return Task.FromResult(resultado);
        }
    }
}