using Microsoft.EntityFrameworkCore;
using SeguimientoFacturacion.Application.DTOs.Facturas;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Entities.Catalogos;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Infrastructure.Persistence;
using SeguimientoFacturacion.Infrastructure.Repositories;

namespace SeguimientoFacturacion.Infrastructure.Tests.Repositories;

public sealed class ConsultaFacturasEfCoreTests
{
    [Fact]
    public async Task Buscar_DebeConsolidarMovimientosFinancieros()
    {
        await using var contexto = CrearContexto();
        var factura = CrearFactura();
        var glosa = new Glosa(
            factura.Id,
            new DateOnly(2026, 8, 3),
            300m);
        glosa.Resolver(
            EstadoGlosa.Conciliada,
            new DateOnly(2026, 8, 4),
            100m,
            "Conciliación de prueba.");
        var glosaAnulada = new Glosa(
            factura.Id,
            new DateOnly(2026, 8, 3),
            50m);
        glosaAnulada.Anular("Registro duplicado.");
        var notaCredito = new NotaFactura(
            factura.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 5),
            "NC-100",
            100m,
            glosa.Id);
        var notaAnulada = new NotaFactura(
            factura.Id,
            TipoNotaFactura.Credito,
            new DateOnly(2026, 8, 5),
            "NC-ANULADA",
            40m,
            glosaAnulada.Id);
        notaAnulada.Anular("Registro duplicado.");
        var pago = CrearPago(factura);
        var paciente = new Paciente(
            1,
            factura.NumeroDocumento,
            factura.NombreCompleto);

        await contexto.AddRangeAsync(
            new Aseguradora(1, "ASEGURADORA PRUEBA"),
            new TipoDocumento(1, "Cédula", "CC"),
            new Atencion(1, "AMBULATORIA"),
            new Costo(1, "GENERAL"),
            new Estado(2, "RADICADA"),
            new Facturador(1, "FACTURADOR PRUEBA"),
            paciente,
            factura,
            glosa,
            glosaAnulada,
            notaCredito,
            notaAnulada,
            pago);
        await contexto.SaveChangesAsync();
        contexto.ChangeTracker.Clear();
        var consulta = new ConsultaFacturasEfCore(contexto);

        var resultado = await consulta.BuscarAsync(
            new FiltroFacturasDto());

        var resumen = Assert.Single(resultado.Elementos);
        Assert.Equal(1000m, resumen.Valor);
        Assert.Equal(300m, resumen.TotalValorGlosas);
        Assert.Equal(100m, resumen.TotalValorAceptadoGlosas);
        Assert.Equal(100m, resumen.TotalNotasCredito);
        Assert.Equal(250m, resumen.TotalPagosAplicados);
        Assert.Equal(50m, resumen.TotalAnticipoDisponible);
        Assert.Equal(650m, resumen.SaldoCartera);
        Assert.Empty(contexto.ChangeTracker.Entries<Factura>());
    }

    private static SeguimientoDbContext CrearContexto()
    {
        var opciones =
            new DbContextOptionsBuilder<SeguimientoDbContext>()
                .UseInMemoryDatabase(
                    $"ConsultaFacturas_{Guid.NewGuid():N}")
                .Options;

        return new SeguimientoDbContext(opciones);
    }

    private static Factura CrearFactura() => new(
        "FE",
        "100",
        new DateOnly(2026, 8, 1),
        1,
        1000m,
        null,
        1,
        "DOC-100",
        "PACIENTE PRUEBA",
        1,
        1,
        "ADM-100",
        new DateOnly(2026, 8, 1),
        2,
        1);

    private static Pago CrearPago(Factura factura)
    {
        var pago = new Pago(
            factura.AseguradoraId,
            new DateOnly(2026, 8, 6),
            "REC-100",
            300m,
            0m,
            0m);
        pago.AgregarAplicacion(
            new AplicacionPago(
                pago.Id,
                factura.Id,
                300m,
                250m,
                50m));
        return pago;
    }
}
