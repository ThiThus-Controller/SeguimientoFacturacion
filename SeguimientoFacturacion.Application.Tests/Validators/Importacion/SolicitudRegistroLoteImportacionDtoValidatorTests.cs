using SeguimientoFacturacion.Application.DTOs.Importacion;
using SeguimientoFacturacion.Application.Validators.Importacion;
using SeguimientoFacturacion.Domain.Enums;

namespace SeguimientoFacturacion.Application.Tests.Validators.Importacion;

/// <summary>
/// Pruebas del validador de registro de lotes.
/// </summary>
public sealed class
    SolicitudRegistroLoteImportacionDtoValidatorTests
{
    [Fact]
    public async Task Validar_ConSolicitudCorrecta_DebeSerValida()
    {
        var validator =
            new SolicitudRegistroLoteImportacionDtoValidator();

        using var contenido =
            new MemoryStream([1, 2, 3, 4]);

        var solicitud =
            new SolicitudRegistroLoteImportacionDto
            {
                Tipo = TipoImportacion.Facturas,
                NombreArchivo = "FACTURAS 2026.XLSX",
                Contenido = contenido,
                Usuario = "administrador"
            };

        var resultado =
            await validator.ValidateAsync(solicitud);

        Assert.True(resultado.IsValid);
        Assert.Empty(resultado.Errors);
    }

    [Fact]
    public async Task Validar_ConDatosBasicosInvalidos_DebeRetornarErrores()
    {
        var validator =
            new SolicitudRegistroLoteImportacionDtoValidator();

        using var contenido =
            new MemoryStream([1, 2, 3]);

        var solicitud =
            new SolicitudRegistroLoteImportacionDto
            {
                Tipo = (TipoImportacion)999,
                NombreArchivo = "facturas.csv",
                Contenido = contenido,
                Usuario = " "
            };

        var resultado =
            await validator.ValidateAsync(solicitud);

        Assert.False(resultado.IsValid);

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(
                    SolicitudRegistroLoteImportacionDto
                        .Tipo));

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(
                    SolicitudRegistroLoteImportacionDto
                        .NombreArchivo));

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(
                    SolicitudRegistroLoteImportacionDto
                        .Usuario));
    }

    [Fact]
    public async Task Validar_ConArchivoVacio_DebeRetornarError()
    {
        var validator =
            new SolicitudRegistroLoteImportacionDtoValidator();

        using var contenido =
            new MemoryStream();

        var solicitud =
            new SolicitudRegistroLoteImportacionDto
            {
                Tipo = TipoImportacion.Pagos,
                NombreArchivo = "Pagos.xlsx",
                Contenido = contenido,
                Usuario = "administrador"
            };

        var resultado =
            await validator.ValidateAsync(solicitud);

        Assert.False(resultado.IsValid);

        Assert.Contains(
            resultado.Errors,
            error =>
                error.PropertyName ==
                nameof(
                    SolicitudRegistroLoteImportacionDto
                        .Contenido));
    }
}