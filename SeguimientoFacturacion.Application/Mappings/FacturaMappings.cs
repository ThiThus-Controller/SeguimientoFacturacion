using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.ValueObjects;

namespace SeguimientoFacturacion.Application.Mappings;

/// <summary>
/// Contiene conversiones explícitas relacionadas
/// con facturas.
/// </summary>
public static class FacturaMappings
{
    /// <summary>
    /// Convierte una factura y su resumen financiero
    /// en un DTO para la grilla principal.
    /// </summary>
    /// <param name="factura">
    /// Factura que contiene la información administrativa.
    /// </param>
    /// <param name="resumenSaldo">
    /// Resultado financiero calculado para la factura.
    /// </param>
    /// <returns>
    /// DTO preparado para presentación.
    /// </returns>
    public static FacturaResumenDto ToResumenDto(
        this Factura factura,
        ResumenSaldoFactura resumenSaldo)
    {
        ArgumentNullException.ThrowIfNull(factura);
        ArgumentNullException.ThrowIfNull(resumenSaldo);

        return new FacturaResumenDto
        {
            Id = factura.Id,
            Prefijo = factura.Prefijo,
            Numero = factura.Numero,
            FechaFactura = factura.FechaFactura,

            AseguradoraId = factura.AseguradoraId,
            Aseguradora =
                factura.Aseguradora?.Descripcion ??
                string.Empty,

            Valor = factura.Valor,
            FechaRadicacion = factura.FechaRadicacion,
            DiasHastaRadicacion =
                factura.DiasHastaRadicacion,

            TipoDocumentoId =
                factura.TipoDocumentoId,

            TipoDocumentoSigla =
                factura.TipoDocumento?.Sigla ??
                string.Empty,

            NumeroDocumento =
                factura.NumeroDocumento,

            NombreCompleto =
                factura.NombreCompleto,

            AtencionId = factura.AtencionId,
            Atencion =
                factura.Atencion?.Descripcion ??
                string.Empty,

            CostoId = factura.CostoId,
            Costo =
                factura.Costo?.Descripcion ??
                string.Empty,

            NumeroAdmision =
                factura.NumeroAdmision,

            FechaAdmision =
                factura.FechaAdmision,

            EstadoId = factura.EstadoId,
            Estado =
                factura.Estado?.Descripcion ??
                string.Empty,

            FacturadorId =
                factura.FacturadorId,

            Facturador =
                factura.Facturador?.Nombre ??
                string.Empty,

            TotalNotasCredito =
                resumenSaldo.TotalNotasCredito,

            TotalNotasDebito =
                resumenSaldo.TotalNotasDebito,

            TotalPagosAplicados =
                resumenSaldo.TotalPagosAplicados,

            ValorGlosaPendiente =
                resumenSaldo.ValorGlosaPendiente,

            SaldoCartera =
                resumenSaldo.SaldoCartera,

            SaldoDisponibleGestion =
                resumenSaldo.SaldoDisponibleGestion
        };
    }
}