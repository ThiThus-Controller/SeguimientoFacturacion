using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Validators.Importacion;

namespace SeguimientoFacturacion.Application.Tests.Validators.Importacion;

/// <summary>
/// Pruebas del validador de solicitudes de análisis.
/// </summary>
public sealed class
    SolicitudAnalisisImportacionDtoValidatorTests
{
    private readonly SolicitudAnalisisImportacionDtoValidator
        _validator = new();

    [Theory]
    [InlineData("Seguimiento 2026.xlsx")]
    [InlineData("seguimiento 2026.XLSX")]
    public void Validar_ArchivoXlsxNoVacio_DebeSerValido(
        string nombreArchivo)
    {
        using var contenido =
            new MemoryStream([1, 2, 3, 4]);

        var solicitud =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = nombreArchivo,
                Contenido = contenido
            };

        var resultado = _validator.Validate(solicitud);

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData("Seguimiento 2026.xls")]
    [InlineData("Seguimiento 2026.csv")]
    [InlineData("Seguimiento 2026.xlsx.exe")]
    public void Validar_ExtensionNoPermitida_DebeSerInvalido(
        string nombreArchivo)
    {
        using var contenido =
            new MemoryStream([1, 2, 3, 4]);

        var solicitud =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = nombreArchivo,
                Contenido = contenido
            };

        var resultado = _validator.Validate(solicitud);

        Assert.False(resultado.IsValid);

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(
                    SolicitudAnalisisImportacionDto
                        .NombreArchivo));
    }

    [Fact]
    public void Validar_ContenidoVacio_DebeSerInvalido()
    {
        using var contenido = new MemoryStream();

        var solicitud =
            new SolicitudAnalisisImportacionDto
            {
                NombreArchivo = "Seguimiento 2026.xlsx",
                Contenido = contenido
            };

        var resultado = _validator.Validate(solicitud);

        Assert.False(resultado.IsValid);

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(
                    SolicitudAnalisisImportacionDto
                        .Contenido));
    }
}