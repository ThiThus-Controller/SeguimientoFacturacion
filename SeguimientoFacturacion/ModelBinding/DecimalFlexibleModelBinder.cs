using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace SeguimientoFacturacion.ModelBinding;

/// <summary>
/// Enlaza propiedades decimales aceptando punto o coma como separador.
/// </summary>
public sealed class DecimalFlexibleModelBinder : IModelBinder
{
    /// <inheritdoc />
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);

        var resultadoValor =
            bindingContext.ValueProvider.GetValue(
                bindingContext.ModelName);

        if (resultadoValor == ValueProviderResult.None)
        {
            return Task.CompletedTask;
        }

        bindingContext.ModelState.SetModelValue(
            bindingContext.ModelName,
            resultadoValor);

        var valorPresentado = resultadoValor.FirstValue;

        if (string.IsNullOrWhiteSpace(valorPresentado))
        {
            if (Nullable.GetUnderlyingType(
                    bindingContext.ModelType) == typeof(decimal))
            {
                bindingContext.Result =
                    ModelBindingResult.Success(null);
            }

            return Task.CompletedTask;
        }

        if (ConversorDecimalFlexible.IntentarConvertir(
                valorPresentado,
                out var valorDecimal))
        {
            bindingContext.Result =
                ModelBindingResult.Success(valorDecimal);

            return Task.CompletedTask;
        }

        var nombreCampo =
            bindingContext.ModelMetadata.DisplayName ??
            bindingContext.FieldName;

        bindingContext.ModelState.TryAddModelError(
            bindingContext.ModelName,
            $"El campo {nombreCampo} debe ser un número válido " +
            "con máximo dos decimales.");

        return Task.CompletedTask;
    }
}
