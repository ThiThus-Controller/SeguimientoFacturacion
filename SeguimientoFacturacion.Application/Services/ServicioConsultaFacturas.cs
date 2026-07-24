using FluentValidation;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;

namespace SeguimientoFacturacion.Application.Services;

/// <summary>
/// Implementa el caso de uso encargado de consultar facturas.
/// </summary>
public sealed class ServicioConsultaFacturas :
    IServicioConsultaFacturas
{
    private readonly IConsultaFacturas _consultaFacturas;
    private readonly IValidator<FiltroFacturasDto> _validator;

    /// <summary>
    /// Inicializa el servicio de consulta de facturas.
    /// </summary>
    /// <param name="consultaFacturas">
    /// Abstracción de persistencia para consultas de facturas.
    /// </param>
    /// <param name="validator">
    /// Validador de los filtros de consulta.
    /// </param>
    public ServicioConsultaFacturas(
        IConsultaFacturas consultaFacturas,
        IValidator<FiltroFacturasDto> validator)
    {
        ArgumentNullException.ThrowIfNull(consultaFacturas);
        ArgumentNullException.ThrowIfNull(validator);

        _consultaFacturas = consultaFacturas;
        _validator = validator;
    }

    /// <inheritdoc />
    public async Task<ResultadoPaginado<FacturaResumenDto>> BuscarAsync(
        FiltroFacturasDto filtro,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var resultadoValidacion =
            await _validator.ValidateAsync(
                filtro,
                cancellationToken);

        if (!resultadoValidacion.IsValid)
        {
            throw new ExcepcionValidacionAplicacion(
                resultadoValidacion.Errors);
        }

        return await _consultaFacturas.BuscarAsync(
            filtro,
            cancellationToken);
    }
}