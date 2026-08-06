namespace SeguimientoFacturacion.Domain.Constants;

/// <summary>
/// Centraliza los códigos empresariales estables del catálogo de estados
/// de factura.
/// </summary>
public static class CodigosEstadoFactura
{
    /// <summary>
    /// Identifica una factura activa.
    /// </summary>
    public const int Activa = 2;

    /// <summary>
    /// Identifica el código histórico de anulación.
    /// </summary>
    public const int AnuladaHistorica = 3;

    /// <summary>
    /// Identifica una factura anulada.
    /// </summary>
    public const int Anulada = 5;

    /// <summary>
    /// Indica si el código tiene tratamiento de anulación.
    /// </summary>
    public static bool EsAnulada(int estadoId)
    {
        return estadoId is
            AnuladaHistorica or
            Anulada;
    }
}
