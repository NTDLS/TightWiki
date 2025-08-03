using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.ModelBinding.Binders;
using Microsoft.Extensions.Logging;
using NTDLS.Helpers;
using System.Globalization;

namespace TightWiki.Library
{
    public class InvariantDecimalModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            if (!context.Metadata.IsComplexType && (context.Metadata.ModelType == typeof(decimal) || context.Metadata.ModelType == typeof(decimal?)))
            {
                var loggerFactory = context.Services.GetService(typeof(ILoggerFactory)) as ILoggerFactory;
                return new InvariantDecimalModelBinder(context.Metadata.ModelType, loggerFactory.EnsureNotNull());
            }

            return null;
        }
    }

    public class InvariantDecimalModelBinder(Type modelType, ILoggerFactory loggerFactory) : IModelBinder
    {
        readonly SimpleTypeModelBinder _baseBinder = new SimpleTypeModelBinder(modelType, loggerFactory);

        public Task BindModelAsync(ModelBindingContext context)
        {
            ArgumentNullException.ThrowIfNull(context);

            var valueProviderResult = context.ValueProvider.GetValue(context.ModelName);

            if (valueProviderResult != ValueProviderResult.None)
            {
                context.ModelState.SetModelValue(context.ModelName, valueProviderResult);

                var valueAsString = valueProviderResult.FirstValue.EnsureNotNull().Replace(",", ".");

                if (decimal.TryParse(valueAsString, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out decimal result))
                {
                    context.Result = ModelBindingResult.Success(result);
                    return Task.CompletedTask;
                }
            }

            return _baseBinder.BindModelAsync(context);
        }
    }
}
