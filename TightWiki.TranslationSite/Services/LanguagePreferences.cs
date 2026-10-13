using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System.Globalization;

namespace TightWiki.TranslationSite.Services
{
    /// <summary>
    /// What is worked out about the languages a visitor reads, and what carries it from the first (server rendered) page
    ///  to the interactive page that follows it.
    /// </summary>
    public sealed record LanguageSelection(string[] Codes, bool Detected);

    /// <summary>
    /// The languages that a visitor cares about, so that they are only asked about suggestions they can judge.
    /// It is what they chose (kept in a cookie, so it is the same next time), or until they choose, what their browser says
    ///  that they read. It is shared by everything on the page, so changing it in one place changes it everywhere.
    /// </summary>
    public sealed class LanguagePreferences
    {
        public const string CookieName = "tw_languages";

        /// <summary>
        /// What the cookie holds when the visitor has chosen to see every language.
        /// </summary>
        public const string NoneMarker = "-";

        /// <summary>
        /// Few people read more than a handful of languages, and a long list defeats the point of filtering.
        /// </summary>
        public const int MaxLanguages = 6;

        /// <summary>
        /// Enough to pick up the languages that someone reads without getting to ones that are only there as a fallback.
        /// </summary>
        public const int MaxDetected = 3;

        private const string StateKey = "tw-languages";

        private readonly TranslationCatalog _catalog;
        private readonly IHttpContextAccessor _accessor;
        private readonly PersistentComponentState _state;
        private readonly IJSRuntime _js;

        private PersistingComponentStateSubscription? _subscription;
        private bool _initialized;
        private List<string> _codes = new();

        public LanguagePreferences(TranslationCatalog catalog, IHttpContextAccessor accessor, PersistentComponentState state, IJSRuntime js)
        {
            _catalog = catalog;
            _accessor = accessor;
            _state = state;
            _js = js;
        }

        /// <summary>
        /// Raised when the languages are changed, so that every part of the page can update itself.
        /// </summary>
        public event Action? Changed;

        /// <summary>
        /// The chosen languages, in the order they were chosen.
        /// </summary>
        public IReadOnlyList<LanguageInfo> Selected { get; private set; } = Array.Empty<LanguageInfo>();

        public bool HasSelection => Selected.Count > 0;

        /// <summary>
        /// True while the languages are the ones that the browser asked for, because nothing has been chosen yet.
        /// </summary>
        public bool IsDetected { get; private set; }

        public IReadOnlyList<string> Names => Selected.Select(o => o.Name).ToList();

        /// <summary>
        /// The names to filter by, or null when every language should be shown.
        /// </summary>
        public IReadOnlyCollection<string>? NamesOrNull => HasSelection ? Names : null;

        public bool Contains(string code) => _codes.Contains(code, StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Works out the languages the first time it is needed. While the first page is being rendered on the server the request is
        ///  available, which has the cookie and the browser's language settings, and what is found is handed on to the interactive
        ///  page that takes over (where the request is no longer available).
        /// </summary>
        public void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;

            if (_state.TryTakeFromJson<LanguageSelection>(StateKey, out var persisted) && persisted != null)
            {
                Apply(persisted.Codes, persisted.Detected);
                return;
            }

            var request = _accessor.HttpContext?.Request;
            if (request == null)
            {
                return;
            }

            if (request.Cookies.TryGetValue(CookieName, out var cookie))
            {
                //They have chosen, even if what they chose is nothing (which means all languages).
                Apply(ParseCookie(_catalog, cookie), detected: false);
            }
            else
            {
                var detected = ParseAcceptLanguage(_catalog, request.Headers.AcceptLanguage.ToString());
                Apply(detected, detected.Count > 0);
            }

            _subscription = _state.RegisterOnPersisting(() =>
            {
                _state.PersistAsJson(StateKey, new LanguageSelection(_codes.ToArray(), IsDetected));
                return Task.CompletedTask;
            });
        }

        /// <summary>
        /// Chooses the languages, and remembers the choice for next time.
        /// </summary>
        public async Task SetAsync(IEnumerable<string> codes)
        {
            Apply(Clean(_catalog, codes), detected: false);
            Changed?.Invoke();

            try
            {
                await _js.InvokeVoidAsync("twPrefs.setLanguages", _codes.Count == 0 ? NoneMarker : string.Join(',', _codes));
            }
            catch (Exception ex) when (ex is JSException or JSDisconnectedException or InvalidOperationException or TaskCanceledException)
            {
                //The choice still applies to this visit, it just will not be remembered.
            }
        }

        /// <summary>
        /// Adds the language, or takes it away if it is already chosen.
        /// </summary>
        public Task ToggleAsync(string code)
        {
            var list = _codes.ToList();

            if (list.RemoveAll(o => o.Equals(code, StringComparison.OrdinalIgnoreCase)) > 0)
            {
                return SetAsync(list);
            }

            list.Add(code);
            return SetAsync(list);
        }

        private void Apply(IEnumerable<string> codes, bool detected)
        {
            _codes = Clean(_catalog, codes);
            IsDetected = detected && _codes.Count > 0;
            Selected = _codes.Select(o => _catalog.FindLanguageByTag(o)).OfType<LanguageInfo>().ToList();
        }

        private static List<string> Clean(TranslationCatalog catalog, IEnumerable<string> codes)
            => codes
                .Select(catalog.FindLanguageByTag)
                .OfType<LanguageInfo>()
                .Select(o => o.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxLanguages)
                .ToList();

        /// <summary>
        /// Reads the cookie (a comma separated list of language codes). It is whatever the browser sent, so anything that is not a
        ///  language that the site has is ignored.
        /// </summary>
        public static List<string> ParseCookie(TranslationCatalog catalog, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return new List<string>();
            }

            return Clean(catalog, value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
        }

        /// <summary>
        /// Reads the browser's Accept-Language header ("de-DE,de;q=0.9,en;q=0.8"): the languages it asks for, most wanted first,
        ///  that the site has. English is never one of them, because it is what everything is translated from.
        /// </summary>
        public static List<string> ParseAcceptLanguage(TranslationCatalog catalog, string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return new List<string>();
            }

            var wanted = new List<(string Tag, double Quality, int Order)>();
            var order = 0;

            foreach (var part in header.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var pieces = part.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
                var quality = 1.0;

                foreach (var parameter in pieces.Skip(1))
                {
                    if (parameter.StartsWith("q=", StringComparison.OrdinalIgnoreCase))
                    {
                        double.TryParse(parameter[2..], NumberStyles.Float, CultureInfo.InvariantCulture, out quality);
                    }
                }

                if (quality > 0 && pieces.Length > 0)
                {
                    wanted.Add((pieces[0], quality, order++));
                }
            }

            return wanted
                .OrderByDescending(o => o.Quality)
                .ThenBy(o => o.Order)
                .Select(o => catalog.FindLanguageByTag(o.Tag))
                .OfType<LanguageInfo>()
                .Select(o => o.Code)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(MaxDetected)
                .ToList();
        }
    }
}
