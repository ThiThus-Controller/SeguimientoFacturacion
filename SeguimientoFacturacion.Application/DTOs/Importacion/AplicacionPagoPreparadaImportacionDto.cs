namespace SeguimientoFacturacion.Application.DTOs.Importacion;

/// <summary>
/// Representa la aplicación de una parte de un pago
/// sobre una factura específica, preparada desde una
/// plantilla modular.
/// </summary>
public sealed class
    AplicacionPagoPreparadaImportacionDto
{
    /// <summary>
    /// Obtiene el nombre de la hoja de origen.
    /// </summary>
    public required string HojaOrigen { get; init; }

    /// <summary>
    /// Obtiene el número de fila de origen.
    /// </summary>
    public required int FilaOrigen { get; init; }

    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public required string IdentificadorFe { get; init; }

    /// <summary>
    /// Obtiene el prefijo de la factura.
    /// </summary>
    public required string Prefijo { get; init; }

    /// <summary>
    /// Obtiene el número de la factura.
    /// </summary>
    public required string NumeroFactura { get; init; }

    /// <summary>
    /// Obtiene el valor bruto aplicado a la factura.
    /// Corresponde a la columna VR PAGADO.
    /// </summary>
    public required decimal ValorAplicado { get; init; }

    /// <summary>
    /// Obtiene el valor cruzado aplicado a la factura.
    /// Corresponde a la columna VR CRUZADO.
    /// </summary>
    public required decimal ValorCruzadoAplicado
    {
        get;
        init;
    }
}