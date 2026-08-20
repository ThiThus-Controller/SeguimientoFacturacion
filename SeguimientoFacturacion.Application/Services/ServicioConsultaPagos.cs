using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la consulta validada de pagos y aplicaciones.
/// </summary>
public sealed class ServicioConsultaPagos : IServicioConsultaPagos
{
    private readonly IConsultaPagos _consulta;
    private readonly IValidator<FiltroPagosDto> _validador;

    public ServicioConsultaPagos(
        IConsultaPagos consulta,
        IValidator<FiltroPagosDto> validador)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        ArgumentNullException.ThrowIfNull(validador);
        _consulta = consulta;
        _validador = validador;
    }

    /// <inheritdoc />
    public async Task<ResultadoPaginado<PagoResumenGeneralDto>>
        BuscarAsync(
            FiltroPagosDto filtro,
            CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var validacion = await _validador.ValidateAsync(
            filtro,
            cancellationToken);

        if (!validacion.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(validacion.Errors);
        }

        return await _consulta.BuscarAsync(filtro, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagoDetalleDto?> ObtenerDetalleAsync(
        Guid pagoId,
        CancellationToken cancellationToken = default)
    {
        if (pagoId == Guid.Empty)
        {
            throw new ArgumentException(
                "El identificador del pago es obligatorio.",
                nameof(pagoId));
        }

        return _consulta.ObtenerDetalleAsync(pagoId, cancellationToken);
    }
}
