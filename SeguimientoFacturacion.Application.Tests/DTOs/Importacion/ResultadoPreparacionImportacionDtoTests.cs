using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.DTOs.Importacion;

public sealed class ResultadoPreparacionImportacionDtoTests
{
    [Fact]
    public void Resultado_ConFacturas_DebeCalcularTotales()
    {
        var factura = CrearFacturaPreparada(
            [
                new MovimientoPreparadoImportacionDto
                {
                    HojaOrigen = "2024",
                    FilaOrigen = 3,
                    TipoMovimientoId =
                        TipoMovimientoCodigo.NotaCredito,
                    Anio = 2024,
                    Fecha = new DateOnly(2024, 8, 15),
                    Valor = 150000m,
                    NumeroNotaCredito = "NC-60195"
                },
                new MovimientoPreparadoImportacionDto
                {
                    HojaOrigen = "2024",
                    FilaOrigen = 3,
                    TipoMovimientoId =
                        TipoMovimientoCodigo.Abono,
                    Anio = 2024,
                    Fecha = null,
                    Valor = 200000m,
                    Observacion =
                        "Movimiento anual sin fecha exacta."
                }
            ]);

        var resultado =
            new ResultadoPreparacionImportacionDto
            {
                NombreArchivo = "Seguimiento 2024.xlsx",
                Facturas = [factura]
            };

        Assert.Equal(1, resultado.TotalFacturas);
        Assert.Equal(2, resultado.TotalMovimientos);
    }

    [Fact]
    public void Resultado_SinFacturas_DebeRetornarTotalesCero()
    {
        var resultado =
            new ResultadoPreparacionImportacionDto
            {
                NombreArchivo = "Seguimiento.xlsx"
            };

        Assert.Equal(0, resultado.TotalFacturas);
        Assert.Equal(0, resultado.TotalMovimientos);
    }

    private static FacturaPreparadaImportacionDto
        CrearFacturaPreparada(
            IReadOnlyCollection<
                MovimientoPreparadoImportacionDto> movimientos)
    {
        return new FacturaPreparadaImportacionDto
        {
            HojaOrigen = "2024",
            FilaOrigen = 3,
            IdentificadorFe = "FE4250",
            Prefijo = "FE",
            Numero = "4250",
            FechaFactura = new DateOnly(2024, 7, 10),
            AseguradoraId = 1,
            Valor = 1_000_000m,
            FechaRadicacion = null,
            TipoDocumentoId = 1,
            NumeroDocumento = "0012345678",
            NombreCompleto = "Paciente de prueba",
            AtencionId = 1,
            CostoId = 1,
            NumeroAdmision = "ADM-100",
            FechaAdmision = new DateOnly(2024, 7, 9),
            EstadoId = 5,
            FacturadorId = 1,
            Movimientos = movimientos
        };
    }
}