namespace SeguimientoFacturacion.Domain.Enums;

/// <summary>
/// Define los tipos de carga masiva soportados
/// por el sistema.
/// </summary>
public enum TipoImportacion
{
    /// <summary>
    /// Carga de catálogos controlados.
    /// </summary>
    Catalogos = 1,

    /// <summary>
    /// Carga de facturas y pacientes.
    /// </summary>
    Facturas = 2,

    /// <summary>
    /// Carga de notas crédito y débito.
    /// </summary>
    NotasFactura = 3,

    /// <summary>
    /// Carga de glosas.
    /// </summary>
    Glosas = 4,

    /// <summary>
    /// Carga de pagos y sus aplicaciones.
    /// </summary>
    Pagos = 5
}