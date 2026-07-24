using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Application.Mappings;

/// <summary>
/// Contiene conversiones explícitas relacionadas con facturas.
/// </summary>
public static class FacturaMappings
{
    /// <summary>
    /// Convierte una entidad Factura en un DTO para la grilla.
    /// </summary>
    public static FacturaResumenDto ToResumenDto(
        this Factura factura)
    {
        ArgumentNullException.ThrowIfNull(factura);

        return new FacturaResumenDto
        {
            Id = factura.Id,
            Prefijo = factura.Prefijo,
            Numero = factura.Numero,
            FechaFactura = factura.FechaFactura,

            AseguradoraId = factura.AseguradoraId,
            Aseguradora =
                factura.Aseguradora?.Descripcion ?? string.Empty,

            Valor = factura.Valor,
            FechaRadicacion = factura.FechaRadicacion,
            DiasHastaRadicacion = factura.DiasHastaRadicacion,

            TipoDocumentoId = factura.TipoDocumentoId,
            TipoDocumentoSigla =
                factura.TipoDocumento?.Sigla ?? string.Empty,

            NumeroDocumento = factura.NumeroDocumento,
            NombreCompleto = factura.NombreCompleto,

            AtencionId = factura.AtencionId,
            Atencion =
                factura.Atencion?.Descripcion ?? string.Empty,

            CostoId = factura.CostoId,
            Costo =
                factura.Costo?.Descripcion ?? string.Empty,

            NumeroAdmision = factura.NumeroAdmision,
            FechaAdmision = factura.FechaAdmision,

            EstadoId = factura.EstadoId,
            Estado =
                factura.Estado?.Descripcion ?? string.Empty,

            FacturadorId = factura.FacturadorId,
            Facturador =
                factura.Facturador?.Nombre ?? string.Empty,

            TotalNotasCredito = factura.TotalNotasCredito,
            TotalAbonos = factura.TotalAbonos,

            TotalGlosasODevoluciones =
                factura.TotalGlosasODevoluciones,

            TotalConciliaciones =
                factura.TotalConciliaciones,

            Saldo = factura.Saldo
        };
    }
}