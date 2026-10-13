# TightWiki Translations

The community translation site for TightWiki (translations.tightwiki.com). Anyone can search every translation in
`Translations.db`, suggest better wording, and vote on other people's suggestions. The best suggestions are
exported and merged back into TightWiki's translations.

* ASP.NET Core with Blazor (interactive server), .NET 10.
* The translations are the same `TightWiki/Translations/Translations.db` that ships with TightWiki. It is copied into the
  site when it is built, is read-only here, and is held in memory.
* Suggestions and votes live in their own `App_Data/Suggestions.db`, so publishing a new `Translations.db` never touches them.
* There are no accounts. Passing a Cloudflare Turnstile check gives a visitor a cookie with a random identifier, which is
  what their suggestions and votes are recorded against. Searching needs no check, only suggesting and voting do.

## Settings

Settings are in `appsettings.json` (the "Site" and "Turnstile" sections).

| Setting | Notes |
|---|---|
| `Turnstile:SiteKey` | Public, it is in the page. Already set. |
| `Turnstile:SecretKey` | **Secret. Never commit it.** See below. |
| `Turnstile:AllowedHostnames` | The host names a verified token may come from. Production is `translations.tightwiki.com`. Never put `localhost` here in production. |
| `Site:AdminKey` | Opens `/admin`. Leave empty to turn the admin page off. **Secret.** |
| `Site:AutoHideScore` | A suggestion is hidden once up-votes minus down-votes reaches this (default -5) unless an admin approves it. |
| `Site:SuggestionsPerHour`, `Site:VotesPerMinute`, `Site:GateAttemptsPerMinute` | Limits per visitor (and per address for the captcha and admin key). |
| `Site:SuggestionsPath`, `Site:TranslationsPath` | Where the databases are. Both default sensibly. |

### Keeping the secrets out of the repository

For development use user-secrets, which are stored outside the repository:

    dotnet user-secrets set "Turnstile:SecretKey" "<the secret key>" --project TightWiki.TranslationSite
    dotnet user-secrets set "Site:AdminKey" "<a long random key>" --project TightWiki.TranslationSite

(`appsettings.Development.json` has a throw-away admin key that only works when running as Development.)

In production set them as environment variables on the server instead: `Turnstile__SecretKey` and `Site__AdminKey`
(double underscore). In IIS that is the site's environment variables (or machine level).

### Languages

Most people read one or two of the 46 languages, so the site only asks about those.

* **Chosen by the visitor.** The "Choose your languages" button in the navigation bar (and the one on the home page) lets a visitor
  pick up to six. The home page's feed, the "Your vote counts" card and the missing translations are then limited to them, and
  search defaults to "My languages" (with "All languages" a click away).
* **Detected until then.** If they have not chosen, their browser's language settings (the `Accept-Language` header) are used, and the
  page says so. English is never picked, since everything is translated from it. A visitor whose browser only asks for English is
  asked which languages they read, and is not asked to vote until they have said.
* **Remembered in a cookie** called `tw_languages` (a list of language codes, or `-` for "show all languages"). It holds nothing about
  the person and is not used for anything else. It is a cookie rather than browser storage so that the server can use it when it draws
  the page, which means the right suggestions are there from the first moment instead of appearing after a flash of every language.

### Auto-translate (administrators only)

When signed in to `/admin`, a small magic-wand button appears beside each language in the search results. It has Einkr
translate that phrase from English and adds the result as a suggestion, which visitors then vote on like any other.
Visitors see these labelled "Draft", and the button itself is never shown to anyone else.

* Nothing is added if the result is the same as the current translation, or has already been suggested.
* The result goes through the same checks as a typed suggestion (right alphabet, intact `{0}` placeholders, one line).
* It needs an Einkr key: `Einkr:ApiKey` (**secret**, use user-secrets or the environment variable `Einkr__ApiKey`).
  Without one there is no button. `Einkr:Model` defaults to `Stratum-27B`, and `Einkr:AutoTranslatesPerMinute` (20) limits use.
* The instructions given to the model are in `Prompts/TranslationPrompt.txt`. It translates one phrase at a time, taking
  `{"phrase": "..."}` and answering `{"translation": "..."}`.

    dotnet user-secrets set "Einkr:ApiKey" "<your key>" --project TightWiki.TranslationSite

## Cloudflare

The widget's domain list in the Cloudflare dashboard must include `translations.tightwiki.com`, and `localhost` while developing.
The page asks for the action `verify` and the server rejects tokens for any other action or host.

## Running it

    dotnet run --project TightWiki.TranslationSite

then browse to http://localhost:5280. Without a secret key nobody can pass the captcha (the log says so).

## Publishing (IIS)

1. `dotnet publish TightWiki.TranslationSite -c Release`.
2. The site needs the **WebSocket Protocol** feature turned on in IIS, since Blazor Server uses SignalR.
3. The application pool's account needs write access to the `App_Data` folder, which is where `Suggestions.db` is created.
   **Do not let a publish delete or replace `App_Data`** (with Web Deploy choose to leave extra files on the destination).
4. Set the two environment variables above.

## Moderation and getting the results back

Open `/admin` and enter the admin key. Suggestions can be approved (always shown), hidden, restored or deleted.
`/admin/export.csv` and `/admin/export.json` list the best suggestion for each phrase and language that scores at least
the chosen minimum, ready to merge into `Translations.db` (for example with a small script, then `LocalizerScan --validate`).
