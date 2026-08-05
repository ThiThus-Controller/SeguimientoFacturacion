using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class RepositorioFacturadoresEfCoreTests
{
    [Fact]
    public async Task ObtenerSiguienteCodigo_SinRegistros_DebeRetornarUno()
    {
        await using var contexto = CrearContexto();
        var repositorio = new RepositorioFacturadoresEfCore(contexto);

        var resultado = await repositorio.ObtenerSiguienteCodigoAsync();

        Assert.Equal(1, resultado);
    }

    [Fact]
    public async Task ObtenerSiguienteCodigo_DebeRetornarMaximoMasUno()
    {
        await using var contexto = CrearContexto();

        contexto.Facturadores.AddRange(
            new Facturador(2, "Facturador dos"),
            new Facturador(41, "Facturador cuarenta y uno"),
            new Facturador(7, "Facturador siete"));

        await contexto.SaveChangesAsync();

        var repositorio = new RepositorioFacturadoresEfCore(contexto);

        var resultado = await repositorio.ObtenerSiguienteCodigoAsync();

        Assert.Equal(42, resultado);
    }

    [Fact]
    public async Task ObtenerSiguienteCodigo_ConMaximoEntero_DebeRechazarlo()
    {
        await using var contexto = CrearContexto();

        contexto.Facturadores.Add(
            new Facturador(int.MaxValue, "Último facturador"));

        await contexto.SaveChangesAsync();

        var repositorio = new RepositorioFacturadoresEfCore(contexto);

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
