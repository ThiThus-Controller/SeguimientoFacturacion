using SeguimientoFacturacion.Application.Common.Exceptions;
using SeguimientoFacturacion.Application.Common.Models;
using SeguimientoFacturacion.Application.DTOs.Glosas;
using SeguimientoFacturacion.Application.Interfaces.Persistence;
using SeguimientoFacturacion.Application.Services;
using SeguimientoFacturacion.Application.Validators.Glosas;

namespace SeguimientoFacturacion.Application.Tests.Services;

public sealed class ServicioConsultaGlosasTests
{
    [Fact]
    public async Task Buscar_FiltroValido_DebeDelegarConsulta()
    {
        var consulta = new ConsultaFalsa();
        var servicio = new ServicioConsultaGlosas(
            consulta,
            new FiltroGlosasDtoValidator());
        var filtro = new FiltroGlosasDto
        {
            TextoBusqueda = "FE100",
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
        var servicio = new ServicioConsultaGlosas(
            consulta,
            new FiltroGlosasDtoValidator());

        await Assert.ThrowsAsync<ExcepcionValidacionAplicacion>(
            () => servicio.BuscarAsync(
                new FiltroGlosasDto
                {
                    Pagina = 0
                }));

        Assert.Equal(0, consulta.Consultas);
        Assert.Null(consulta.UltimoFiltro);
    }

    private sealed class ConsultaFalsa : IConsultaGlosas
    {
        public int Consultas { get; private set; }
        public FiltroGlosasDto? UltimoFiltro { get; private set; }

        public Task<ResultadoPaginado<GlosaResumenDto>> BuscarAsync(
            FiltroGlosasDto filtro,
            CancellationToken cancellationToken = default)
        {
            Consultas++;
            UltimoFiltro = filtro;

            return Task.FromResult(
                new ResultadoPaginado<GlosaResumenDto>(
                    elementos: [],
                    totalRegistros: 0,
                    pagina: filtro.Pagina,
                    tamanoPagina: filtro.TamanoPagina));
        }
    }
}
