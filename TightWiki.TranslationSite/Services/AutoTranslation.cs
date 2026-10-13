using Microsoft.Extensions.Options;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;
using VPT.Einkr.Client;

namespace TightWiki.TranslationSite.Services
{
    /// <summary>
    /// Something that can translate an English phrase into another language.
    /// </summary>
    public interface ITranslationModel
    {
        /// <summary>
        /// False when the model has not been set up (such as there being no API key), in which case nothing is offered.
        /// </summary>
        bool IsAvailable { get; }

        /// <summary>
        /// Translates the phrase, or returns null when the model did not produce a usable answer.
        /// </summary>
        Task<string?> TranslateAsync(string english, string languageName, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Translates one phrase with an Einkr model. The phrase goes in, and the answer comes back, as JSON, so that the answer
    ///  can be read without guessing where it starts and ends (no quotation marks, notes or explanations to strip off).
    /// The instructions are in Prompts/TranslationPrompt.txt, which began as LocalizerScan's prompt for translating batches.
    /// </summary>
    public sealed class EinkrTranslationModel : ITranslationModel, IDisposable
    {
        private static readonly JsonSerializerOptions _inputOptions = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

        private readonly EinkrAIClient? _client;
        private readonly string _prompt;

        public EinkrTranslationModel(IOptions<EinkrOptions> options)
        {
            var settings = options.Value;

            if (!string.IsNullOrWhiteSpace(settings.ApiKey))
            {
                _client = new EinkrAIClient(settings.Model, settings.ApiKey, string.IsNullOrWhiteSpace(settings.BaseUrl) ? null : settings.BaseUrl);
            }

            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("TranslationPrompt.txt")
                ?? throw new InvalidOperationException("The translation prompt is missing from the site.");
            using var reader = new StreamReader(stream);
            _prompt = reader.ReadToEnd();
        }

        public bool IsAvailable => _client != null;

        public async Task<string?> TranslateAsync(string english, string languageName, CancellationToken cancellationToken = default)
        {
            if (_client == null)
            {
                return null;
            }

            var instructions = _prompt
                .Replace("{sourceLanguage}", TranslationCatalog.SourceLanguage)
                .Replace("{targetLanguage}", languageName);

            //Relaxed escaping keeps characters such as < and & as they are, since they can be part of the phrase.
            var input = JsonSerializer.Serialize(new { phrase = english }, _inputOptions);

            //There is nothing to think over for one short phrase, and the wording should be steady rather than creative.
            var response = await _client.CompleteChatAsync([
                new SystemChatMessage(instructions),
                new UserChatMessage(input)
            ], enableThinking: false, maxTokens: 1024, temperature: 0.2, cancellationToken: cancellationToken);

            return ParseResponse(response.Content[0].Text);
        }

        /// <summary>
        /// Reads {"translation": "..."} from the model's reply. A code fence around it, or a sentence before or after it, is
        ///  tolerated since models do that, but anything that is not that object with a text value is not used.
        /// </summary>
        public static string? ParseResponse(string? text)
        {
            text = text?.Trim();
            if (string.IsNullOrEmpty(text))
            {
                return null;
            }

            //Remove a code fence such as ```json ... ```.
            if (text.StartsWith("```", StringComparison.Ordinal))
            {
                var firstLine = text.IndexOf('\n');
                var lastFence = text.LastIndexOf("```", StringComparison.Ordinal);
                if (firstLine > 0 && lastFence > firstLine)
                {
                    text = text[(firstLine + 1)..lastFence].Trim();
                }
            }

            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start)
            {
                return null;
            }

            try
            {
                using var document = JsonDocument.Parse(text[start..(end + 1)]);

                if (document.RootElement.ValueKind == JsonValueKind.Object
                    && document.RootElement.TryGetProperty("translation", out var translation)
                    && translation.ValueKind == JsonValueKind.String)
                {
                    return NullIfEmpty(translation.GetString() ?? string.Empty);
                }
            }
            catch (JsonException)
            {
                //Not JSON, so it is not an answer.
            }

            return null;
        }

        private static string? NullIfEmpty(string value)
        {
            value = value.Trim();
            return value.Length == 0 ? null : value;
        }

        public void Dispose() => _client?.Dispose();
    }

