using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class RepositorioAseguradorasEfCoreTests
{
    [Fact]
    public async Task ObtenerSiguienteCodigo_SinRegistros_DebeRetornarUno()
    {
        await using var contexto = CrearContexto();
        var repositorio = new RepositorioAseguradorasEfCore(contexto);

        var resultado = await repositorio.ObtenerSiguienteCodigoAsync();

        Assert.Equal(1, resultado);
    }

    [Fact]
    public async Task ObtenerSiguienteCodigo_DebeRetornarMaximoMasUno()
    {
        await using var contexto = CrearContexto();

        contexto.Aseguradoras.AddRange(
            new Aseguradora(2, "Aseguradora dos"),
            new Aseguradora(41, "Aseguradora cuarenta y uno"),
            new Aseguradora(7, "Aseguradora siete"));

        await contexto.SaveChangesAsync();

        var repositorio = new RepositorioAseguradorasEfCore(contexto);

        var resultado = await repositorio.ObtenerSiguienteCodigoAsync();

        Assert.Equal(42, resultado);
    }

    [Fact]
    public async Task ObtenerSiguienteCodigo_ConMaximoEntero_DebeRechazarlo()
    {
        await using var contexto = CrearContexto();

        contexto.Aseguradoras.Add(
            new Aseguradora(int.MaxValue, "Última aseguradora"));

        await contexto.SaveChangesAsync();

        var repositorio = new RepositorioAseguradorasEfCore(contexto);

        var accion = () => repositorio.ObtenerSiguienteCodigoAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(accion);
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var options =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString("N"))
                .Options;

        return new SeguimientoDbContext(options);
    }
}
