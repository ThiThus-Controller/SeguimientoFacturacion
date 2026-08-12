using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SeguimientoFacturacion.ModelBinding;

/// <summary>
/// Proporciona el enlazador flexible para propiedades decimales.
/// </summary>
public sealed class DecimalFlexibleModelBinderProvider :
    IModelBinderProvider
{
    private static readonly IModelBinder Enlazador =
        new DecimalFlexibleModelBinder();

    /// <inheritdoc />
    public IModelBinder? GetBinder(
        ModelBinderProviderContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var tipoModelo = context.Metadata.ModelType;
        var tipoSubyacente =
            Nullable.GetUnderlyingType(tipoModelo) ?? tipoModelo;

        return tipoSubyacente == typeof(decimal)
            ? Enlazador
            : null;
    }
}
