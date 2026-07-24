using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Importacion;

namespace SeguimientoFacturacion.Application.Tests.Services;

/// <summary>
/// Pruebas del servicio de análisis de importación.
/// </summary>
public sealed class ServicioAnalisisImportacionTests
{
    [Fact]
    public async Task AnalizarAsync_ConSolicitudValida_DebeEjecutarLector()
    {
        var resultadoEsperado =
            new ResultadoAnalisisImportacionDto
            {
                NombreArchivo = "Seguimiento 2026.xlsx",
                HojasDetectadas = ["Seguimiento"],
                AniosDetectados = [2026],
                TotalFilasAnalizadas = 10,
                FacturasDetectadas = 8,
                MovimientosDetectados = 2,
                CatalogosNoMapeados = 0
            };

        var lector =
            new LectorArchivoFacturacionFalso(
                resultadoEsperado);

        var servicio =
            new ServicioAnalisisImportacion(
                lector,
                new
                    SolicitudAnalisisImportacionDtoValidator());

        using var contenido =
            new MemoryStream([1, 2, 3, 4]);

        contenido.Position = 2;

        var solicitud =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = "Seguimiento 2026.xlsx",
                Contenido = contenido
            };

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var resultado = await servicio.AnalizarAsync(
            solicitud,
            cancellationTokenSource.Token);

        Assert.Same(resultadoEsperado, resultado);
        Assert.Equal(1, lector.NumeroInvocaciones);
        Assert.Same(solicitud, lector.UltimaSolicitud);

        Assert.Equal(
            cancellationTokenSource.Token,
            lector.UltimoCancellationToken);

        Assert.Equal(0, lector.PosicionAlInvocar);
    }

    [Fact]
    public async Task AnalizarAsync_ConExtensionInvalida_NoDebeEjecutarLector()
    {
        var resultadoLector =
            new ResultadoAnalisisImportacionDto
            {
                NombreArchivo = "Seguimiento 2026.csv"
            };

        var lector =
            new LectorArchivoFacturacionFalso(
                resultadoLector);

        var servicio =
            new ServicioAnalisisImportacion(
                lector,
                new
                    SolicitudAnalisisImportacionDtoValidator());

        using var contenido =
            new MemoryStream([1, 2, 3, 4]);

        var solicitud =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = "Seguimiento 2026.csv",
                Contenido = contenido
            };

        await Assert.ThrowsAsync<
            ExcepcionValidacionAplicacion>(
                () => servicio.AnalizarAsync(solicitud));

        Assert.Equal(0, lector.NumeroInvocaciones);
    }

    private sealed class LectorArchivoFacturacionFalso :
        ILectorArchivoFacturacion
    {
        private readonly ResultadoAnalisisImportacionDto
            _resultado;

        public LectorArchivoFacturacionFalso(
            ResultadoAnalisisImportacionDto resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones { get; private set; }

        public SolicitudAnalisisImportacionDto?
            UltimaSolicitud
        {
            get;
            private set;
        }

        public CancellationToken UltimoCancellationToken
        {
            get;
            private set;
        }

        public long? PosicionAlInvocar
        {
            get;
            private set;
        }

        public Task<ResultadoAnalisisImportacionDto>
            AnalizarAsync(
                SolicitudAnalisisImportacionDto solicitud,
                CancellationToken cancellationToken = default)
        {
            NumeroInvocaciones++;
            UltimaSolicitud = solicitud;
            UltimoCancellationToken = cancellationToken;

            PosicionAlInvocar =
                solicitud.Contenido.CanSeek
                    ? solicitud.Contenido.Position
                    : null;

            return Task.FromResult(_resultado);
        }
    }
}