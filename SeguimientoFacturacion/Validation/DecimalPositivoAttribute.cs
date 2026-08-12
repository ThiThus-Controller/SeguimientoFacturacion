using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace SeguimientoFacturacion.Validation;

/// <summary>
/// Valida valores decimales monetarios estrictamente mayores que cero.
/// </summary>
[AttributeUsage(
    AttributeTargets.Property |
    AttributeTargets.Field |
    AttributeTargets.Parameter)]
public sealed class DecimalPositivoAttribute :
    ValidationAttribute,
    IClientModelValidator
{
    private const string MensajePredeterminado =
        "El valor de la factura debe ser mayor que cero.";

    /// <summary>
    /// Inicializa una validación monetaria positiva.
    /// </summary>
    public DecimalPositivoAttribute()
        : base(MensajePredeterminado)
    {
    }

    /// <inheritdoc />
    public override bool IsValid(object? value)
    {
        return value is null ||
            value is decimal valorDecimal &&
            valorDecimal > decimal.Zero;
    }

    /// <inheritdoc />
    public void AddValidation(
        ClientModelValidationContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        AgregarAtributoSiNoExiste(
            context.Attributes,
            "data-val",
            "true");

        AgregarAtributoSiNoExiste(
            context.Attributes,
            "data-val-decimalpositivo",
            FormatErrorMessage(
                context.ModelMetadata.GetDisplayName()));
    }

    private static void AgregarAtributoSiNoExiste(
        IDictionary<string, string> atributos,
        string clave,
        string valor)
    {
        if (!atributos.ContainsKey(clave))
        {
            atributos.Add(clave, valor);
        }
    }
}
