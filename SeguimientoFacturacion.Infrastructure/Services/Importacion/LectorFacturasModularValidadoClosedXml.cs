using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Interfaces.Importacion;

namespace SeguimientoFacturacion.Infrastructure.Services.Importacion;

/// <summary>
/// Coordina la inspección estructural, la consulta de
/// catálogos y la validación detallada de una plantilla
/// modular de facturas.
/// </summary>
public sealed class
    LectorFacturasModularValidadoClosedXml :
    ILectorArchivoFacturacion
{
    private readonly
        LectorEstructuralFacturasModularClosedXml
        _lectorEstructural;

    private readonly
        IValidadorFilasFacturasModular
        _validadorFilas;

    private readonly
        IConsultaCatalogosImportacion
        _consultaCatalogos;

    /// <summary>
    /// Inicializa el lector modular validado.
    /// </summary>
    public LectorFacturasModularValidadoClosedXml(
        LectorEstructuralFacturasModularClosedXml
            lectorEstructural,
        IValidadorFilasFacturasModular
            validadorFilas,
        IConsultaCatalogosImportacion
            consultaCatalogos)
    {
        ArgumentNullException.ThrowIfNull(
            lectorEstructural);

        ArgumentNullException.ThrowIfNull(
            validadorFilas);

        ArgumentNullException.ThrowIfNull(
            consultaCatalogos);

        _lectorEstructural = lectorEstructural;
        _validadorFilas = validadorFilas;
        _consultaCatalogos = consultaCatalogos;
    }

    /// <inheritdoc />
    public async Task<ResultadoAnalisisImportacionDto>
        AnalizarAsync(
            SolicitudAnalisisImportacionDto solicitud,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(solicitud);
        ArgumentNullException.ThrowIfNull(
            solicitud.Contenido);

        cancellationToken.ThrowIfCancellationRequested();

        var lecturaEstructural =
            await _lectorEstructural
                .InspeccionarYAnalizarAsync(
                    solicitud,
                    cancellationToken);

        if (!lecturaEstructural.Analisis.EsValido)
        {
            return lecturaEstructural.Analisis;
        }

        var catalogos =
            await _consultaCatalogos
                .ObtenerAsync(
                    cancellationToken);

        var validacionFilas =
            await _validadorFilas
                .ValidarAsync(
                    solicitud.Contenido,
                    lecturaEstructural.Inspeccion,
                    catalogos,
                    cancellationToken);

        var inconsistencias =
            lecturaEstructural
                .Analisis
                .Inconsistencias
                .Concat(
                    validacionFilas.Inconsistencias)
                .ToArray();

        return lecturaEstructural.Analisis with
        {
            TotalFilasAnalizadas =
                validacionFilas.TotalFilasAnalizadas,

            FacturasDetectadas =
                validacionFilas.FacturasDetectadas,

            AniosDetectados =
                validacionFilas.AniosDetectados,

            MovimientosDetectados = 0,

            CatalogosNoMapeados =
                validacionFilas.CatalogosNoMapeados,

            Inconsistencias =
                inconsistencias
        };
    }
}