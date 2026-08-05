using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class ConsultaLoteImportacionDuplicadoEfCoreTests
{
    private const string HashArchivo =
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA" +
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";

    [Fact]
    public async Task Obtener_AnalizadoSinErrores_DebeRecuperarlo()
    {
        await using var contexto = CrearContexto();
        var lote = CrearLote(totalErrores: 0);

        contexto.LotesImportacion.Add(lote);
        await contexto.SaveChangesAsync();

        var consulta =
            new ConsultaLoteImportacionDuplicadoEfCore(
                contexto);

        var resultado = await consulta.ObtenerAsync(
            TipoImportacion.NotasFactura,
            HashArchivo.ToLowerInvariant());

        Assert.NotNull(resultado);
        Assert.Equal(lote.Id, resultado.LoteId);
        Assert.Equal(EstadoImportacion.Analizada, resultado.Estado);
        Assert.Equal(0, resultado.TotalErrores);
        Assert.True(resultado.PuedeContinuarConfirmacion);
    }

    [Fact]
    public async Task Obtener_SoloAnalizadoConErrores_DebePermitirReintento()
    {
        await using var contexto = CrearContexto();
        contexto.LotesImportacion.Add(
            CrearLote(totalErrores: 1));
        await contexto.SaveChangesAsync();

        var consulta =
            new ConsultaLoteImportacionDuplicadoEfCore(
                contexto);

        var resultado = await consulta.ObtenerAsync(
            TipoImportacion.NotasFactura,
            HashArchivo);

        Assert.Null(resultado);
    }

    private static LoteImportacion CrearLote(
        int totalErrores)
    {
        var lote = new LoteImportacion(
            TipoImportacion.NotasFactura,
            "NotasFactura.xlsx",
            HashArchivo);

        lote.RegistrarCreacion(
            new DateTimeOffset(
                2026, 8, 5, 20, 0, 0, TimeSpan.Zero),
            "pruebas");

        var filasConError = totalErrores > 0 ? 1 : 0;

        lote.RegistrarAnalisis(
            totalFilas: 18,
            totalFilasValidas: 18 - filasConError,
            totalFilasConError: filasConError,
            totalAdvertencias: 0,
            fechaAnalisis: new DateTimeOffset(
                2026, 8, 5, 20, 1, 0, TimeSpan.Zero),
            totalErrores: totalErrores);

        return lote;
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
