namespace SeguimientoFacturacion.Application.DTOs.Pagos;

/// <summary>
/// Clasifica un recibo según su distribución financiera definitiva.
/// </summary>
public enum TipoDistribucionPago
{
    Aplicado = 1,
    Anticipo = 2,
    Mixto = 3
}
