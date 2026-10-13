using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using TightWiki.TranslationSite.Services;

namespace TightWiki.TranslationSite.Components.Shared
{
    /// <summary>
    /// Base for pages that need to know who is looking at them and that refresh when suggestions change.
    /// </summary>
    public abstract class VisitorComponentBase : ComponentBase, IDisposable
    {
        [CascadingParameter] private Task<AuthenticationState>? AuthState { get; set; }

        [Inject] protected NavigationManager Nav { get; set; } = default!;
        [Inject] protected SuggestionService Suggestions { get; set; } = default!;
        [Inject] protected TranslationCatalog Catalog { get; set; } = default!;
        [Inject] protected LanguagePreferences Prefs { get; set; } = default!;

        protected Visitor Visitor { get; private set; } = Visitor.Anonymous;

        /// <summary>
        /// Pages that want to refresh when something changes return true.
        /// </summary>
        protected virtual bool RefreshOnChange => true;

        protected override async Task OnInitializedAsync()
        {
            if (AuthState != null)
            {
                Visitor = Visitor.From((await AuthState).User);
            }

            Prefs.EnsureInitialized();

            if (RefreshOnChange)
            {
                Suggestions.Changed += OnSuggestionsChanged;
                Prefs.Changed += OnSuggestionsChanged;
            }

            await LoadAsync();
        }

        /// <summary>
        /// Loads whatever the page shows. Called when the page opens and whenever something changes.
        /// </summary>
        protected virtual Task LoadAsync() => Task.CompletedTask;

        private void OnSuggestionsChanged()
        {
            //This is raised from whichever request made the change, so we have to hop back onto this page's context.
            _ = InvokeAsync(async () =>
            {
                await LoadAsync();
                StateHasChanged();
            });
        }

        /// <summary>
        /// Sends the visitor to the captcha, and then back to the page that they were on.
        /// </summary>
        protected void GoVerify()
        {
            var returnUrl = "/" + Nav.ToBaseRelativePath(Nav.Uri);
            Nav.NavigateTo($"/gate?returnUrl={Uri.EscapeDataString(returnUrl)}", forceLoad: true);
        }

        public virtual void Dispose()
        {
            Suggestions.Changed -= OnSuggestionsChanged;
            Prefs.Changed -= OnSuggestionsChanged;
            GC.SuppressFinalize(this);
        }
    }
}
