using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Notas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Notas;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioConsultaNotasFacturaTests
{
    [Fact]
    public async Task Buscar_FiltroValido_DebeDelegarConsulta()
    {
        var consulta = new ConsultaFalsa();
        var servicio = new ServicioConsultaNotasFactura(
            consulta,
            new FiltroNotasFacturaDtoValidator());
        var filtro = new FiltroNotasFacturaDto
        {
            TextoBusqueda = "NC-100",
            Pagina = 2,
            TamanoPagina = 10
        };

        var resultado = await servicio.BuscarAsync(filtro);

        Assert.Same(filtro, consulta.UltimoFiltro);
        Assert.Equal(1, consulta.Consultas);
        Assert.Equal(2, resultado.Pagina);
        Assert.Equal(10, resultado.TamanoPagina);
    }

    [Fact]
    public async Task Buscar_FiltroInvalido_DebeEvitarConsulta()
    {
        var consulta = new ConsultaFalsa();
        var servicio = new ServicioConsultaNotasFactura(
            consulta,
            new FiltroNotasFacturaDtoValidator());

        await Assert.ThrowsAsync<ExcepcionValidacionAplicacion>(
            () => servicio.BuscarAsync(
                new FiltroNotasFacturaDto
                {
                    Pagina = 0
                }));

        Assert.Equal(0, consulta.Consultas);
        Assert.Null(consulta.UltimoFiltro);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarServicio()
    {
        ServiceCollection servicios = new();

        servicios.AddApplication();

        var descriptor = servicios.Single(
            elemento => elemento.ServiceType ==
                typeof(IServicioConsultaNotasFactura));

        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        Assert.Equal(
            typeof(ServicioConsultaNotasFactura),
            descriptor.ImplementationType);
    }

    private sealed class ConsultaFalsa : IConsultaNotasFactura
    {
        public int Consultas { get; private set; }
        public FiltroNotasFacturaDto? UltimoFiltro { get; private set; }

        public Task<ResultadoPaginado<NotaFacturaResumenGeneralDto>>
            BuscarAsync(
                FiltroNotasFacturaDto filtro,
                CancellationToken cancellationToken = default)
        {
            Consultas++;
            UltimoFiltro = filtro;

            return Task.FromResult(
                new ResultadoPaginado<NotaFacturaResumenGeneralDto>(
                    elementos: [],
                    totalRegistros: 0,
                    pagina: filtro.Pagina,
                    tamanoPagina: filtro.TamanoPagina));
        }
    }
}
