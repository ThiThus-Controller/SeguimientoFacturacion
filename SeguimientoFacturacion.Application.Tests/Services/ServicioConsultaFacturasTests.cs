using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Facturas;

namespace SeguimientoFacturacion.Application.Tests.Services;

/// <summary>
/// Pruebas del servicio de consulta de facturas.
/// </summary>
public sealed class ServicioConsultaFacturasTests
{
    [Fact]
    public async Task BuscarAsync_ConFiltroValido_DebeEjecutarConsulta()
    {
        var resultadoEsperado =
            new ResultadoPaginado<FacturaResumenDto>(
                Array.Empty<FacturaResumenDto>(),
                totalRegistros: 0,
                pagina: 1,
                tamanoPagina: 50);

        var consulta = new ConsultaFacturasFalsa(
            resultadoEsperado);

        var servicio = new ServicioConsultaFacturas(
            consulta,
            new FiltroFacturasDtoValidator());

        var filtro = new FiltroFacturasDto();

        using var cancellationTokenSource =
            new CancellationTokenSource();

        var resultado = await servicio.BuscarAsync(
            filtro,
            cancellationTokenSource.Token);

        Assert.Same(resultadoEsperado, resultado);
        Assert.Same(filtro, consulta.UltimoFiltro);

        Assert.Equal(
            cancellationTokenSource.Token,
            consulta.UltimoCancellationToken);

        Assert.Equal(1, consulta.NumeroInvocaciones);
    }

    [Fact]
    public async Task BuscarAsync_ConFiltroInvalido_NoDebeEjecutarConsulta()
    {
        var resultadoConsulta =
            new ResultadoPaginado<FacturaResumenDto>(
                Array.Empty<FacturaResumenDto>(),
                totalRegistros: 0,
                pagina: 1,
                tamanoPagina: 50);

        var consulta = new ConsultaFacturasFalsa(
            resultadoConsulta);

        var servicio = new ServicioConsultaFacturas(
            consulta,
            new FiltroFacturasDtoValidator());

        var filtro = new FiltroFacturasDto
        {
            Pagina = 0
        };

        var excepcion =
            await Assert.ThrowsAsync<
                ExcepcionValidacionAplicacion>(
                    () => servicio.BuscarAsync(filtro));

        Assert.Contains(
            nameof(FiltroFacturasDto.Pagina),
            excepcion.Errores.Keys);

        Assert.Equal(0, consulta.NumeroInvocaciones);
    }

    private sealed class ConsultaFacturasFalsa :
        IConsultaFacturas
    {
        private readonly ResultadoPaginado<FacturaResumenDto>
            _resultado;

        public ConsultaFacturasFalsa(
            ResultadoPaginado<FacturaResumenDto> resultado)
        {
            _resultado = resultado;
        }

        public int NumeroInvocaciones { get; private set; }

        public FiltroFacturasDto? UltimoFiltro { get; private set; }

        public CancellationToken UltimoCancellationToken
        {
            get;
            private set;
        }

        public Task<ResultadoPaginado<FacturaResumenDto>>
            BuscarAsync(
                FiltroFacturasDto filtro,
                CancellationToken cancellationToken = default)
        {
            NumeroInvocaciones++;
            UltimoFiltro = filtro;
            UltimoCancellationToken = cancellationToken;

            return Task.FromResult(_resultado);
        }
    }
}