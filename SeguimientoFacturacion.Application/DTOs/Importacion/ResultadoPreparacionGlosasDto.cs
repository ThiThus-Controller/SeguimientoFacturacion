namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Contiene las glosas preparadas desde una plantilla
/// modular validada.
/// </summary>
public sealed class ResultadoPreparacionGlosasDto
{
    /// <summary>
    /// Obtiene el nombre del archivo procesado.
    /// </summary>
    public required string NombreArchivo { get; init; }

    /// <summary>
    /// Obtiene las glosas preparadas.
    /// </summary>
    public IReadOnlyCollection<
        GlosaPreparadaImportacionDto> Glosas
    {
        get;
        init;
    } = Array.Empty<GlosaPreparadaImportacionDto>();

    /// <summary>
    /// Obtiene la cantidad total de glosas.
    /// </summary>
    public int TotalGlosas => Glosas.Count;

    /// <summary>
    /// Obtiene la cantidad de glosas que contienen
    /// fecha de respuesta.
    /// </summary>
    public int TotalGlosasConRespuesta =>
        Glosas.Count(glosa => glosa.TieneRespuesta);

    /// <summary>
    /// Obtiene la cantidad de glosas todavía abiertas.
    /// </summary>
    public int TotalGlosasSinRespuesta =>
        Glosas.Count(glosa => !glosa.TieneRespuesta);

    /// <summary>
    /// Obtiene el valor total glosado.
    /// </summary>
    public decimal ValorTotalGlosado =>
        Glosas.Sum(glosa => glosa.ValorGlosa);
}