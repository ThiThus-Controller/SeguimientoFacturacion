namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Representa una factura preparada para mostrarse
/// en la grilla principal.
/// </summary>
public sealed record FacturaResumenDto
{
    /// <summary>
    /// Obtiene el identificador FE de la factura.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Obtiene el prefijo de la factura.
    /// </summary>
    public required string Prefijo { get; init; }

    /// <summary>
    /// Obtiene el número de la factura.
    /// </summary>
    public required string Numero { get; init; }

    /// <summary>
    /// Obtiene la fecha de emisión.
    /// </summary>
    public DateOnly FechaFactura { get; init; }

    /// <summary>
    /// Obtiene el identificador de la aseguradora.
    /// </summary>
    public int AseguradoraId { get; init; }

    /// <summary>
    /// Obtiene el nombre de la aseguradora.
    /// </summary>
    public required string Aseguradora { get; init; }

    /// <summary>
    /// Obtiene el valor original de la factura.
    /// </summary>
    public decimal Valor { get; init; }

    /// <summary>
    /// Obtiene la fecha de radicación.
    /// </summary>
    public DateOnly? FechaRadicacion { get; init; }

    /// <summary>
    /// Obtiene los días transcurridos hasta la radicación.
    /// </summary>
    public int? DiasHastaRadicacion { get; init; }

    /// <summary>
    /// Obtiene el identificador del tipo de documento.
    /// </summary>
    public int TipoDocumentoId { get; init; }

    /// <summary>
    /// Obtiene la sigla del tipo de documento.
    /// </summary>
    public required string TipoDocumentoSigla { get; init; }

    /// <summary>
    /// Obtiene el número de documento del paciente.
    /// </summary>
    public required string NumeroDocumento { get; init; }

    /// <summary>
    /// Obtiene el nombre completo del paciente.
    /// </summary>
    public required string NombreCompleto { get; init; }

    /// <summary>
    /// Obtiene el identificador del tipo de atención.
    /// </summary>
    public int AtencionId { get; init; }

    /// <summary>
    /// Obtiene la descripción del tipo de atención.
    /// </summary>
    public required string Atencion { get; init; }

    /// <summary>
    /// Obtiene el identificador del centro de costo.
    /// </summary>
    public int CostoId { get; init; }

    /// <summary>
    /// Obtiene la descripción del centro de costo.
    /// </summary>
    public required string Costo { get; init; }

    /// <summary>
    /// Obtiene el número de admisión.
    /// </summary>
    public string? NumeroAdmision { get; init; }

    /// <summary>
    /// Obtiene la fecha de admisión.
    /// </summary>
    public DateOnly? FechaAdmision { get; init; }

    /// <summary>
    /// Obtiene el identificador del estado.
    /// </summary>
    public int EstadoId { get; init; }

    /// <summary>
    /// Obtiene la descripción del estado.
    /// </summary>
    public required string Estado { get; init; }

    /// <summary>
    /// Obtiene el identificador del facturador.
    /// </summary>
    public int FacturadorId { get; init; }

    /// <summary>
    /// Obtiene el nombre del facturador.
    /// </summary>
    public required string Facturador { get; init; }

    /// <summary>
    /// Obtiene la versión de concurrencia de la factura. Al serializarse
    /// como JSON se representa en Base64.
    /// </summary>
    public required byte[] VersionFila { get; init; }

    /// <summary>
    /// Obtiene el total de notas crédito activas.
    /// </summary>
    public decimal TotalNotasCredito { get; init; }

    /// <summary>
    /// Obtiene el total de notas débito activas.
    /// </summary>
    public decimal TotalNotasDebito { get; init; }

    /// <summary>
    /// Obtiene el total bruto de pagos aplicados.
    /// </summary>
    public decimal TotalPagosAplicados { get; init; }

    /// <summary>
    /// Obtiene el valor de las glosas pendientes.
    /// </summary>
    public decimal ValorGlosaPendiente { get; init; }

    /// <summary>
    /// Obtiene el saldo contable de cartera.
    /// </summary>
    public decimal SaldoCartera { get; init; }

    /// <summary>
    /// Obtiene el saldo disponible después de separar
    /// las glosas pendientes.
    /// </summary>
    public decimal SaldoDisponibleGestion { get; init; }
}
