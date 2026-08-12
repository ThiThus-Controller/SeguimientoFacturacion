using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class
    NotaFacturaImportacionTemporalTests
{
    [Fact]
    public void
        Crear_DatosValidos_DebeNormalizarYCalcularImpacto()
    {
        var loteId =
            Guid.NewGuid();

        var registro =
            new NotaFacturaImportacionTemporal(
                loteImportacionId: loteId,
                hojaOrigen: " Notas ",
                filaOrigen: 2,
                identificadorFe: " fe000001 ",
                prefijo: " fe ",
                numeroFactura: " 000001 ",
                aseguradoraId: 1,
                tipo: TipoNotaFactura.Credito,
                fechaNota:
                    new DateOnly(2026, 7, 29),
                numeroNota: " nc-001 ",
                valorNota: 100000m,
                glosaId: Guid.NewGuid());

        Assert.NotEqual(Guid.Empty, registro.Id);
        Assert.Equal(loteId, registro.LoteImportacionId);
        Assert.Equal("Notas", registro.HojaOrigen);
        Assert.Equal(2, registro.FilaOrigen);
        Assert.Equal("FE000001", registro.IdentificadorFe);
        Assert.Equal("FE", registro.Prefijo);
        Assert.Equal("000001", registro.NumeroFactura);
        Assert.Equal(1, registro.AseguradoraId);

        Assert.Equal(
            TipoNotaFactura.Credito,
            registro.Tipo);

        Assert.Equal(
            new DateOnly(2026, 7, 29),
            registro.FechaNota);

        Assert.Equal("NC-001", registro.NumeroNota);
        Assert.Equal(100000m, registro.ValorNota);
        Assert.Equal(-100000m, registro.ImpactoSaldo);
    }

    [Fact]
    public void
        Crear_LoteVacio_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearRegistro(
                loteId: Guid.Empty);
        }

        Assert.Throws<ArgumentException>(Accion);
    }

    [Fact]
    public void
        Crear_TipoInvalido_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearRegistro(
                tipo:
                    (TipoNotaFactura)999);
        }

        Assert.Throws<
            ArgumentOutOfRangeException>(Accion);
    }

    [Fact]
    public void
        Crear_ValorNoPositivo_DebeLanzarExcepcion()
    {
        void Accion()
        {
            _ = CrearRegistro(
                valorNota: decimal.Zero);
        }

        Assert.Throws<
            ArgumentOutOfRangeException>(Accion);
    }

    private static
        NotaFacturaImportacionTemporal CrearRegistro(
            Guid? loteId = null,
            TipoNotaFactura tipo =
                TipoNotaFactura.Credito,
            decimal valorNota = 100000m)
    {
        return new NotaFacturaImportacionTemporal(
            loteImportacionId:
                loteId ?? Guid.NewGuid(),
            hojaOrigen: "Notas",
            filaOrigen: 2,
            identificadorFe: "FE000001",
            prefijo: "FE",
            numeroFactura: "000001",
            aseguradoraId: 1,
            tipo: tipo,
            fechaNota:
                new DateOnly(2026, 7, 29),
            numeroNota: "NC-001",
            valorNota: valorNota,
            glosaId: tipo == TipoNotaFactura.Credito
                ? Guid.NewGuid()
                : null);
    }

    [Fact]
    public void Crear_NotaCreditoSinGlosa_DebeLanzarExcepcion()
    {
        var accion = () => new NotaFacturaImportacionTemporal(
            loteImportacionId: Guid.NewGuid(),
            hojaOrigen: "Notas",
            filaOrigen: 2,
            identificadorFe: "FE000001",
            prefijo: "FE",
            numeroFactura: "000001",
            aseguradoraId: 1,
            tipo: TipoNotaFactura.Credito,
            fechaNota: new DateOnly(2026, 7, 29),
            numeroNota: "NC-001",
            valorNota: 100000m);

        Assert.Throws<ArgumentException>(accion);
    }
}
