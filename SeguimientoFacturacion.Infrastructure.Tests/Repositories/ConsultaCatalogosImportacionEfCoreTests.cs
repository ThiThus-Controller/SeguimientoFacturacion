using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class ConsultaCatalogosImportacionEfCoreTests
{
    [Fact]
    public async Task Obtener_DebeExcluirCatalogosAdministrablesInactivos()
    {
        await using var contexto = CrearContexto();

        var aseguradoraActiva = new Aseguradora(101, "Activa");
        var aseguradoraInactiva = new Aseguradora(102, "Inactiva");
        aseguradoraInactiva.Desactivar();

        var facturadorActivo = new Facturador(101, "Activo");
        var facturadorInactivo = new Facturador(102, "Inactivo");
        facturadorInactivo.Desactivar();

        contexto.Aseguradoras.AddRange(
            aseguradoraActiva,
            aseguradoraInactiva);
        contexto.Facturadores.AddRange(
            facturadorActivo,
            facturadorInactivo);

        await contexto.SaveChangesAsync();

        var consulta = new ConsultaCatalogosImportacionEfCore(contexto);

        var resultado = await consulta.ObtenerAsync();

        Assert.Contains(
            resultado.Aseguradoras,
            item => item.Id == aseguradoraActiva.Id);
        Assert.DoesNotContain(
            resultado.Aseguradoras,
            item => item.Id == aseguradoraInactiva.Id);
        Assert.Contains(
            resultado.Facturadores,
            item => item.Id == facturadorActivo.Id);
        Assert.DoesNotContain(
            resultado.Facturadores,
            item => item.Id == facturadorInactivo.Id);
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
