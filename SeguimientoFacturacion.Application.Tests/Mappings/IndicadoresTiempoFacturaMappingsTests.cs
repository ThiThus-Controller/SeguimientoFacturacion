using SeguimientoFacturacion.Application.Mappings;
using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;
using SeguimientoFacturacion.Domain.Services;

namespace SeguimientoFacturacion.Application.Tests.Mappings;

public sealed class IndicadoresTiempoFacturaMappingsTests
{
    [Fact]
    public void ToDto_DebeConservarIndicadoresYConteos()
    {
        var factura = new Factura(
            prefijo: "FE",
            numero: "100",
            fechaFactura: new DateOnly(2026, 1, 1),
            aseguradoraId: 1,
            valor: 1_000m,
            fechaRadicacion: new DateOnly(2026, 1, 5),
            tipoDocumentoId: 1,
            numeroDocumento: "1000000",
            nombreCompleto: "Paciente de prueba",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: null,
            fechaAdmision: null,
            estadoId: 1,
            facturadorId: 1);

        var glosa = new Glosa(
            factura.Id,
            new DateOnly(2026, 1, 10),
            100m);

        var resumen = new CalculadoraIndicadoresTiempoFactura()
            .Calcular(
                factura,
                new[] { glosa },
                new DateOnly(2026, 1, 20));

        var resultado = resumen.ToDto();

        Assert.Equal(new DateOnly(2026, 1, 20), resultado.FechaCorte);
        Assert.Equal(4, resultado.FacturaARadicacion.Dias);
        Assert.Equal(5, resultado.RadicacionAPrimeraObjecion.Dias);
        Assert.Equal(10, resultado.MaximoObjecionARespuesta.Dias);
        Assert.Equal(
            EstadoIndicadorPlazo.Pendiente,
            resultado.MaximoObjecionARespuesta.Estado);
        Assert.Equal(1, resultado.TotalGlosas);
        Assert.Equal(1, resultado.GlosasPendientes);
    }
}
