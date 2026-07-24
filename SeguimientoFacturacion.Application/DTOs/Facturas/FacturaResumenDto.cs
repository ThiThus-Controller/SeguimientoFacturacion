namespace SeguimientoFacturacion.Application.DTOs.Facturas;

/// <summary>
/// Representa una factura preparada para mostrarse
/// en la grilla principal.
/// </summary>
public sealed record FacturaResumenDto
{
    public required string Id { get; init; }

    public required string Prefijo { get; init; }

    public required string Numero { get; init; }

    public DateOnly FechaFactura { get; init; }

    public int AseguradoraId { get; init; }

    public required string Aseguradora { get; init; }

    public decimal Valor { get; init; }

    public DateOnly? FechaRadicacion { get; init; }

    public int? DiasHastaRadicacion { get; init; }

    public int TipoDocumentoId { get; init; }

    public required string TipoDocumentoSigla { get; init; }

    public required string NumeroDocumento { get; init; }

    public required string NombreCompleto { get; init; }

    public int AtencionId { get; init; }

    public required string Atencion { get; init; }

    public int CostoId { get; init; }

    public required string Costo { get; init; }

    public string? NumeroAdmision { get; init; }

    public DateOnly? FechaAdmision { get; init; }

    public int EstadoId { get; init; }

    public required string Estado { get; init; }

    public int FacturadorId { get; init; }

    public required string Facturador { get; init; }

    public decimal TotalNotasCredito { get; init; }

    public decimal TotalAbonos { get; init; }

    public decimal TotalGlosasODevoluciones { get; init; }

    public decimal TotalConciliaciones { get; init; }

    public decimal Saldo { get; init; }
}