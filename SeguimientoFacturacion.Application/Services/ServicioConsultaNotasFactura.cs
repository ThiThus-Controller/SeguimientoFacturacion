using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la consulta general validada de notas factura.
/// </summary>
public sealed class ServicioConsultaNotasFactura :
    IServicioConsultaNotasFactura
{
    private readonly IConsultaNotasFactura _consulta;
    private readonly IValidator<FiltroNotasFacturaDto> _validador;

    public ServicioConsultaNotasFactura(
        IConsultaNotasFactura consulta,
        IValidator<FiltroNotasFacturaDto> validador)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        ArgumentNullException.ThrowIfNull(validador);
        _consulta = consulta;
        _validador = validador;
    }

    /// <inheritdoc />
    public async Task<ResultadoPaginado<NotaFacturaResumenGeneralDto>>
        BuscarAsync(
            FiltroNotasFacturaDto filtro,
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
}
