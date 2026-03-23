using Microsoft.Extensions.Localization;

namespace TightWiki.Translations
{
    public class SharedLocalizer
    {
        /// <summary>
        /// This class is just used to provide localization to areas that are not views, pages, or controllers.
        /// Like a global localization provider.
        /// </summary>
        public static IStringLocalizer Static
        {
            get
            {
                if (_localizer == null)
                {
                    throw new InvalidOperationException("SharedLocalizer has not been initialized. Call SharedLocalizer.InitializeStaticLocalizer() with a valid IStringLocalizer instance.");
                }
                return _localizer;
            }
        }
        private static IStringLocalizer? _localizer;

        public static void InitializeStaticLocalizer(IStringLocalizer localizer)
        {
            _localizer = localizer;
        }
    }
}
