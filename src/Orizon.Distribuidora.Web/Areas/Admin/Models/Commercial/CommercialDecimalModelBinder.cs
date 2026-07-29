using System.Globalization;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Orizon.Distribuidora.Web.Areas.Admin.Models.Commercial;

public sealed class CommercialDecimalModelBinder : IModelBinder
{
    private static readonly CultureInfo PtBr = CultureInfo.GetCultureInfo("pt-BR");

    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        ArgumentNullException.ThrowIfNull(bindingContext);
        var valueResult = bindingContext.ValueProvider.GetValue(bindingContext.ModelName);
        if (valueResult == ValueProviderResult.None) return Task.CompletedTask;

        bindingContext.ModelState.SetModelValue(bindingContext.ModelName, valueResult);
        var raw = valueResult.FirstValue?.Trim();
        if (string.IsNullOrEmpty(raw))
        {
            if (Nullable.GetUnderlyingType(bindingContext.ModelType) is not null)
                bindingContext.Result = ModelBindingResult.Success(null);
            return Task.CompletedTask;
        }

        const NumberStyles style = NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint |
            NumberStyles.AllowThousands;
        var primaryCulture = raw.Contains(',') ? PtBr : CultureInfo.InvariantCulture;
        var fallbackCulture = raw.Contains(',') ? CultureInfo.InvariantCulture : PtBr;
        if (decimal.TryParse(raw, style, primaryCulture, out var parsed) ||
            decimal.TryParse(raw, style, fallbackCulture, out parsed))
        {
            bindingContext.Result = ModelBindingResult.Success(parsed);
            return Task.CompletedTask;
        }

        bindingContext.ModelState.TryAddModelError(bindingContext.ModelName,
            $"O valor “{raw}” não é um número válido.");
        return Task.CompletedTask;
    }
}
