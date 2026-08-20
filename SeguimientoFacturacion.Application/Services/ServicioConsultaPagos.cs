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

    /// <inheritdoc />
    public Task<IReadOnlyList<AnticipoEntidadResumenDto>>
        ListarAnticiposPorEntidadAsync(
            CancellationToken cancellationToken = default)
    {
        return _consulta.ListarAnticiposPorEntidadAsync(
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<ResultadoPaginado<AnticipoFacturaResumenDto>>
        BuscarFacturasAnticipoAsync(
            int aseguradoraId,
            string? textoBusqueda,
            int pagina,
            int tamanoPagina,
            CancellationToken cancellationToken = default)
    {
        if (aseguradoraId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(aseguradoraId));
        }

        if (pagina <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(pagina));
        }

        if (tamanoPagina is < 1 or > 50)
        {
            throw new ArgumentOutOfRangeException(
                nameof(tamanoPagina));
        }

        if (!string.IsNullOrEmpty(textoBusqueda) &&
            textoBusqueda.Trim().Length > 100)
        {
            throw new ArgumentException(
                "La búsqueda admite máximo 100 caracteres.",
                nameof(textoBusqueda));
        }

        return _consulta.BuscarFacturasAnticipoAsync(
            aseguradoraId,
            textoBusqueda?.Trim(),
            pagina,
            tamanoPagina,
            cancellationToken);
    }
}
