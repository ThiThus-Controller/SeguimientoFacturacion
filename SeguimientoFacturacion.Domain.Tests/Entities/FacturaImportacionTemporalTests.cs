using SeguimientoFacturacion.Domain.Entities;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

/// <summary>
/// Pruebas de la fila temporal de facturación.
/// </summary>
public sealed class FacturaImportacionTemporalTests
{
    [Fact]
    public void Crear_ConDatosValidos_DebeNormalizarlos()
    {
        var loteId = Guid.NewGuid();

        var registro = CrearRegistro(
            loteId,
            fechaRadicacion:
                new DateOnly(2026, 7, 20),
            fechaAdmision:
                new DateOnly(2026, 7, 10));

        Assert.NotEqual(Guid.Empty, registro.Id);
        Assert.Equal(loteId, registro.LoteImportacionId);
        Assert.Equal("Facturas", registro.HojaOrigen);
        Assert.Equal(2, registro.FilaOrigen);
        Assert.Equal("FE001", registro.IdentificadorFe);
        Assert.Equal("FV", registro.Prefijo);
        Assert.Equal("000001", registro.Numero);
        Assert.Equal("123ABC", registro.NumeroDocumento);
        Assert.Equal("Paciente de prueba", registro.NombreCompleto);
        Assert.Equal("ADM-001", registro.NumeroAdmision);
    }

    [Fact]
    public void Crear_ConLoteVacio_DebeLanzarExcepcion()
    {
        Assert.Throws<ArgumentException>(
            () => CrearRegistro(Guid.Empty));
    }

    [Fact]
    public void Crear_ConFilaCero_DebeLanzarExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CrearRegistro(
                Guid.NewGuid(),
                filaOrigen: 0));
    }

    [Fact]
    public void Crear_ConRadicacionAnterior_DebeLanzarExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CrearRegistro(
                Guid.NewGuid(),
                fechaRadicacion:
                    new DateOnly(2026, 7, 14)));
    }

    [Fact]
    public void Crear_ConAdmisionPosterior_DebeLanzarExcepcion()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CrearRegistro(
                Guid.NewGuid(),
                fechaAdmision:
                    new DateOnly(2026, 7, 16)));
    }

    [Fact]
    public void Crear_ConCamposOpcionalesVacios_DebeUsarNull()
    {
        var registro = CrearRegistro(
            Guid.NewGuid(),
            numeroAdmision: " ",
            fechaRadicacion: null,
            fechaAdmision: null);

        Assert.Null(registro.NumeroAdmision);
        Assert.Null(registro.FechaRadicacion);
        Assert.Null(registro.FechaAdmision);
    }

    private static FacturaImportacionTemporal CrearRegistro(
        Guid loteId,
        int filaOrigen = 2,
        string? numeroAdmision = " adm-001 ",
        DateOnly? fechaRadicacion = null,
        DateOnly? fechaAdmision = null)
    {
        return new FacturaImportacionTemporal(
            loteImportacionId: loteId,
            hojaOrigen: " Facturas ",
            filaOrigen: filaOrigen,
            identificadorFe: " fe001 ",
            prefijo: " fv ",
            numero: " 000001 ",
            fechaFactura:
                new DateOnly(2026, 7, 15),
            aseguradoraId: 1,
            valor: 150000m,
            fechaRadicacion: fechaRadicacion,
            tipoDocumentoId: 1,
            numeroDocumento: " 123abc ",
            nombreCompleto: " Paciente de prueba ",
            atencionId: 1,
            costoId: 1,
            numeroAdmision: numeroAdmision,
            fechaAdmision: fechaAdmision,
            estadoId: 1,
            facturadorId: 1);
    }
}