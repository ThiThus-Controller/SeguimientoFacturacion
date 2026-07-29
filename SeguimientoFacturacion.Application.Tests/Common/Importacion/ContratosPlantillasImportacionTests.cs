using SeguimientoFacturacion.Application.Common.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests
    .Common.Importacion;

public sealed class
    ContratosPlantillasImportacionTests
{
    public static IEnumerable<object[]>
        PlantillasValidas()
    {
        yield return
        [
            TipoImportacion.Facturas,
            new[]
            {
                "FE",
                "PREFIJO",
                "FACTURA",
                "FECHA FACTURA",
                "ASEGURADORA",
                "VALOR",
                "FECHA DE RADICACION",
                "TIPO DTO",
                "NUMERO DTO",
                "NOMBRE COMPLETO",
                "ATENCION",
                "COSTO",
                "NO ADMISION",
                "FECHA ADMISION",
                "ESTADO DE DTO",
                "FACTURADOR"
            }
        ];

        yield return
        [
            TipoImportacion.NotasFactura,
            new[]
            {
                "FE",
                "PREFIJO",
                "FACTURA",
                "ASEGURADORA",
                "TIPO NOTA",
                "FECHA NOTA",
                "NUMERO NOTA",
                "VALOR NOTA"
            }
        ];

        yield return
        [
            TipoImportacion.Glosas,
            new[]
            {
                "FE",
                "PREFIJO",
                "FACTURA",
                "ASEGURADORA",
                "FECHA GLOSA",
                "VALOR GLOSA",
                "FECHA RTA GLOSA ",
                "ESTADO GLOSA",
                "VALOR ACEPTADO"
            }
        ];

        yield return
        [
            TipoImportacion.Pagos,
            new[]
            {
                "FE",
                "PREFIJO",
                "FACTURA",
                "ASEGURADORA",
                "VALOR PAGADO",
                "VALOR CRUZADO",
                "RETENCION",
                "RETE ICA ",
                "SALDO FAVOR",
                "SALDO RETENCION",
                "VR PAGADO",
                "VR CRUZADO",
                "FECHA DE PAGO",
                "RECIBO",
                "NOTAS"
            }
        ];
    }

    [Theory]
    [MemberData(nameof(PlantillasValidas))]
    public void Detectar_PlantillaValida_DebeResolverTipo(
        TipoImportacion tipoEsperado,
        string[] encabezados)
    {
        var resultado =
            ContratosPlantillasImportacion
                .Detectar(encabezados);

        Assert.NotNull(resultado);
        Assert.Equal(tipoEsperado, resultado.Tipo);
    }

    [Fact]
    public void Pagos_SaldoRetencion_DebeResolverAlias()
    {
        var resultado =
            ContratosPlantillasImportacion
                .Pagos
                .ResolverEncabezado(
                    " SALDO RETENCION ");

        Assert.Equal(
            "SALDO CRUZADO PENDIENTE",
            resultado);
    }

    [Fact]
    public void Contrato_ConColumnaFaltante_DebeReportarla()
    {
        string[] encabezados =
        [
            "FE",
            "PREFIJO",
            "FACTURA"
        ];

        var faltantes =
            ContratosPlantillasImportacion
                .Facturas
                .ObtenerEncabezadosFaltantes(
                    encabezados);

        Assert.Contains(
            "FECHA FACTURA",
            faltantes);

        Assert.Contains(
            "FACTURADOR",
            faltantes);
    }

    [Fact]
    public void Contrato_ConMovimientoAntiguo_DebeRechazarlo()
    {
        var encabezados =
            ContratosPlantillasImportacion
                .Facturas
                .EncabezadosRequeridos
                .Concat(
                    new[]
                    {
                        "AÑO 2026"
                    })
                .ToArray();

        var noReconocidos =
            ContratosPlantillasImportacion
                .Facturas
                .ObtenerEncabezadosNoReconocidos(
                    encabezados);

        Assert.Contains(
            "AÑO 2026",
            noReconocidos);

        Assert.Null(
            ContratosPlantillasImportacion
                .Detectar(encabezados));
    }

    [Fact]
    public void Obtener_Catalogos_DebeLanzarExcepcion()
    {
        var accion = () =>
            ContratosPlantillasImportacion.Obtener(
                TipoImportacion.Catalogos);

        Assert.Throws<
            ArgumentOutOfRangeException>(accion);
    }
}