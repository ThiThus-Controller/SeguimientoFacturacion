using Microsoft.Extensions.DependencyInjection;
using SeguimientoFacturacion.Application;
using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Pagos;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Interfaces.Services;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Pagos;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioConsultaPagosTests
{
    [Fact]
    public async Task Buscar_FiltroValido_DebeDelegarConsulta()
    {
        var consulta = new ConsultaFalsa();
        var servicio = CrearServicio(consulta);
        var filtro = new FiltroPagosDto
        {
            TextoBusqueda = "REC-100",
            Pagina = 2,
            TamanoPagina = 10
        };

        var resultado = await servicio.BuscarAsync(filtro);

        Assert.Same(filtro, consulta.UltimoFiltro);
        Assert.Equal(1, consulta.Consultas);
        Assert.Equal(2, resultado.Pagina);
    }

    [Fact]
    public async Task Buscar_FiltroInvalido_DebeEvitarConsulta()
    {
        var consulta = new ConsultaFalsa();
        var servicio = CrearServicio(consulta);

        await Assert.ThrowsAsync<ExcepcionValidacionAplicacion>(
            () => servicio.BuscarAsync(
                new FiltroPagosDto { Pagina = 0 }));

        Assert.Equal(0, consulta.Consultas);
    }

    [Fact]
    public async Task ObtenerDetalle_DebeDelegarIdentificador()
    {
        var consulta = new ConsultaFalsa();
        var servicio = CrearServicio(consulta);
        var pagoId = Guid.NewGuid();

        await servicio.ObtenerDetalleAsync(pagoId);

        Assert.Equal(pagoId, consulta.UltimoPagoId);
    }

    [Fact]
    public async Task ObtenerDetalle_IdVacio_DebeRechazar()
    {
        var consulta = new ConsultaFalsa();
        var servicio = CrearServicio(consulta);

        await Assert.ThrowsAsync<ArgumentException>(
            () => servicio.ObtenerDetalleAsync(Guid.Empty));

        Assert.Null(consulta.UltimoPagoId);
    }

    [Fact]
    public async Task BuscarFacturasAnticipo_DebeDelegarPaginacion()
    {
        var consulta = new ConsultaFalsa();
        var servicio = CrearServicio(consulta);

        var resultado = await servicio.BuscarFacturasAnticipoAsync(
            2,
            "FE100",
            3,
            10);

        Assert.Equal(2, consulta.UltimaAseguradoraId);
        Assert.Equal("FE100", consulta.UltimoTextoAnticipo);
        Assert.Equal(3, resultado.Pagina);
    }

    [Fact]
    public void DependencyInjection_DebeRegistrarServicio()
    {
        ServiceCollection servicios = new();
        servicios.AddApplication();

        var descriptor = servicios.Single(
            elemento => elemento.ServiceType ==
                typeof(IServicioConsultaPagos));

        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
        Assert.Equal(
            typeof(ServicioConsultaPagos),
            descriptor.ImplementationType);
    }

    private static ServicioConsultaPagos CrearServicio(
        IConsultaPagos consulta) =>
        new(consulta, new FiltroPagosDtoValidator());

    private sealed class ConsultaFalsa : IConsultaPagos
    {
        public int Consultas { get; private set; }
        public FiltroPagosDto? UltimoFiltro { get; private set; }
        public Guid? UltimoPagoId { get; private set; }
        public int? UltimaAseguradoraId { get; private set; }
        public string? UltimoTextoAnticipo { get; private set; }

        public Task<ResultadoPaginado<PagoResumenGeneralDto>>
            BuscarAsync(
                FiltroPagosDto filtro,
                CancellationToken cancellationToken = default)
        {
            Consultas++;
            UltimoFiltro = filtro;
            return Task.FromResult(
                new ResultadoPaginado<PagoResumenGeneralDto>(
                    [], 0, filtro.Pagina, filtro.TamanoPagina));
        }

        public Task<PagoDetalleDto?> ObtenerDetalleAsync(
            Guid pagoId,
            CancellationToken cancellationToken = default)
        {
            UltimoPagoId = pagoId;
            return Task.FromResult<PagoDetalleDto?>(null);
        }

        public Task<IReadOnlyList<AnticipoEntidadResumenDto>>
            ListarAnticiposPorEntidadAsync(
                CancellationToken cancellationToken = default)
        {
            IReadOnlyList<AnticipoEntidadResumenDto> resultado = [];
            return Task.FromResult(resultado);
        }

        public Task<ResultadoPaginado<AnticipoFacturaResumenDto>>
            BuscarFacturasAnticipoAsync(
                int aseguradoraId,
                string? textoBusqueda,
                int pagina,
                int tamanoPagina,
                CancellationToken cancellationToken = default)
        {
            UltimaAseguradoraId = aseguradoraId;
            UltimoTextoAnticipo = textoBusqueda;
            return Task.FromResult(
                new ResultadoPaginado<AnticipoFacturaResumenDto>(
                    [], 0, pagina, tamanoPagina));
        }
    }
}
