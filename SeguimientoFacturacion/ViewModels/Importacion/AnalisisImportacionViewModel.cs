using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Configurations;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa los datos utilizados por la pantalla
/// de análisis modular de archivos.
/// </summary>
public sealed class AnalisisImportacionViewModel
{
    /// <summary>
    /// Obtiene o establece los procesos de importación que el usuario
    /// autenticado tiene permitido ejecutar.
    /// </summary>
    public IReadOnlyCollection<TipoImportacion> TiposPermitidos
    {
        get;
        set;
    } = Array.Empty<TipoImportacion>();

    /// <summary>
    /// Obtiene o establece el tipo de información
    /// que contiene el archivo.
    /// </summary>
    [Display(Name = "Tipo de importación")]
    public TipoImportacion? Tipo { get; set; }

    /// <summary>
    /// Obtiene o establece el archivo seleccionado
    /// por el usuario.
    /// </summary>
    [Display(Name = "Archivo modular")]
    public IFormFile? Archivo { get; set; }

    /// <summary>
    /// Obtiene el resultado unificado del análisis.
    /// </summary>
    public ResultadoImportacionModularViewModel?
        Resultado
    {
        get;
        init;
    }

    /// <summary>
    /// Obtiene el tamaño máximo permitido,
    /// expresado en megabytes.
    /// </summary>
    public int TamanoMaximoMegabytes =>
        LimitesCargaArchivos.TamanoMaximoMegabytes;
}
