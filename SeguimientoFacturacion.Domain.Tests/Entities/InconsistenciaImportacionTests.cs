using SeguimientoFacturacion.Domain.Entities;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Domain.Tests.Entities;

public sealed class InconsistenciaImportacionTests
{
    [Fact]
    public void CrearInconsistencia_ConDatosValidos_DebeConservarInformacion()
    {
        var loteId = Guid.NewGuid();

        var inconsistencia =
            new InconsistenciaImportacion(
                loteImportacionId: loteId,
                severidad: SeveridadImportacion.Error,
                codigo: " catalogo_no_mapeado ",
                mensaje:
                    "El valor no existe en el catálogo.",
                numeroFila: 25,
                columna: " ASEGURADORA ",
                valorPresentado: "Entidad de prueba");

        Assert.NotEqual(
            Guid.Empty,
            inconsistencia.Id);

        Assert.Equal(
            loteId,
            inconsistencia.LoteImportacionId);

        Assert.Equal(
            "CATALOGO_NO_MAPEADO",
            inconsistencia.Codigo);

        Assert.Equal(
            25,
            inconsistencia.NumeroFila);

        Assert.Equal(
            "ASEGURADORA",
            inconsistencia.Columna);

        Assert.Equal(
            "Entidad de prueba",
            inconsistencia.ValorPresentado);
    }

    [Fact]
    public void CrearInconsistencia_General_DebePermitirFilaYColumnaNulas()
    {
        var inconsistencia =
            new InconsistenciaImportacion(
                loteImportacionId: Guid.NewGuid(),
                severidad: SeveridadImportacion.Error,
                codigo: "ARCHIVO_INVALIDO",
                mensaje:
                    "El archivo no tiene la estructura esperada.");

        Assert.Null(
            inconsistencia.NumeroFila);

        Assert.Null(
            inconsistencia.Columna);

        Assert.Null(
            inconsistencia.ValorPresentado);
    }

    [Fact]
    public void CrearInconsistencia_SinLote_DebeLanzarExcepcion()
    {
        var accion = () =>
            new InconsistenciaImportacion(
                loteImportacionId: Guid.Empty,
                severidad: SeveridadImportacion.Error,
                codigo: "ARCHIVO_INVALIDO",
                mensaje: "Archivo inválido.");

        Assert.Throws<ArgumentException>(
            accion);
    }

    [Fact]
    public void CrearInconsistencia_ConFilaCero_DebeLanzarExcepcion()
    {
        var accion = () =>
            new InconsistenciaImportacion(
                loteImportacionId: Guid.NewGuid(),
                severidad: SeveridadImportacion.Error,
                codigo: "FILA_INVALIDA",
                mensaje: "La fila no es válida.",
                numeroFila: 0);

        Assert.Throws<ArgumentOutOfRangeException>(
            accion);
    }

    [Fact]
    public void CrearInconsistencia_SinCodigo_DebeLanzarExcepcion()
    {
        var accion = () =>
            new InconsistenciaImportacion(
                loteImportacionId: Guid.NewGuid(),
                severidad: SeveridadImportacion.Error,
                codigo: " ",
                mensaje: "La fila no es válida.");

        Assert.Throws<ArgumentException>(
            accion);
    }

    [Fact]
    public void CrearInconsistencia_Sensible_DebeConservarIndicador()
    {
        var inconsistencia =
            new InconsistenciaImportacion(
                loteImportacionId: Guid.NewGuid(),
                severidad:
                    SeveridadImportacion.Advertencia,
                codigo: "DOCUMENTO_INVALIDO",
                mensaje:
                    "El documento necesita revisión.",
                numeroFila: 10,
                columna: "NÚMERO DTO",
                valorPresentado: "***1234",
                esDatoSensible: true);

        Assert.True(
            inconsistencia.EsDatoSensible);

        Assert.Equal(
            "***1234",
            inconsistencia.ValorPresentado);

        Assert.Equal(
            SeveridadImportacion.Advertencia,
            inconsistencia.Severidad);
    }
}