using Microsoft.Extensions.Localization;

namespace TightWiki.Translations
{
    public static class LocalizerFactory
    {
        private static IServiceProvider? _serviceProvider;

        /// <summary>
        /// Creates a localizer for the current thread (using the thread's language).
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
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
