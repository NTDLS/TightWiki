using Microsoft.Extensions.Localization;

namespace TightWiki.Static
{
    /// <summary>
    /// This class is just used to provide localization to areas that are not views, pages, or controllers.
    /// Like a global localization provider.
    /// </summary>
    public class StaticHelper
    {
        public static IStringLocalizer Localizer
        {
            get
            {
                if (_localizer == null)
                {
                    throw new InvalidOperationException("StaticHelper has not been initialized. Call StaticHelper.Initializer() with a valid IStringLocalizer instance.");
                }
                return _localizer;
            }
        }
        private static IStringLocalizer? _localizer;

        public static void Initialize(IStringLocalizer localizer)
        {
            _localizer = localizer;
        }
    }
}
