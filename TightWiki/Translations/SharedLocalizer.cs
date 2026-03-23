using Microsoft.Extensions.Localization;

namespace TightWiki.Translations
{
    public class SharedLocalizer
    {
        private static IServiceProvider? _serviceProvider;

        public static IStringLocalizer Create()
        {
            if (_serviceProvider == null)
            {
                throw new InvalidOperationException("SharedLocalizer not initialized.");
            }

            var factory = _serviceProvider.GetRequiredService<IStringLocalizerFactory>();

            return factory.Create(typeof(SharedLocalizer));
        }

        public static void Initialize(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
    }
}