    public sealed record AutoTranslateResult(bool Added, string Message, Suggestion? Suggestion);

    /// <summary>
    /// Lets an administrator have a phrase translated from English and added as a suggestion, which people can then vote on.
    /// </summary>
    public sealed class AutoTranslationService
    {
        private readonly TranslationCatalog _catalog;
        private readonly SuggestionService _suggestions;
        private readonly ITranslationModel _model;
        private readonly EinkrOptions _options;
        private readonly ILogger<AutoTranslationService> _logger;
        private readonly SlidingWindowLimiter _limiter;

        public AutoTranslationService(TranslationCatalog catalog, SuggestionService suggestions, ITranslationModel model,
            IOptions<EinkrOptions> options, ILogger<AutoTranslationService> logger, TimeProvider? time = null)
        {
            _catalog = catalog;
            _suggestions = suggestions;
            _model = model;
            _options = options.Value;
            _logger = logger;
            _limiter = new SlidingWindowLimiter(time ?? TimeProvider.System);
        }

        /// <summary>
        /// Whether there is anything to offer. The button is only shown to administrators, and only when this is true.
        /// </summary>
        public bool IsAvailable => _model.IsAvailable;

        public async Task<AutoTranslateResult> TranslateAsync(Visitor requester, string english, string language, CancellationToken cancellationToken = default)
        {
            //The page only shows the button to administrators, but this is what actually decides.
            if (!requester.IsAdmin || requester.Id == null)
            {
                return new AutoTranslateResult(false, "Only administrators can do that.", null);
            }

            if (!_model.IsAvailable)
            {
                return new AutoTranslateResult(false, "Auto-translate has not been set up on this site.", null);
            }

            var phrase = _catalog.FindByEnglish(english);
            var languageInfo = _catalog.FindLanguage(language);
            if (phrase == null || languageInfo == null)
            {
                return new AutoTranslateResult(false, "That phrase or language could not be found.", null);
            }

            if (!_limiter.TryAcquire(requester.Id, Math.Max(1, _options.AutoTranslatesPerMinute), TimeSpan.FromMinutes(1)))
            {
                return new AutoTranslateResult(false, "Auto-translate is being used very quickly. Please wait a moment.", null);
            }

            string? translated;
            try
            {
                translated = await _model.TranslateAsync(phrase.English, languageInfo.Name, cancellationToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                //The details can include what the service said about the request, which is for the log and not for the page.
                _logger.LogWarning(ex, "Auto-translating '{Phrase}' to {Language} failed.", phrase.English, languageInfo.Name);
                return new AutoTranslateResult(false, "The translation service did not respond. Please try again.", null);
            }

            if (string.IsNullOrWhiteSpace(translated))
            {
                _logger.LogWarning("The translation service returned nothing usable for '{Phrase}' in {Language}.", phrase.English, languageInfo.Name);
                return new AutoTranslateResult(false, "The translation service returned nothing usable.", null);
            }

            var result = _suggestions.SubmitAutomatic(phrase.English, languageInfo.Name, translated);
            if (result.Ok)
            {
                _logger.LogInformation("Auto-translated '{Phrase}' to {Language}.", phrase.English, languageInfo.Name);
                return new AutoTranslateResult(true, $"Added \"{result.Suggestion?.Text ?? translated}\" as a suggestion.", result.Suggestion);
            }

            //Nothing is added when the result is the same as what is already there, or what has already been suggested.
            var message = result.Error switch
            {
                "That is already the current translation." => "The auto-translation is the same as the current translation, so nothing was added.",
                "That has already been suggested." => "That translation has already been suggested, so nothing was added.",
                _ => $"Not added. {result.Error}"
            };

            return new AutoTranslateResult(false, message, result.Suggestion);
        }
    }
}
