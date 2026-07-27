using System.ComponentModel.DataAnnotations;
using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Configurations;

namespace SeguimientoFacturacion.ViewModels.Importacion;

/// <summary>
/// Representa los datos utilizados por la pantalla
/// de análisis previo de archivos.
/// </summary>
public sealed class AnalisisImportacionViewModel
{
    /// <summary>
    /// Obtiene o establece el archivo seleccionado
    /// por el usuario.
    /// </summary>
    [Display(Name = "Archivo de seguimiento")]
    public IFormFile? Archivo { get; set; }

    /// <summary>
    /// Obtiene el resultado del análisis realizado.
    /// </summary>
    public ResultadoAnalisisImportacionDto? Resultado
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
