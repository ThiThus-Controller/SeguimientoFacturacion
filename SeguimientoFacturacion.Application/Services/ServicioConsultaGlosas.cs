using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa la consulta general validada de glosas.
/// </summary>
public sealed class ServicioConsultaGlosas : IServicioConsultaGlosas
{
    private readonly IConsultaGlosas _consulta;
    private readonly IValidator<FiltroGlosasDto> _validador;

    public ServicioConsultaGlosas(
        IConsultaGlosas consulta,
        IValidator<FiltroGlosasDto> validador)
    {
        ArgumentNullException.ThrowIfNull(consulta);
        ArgumentNullException.ThrowIfNull(validador);

        _consulta = consulta;
        _validador = validador;
    }

    /// <inheritdoc />
    public async Task<ResultadoPaginado<GlosaResumenDto>> BuscarAsync(
        FiltroGlosasDto filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var resultadoValidacion = await _validador.ValidateAsync(
            filtro,
            cancellationToken);

        if (!resultadoValidacion.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(
                resultadoValidacion.Errors);
        }

        return await _consulta.BuscarAsync(
            filtro,
            cancellationToken);
    }
}
