# TightWiki

[![Regression Tests](https://github.com/eMukator/TightWiki/actions/workflows/Regression%20Tests.yml/badge.svg)](https://github.com/eMukator/TightWiki/actions/workflows/Regression%20Tests.yml)

For years I’ve worked at places where we just needed a simple to use, searchable, unobtrusive, no-nonsense, collaborative and free place to dump documentation.
The first thing that comes to mind is a Wiki but for some reason I can never find anything that "checks all the boxes". Hopefully you'll find this one does for you.

:yum: TightWiki is an ASP.NET Core MVC Razor WIKI written in C# that sits on top of a SQLite database (zero configuration required).

> **This fork** extends the original project with built-in support for running on Microsoft SQL Server and
> PostgreSQL, via Entity Framework Core — see [Database Providers](#database-providers) below for how to choose
> one. Because the EF Core model is provider-agnostic, support for further relational databases (e.g.
> MySQL/MariaDB, Oracle) could be added the same way down the line. SQLite remains the default, zero-configuration
> option exactly as in the original project — nothing changes if you don't opt into a different provider. All
> three providers run the same regression suite in CI (see the badge above) — SQLite and SQL Server on
> `windows-latest`, PostgreSQL on `ubuntu-latest`.

:crossed_fingers: Play with the latest dev build at http://TightWiki.com/. If you want to edit, you can signup using google auth or native TightWiki login.

:eyes: Or check out the full wiki [documentation](https://tightwiki.com/Wiki%20Help%20::%20Wiki%20Help) to learn about the engine functionality.

:star: Ready to run it for yourself? Check out the [installation instructions](https://tightwiki.com/wiki_help::installation)!

:boom: Also be sure to check out the screenshots below the feature list...

:anguished: Its been like a modern retelling of Sisyphus, only this time the stone is RegEx.

# :astonished: Features (some of them anyway)
* MIT license, you can use it for free at home or at your business.
* Open source, you can make changes, submit fixes or just make suggestions.
* Completely customizable and rebrandable including name, title, footer, copyright and all images.
* Editor toolbar with markup, link insertion, feature search, and emojis.
* User signup can be disabled, enabled and can require users to verify email before logging in.
* Role-based and per-user security for managing who can read/edit/delete/moderate pages and namespaces.
* Easy page linking. Can even link to pages that do not exist and the link will subtly prompt you to create the page when logged in with a role that has page creation support.
* Admin shows missing pages, namespace metrics, users, roles, etc.
* Multi-language. Translated into 25 languages, so if you speak it - so does TightWiki.
* Manual account creation, editing and deletion.
* Emojis! Lots built-in, and you can add custom - including animations.
* Page creation templates - to assit in uniformity and rapid page creation.
* All dates/times are stored in UTC and localized for logged in users.
* Admin moderation which is driven by page processing instructions for things like page deletions, review, drafts, etc.
* Page versioning. Revisions can be viewed by the original page URL with a /r/number route or by logging in a viewing the full page history.
* Revertible page history.
* Theme-able, with 25+ built in themes.
* Drag-drop fie uploads / page attachments, images.
* Versioned file uploads.
* Namespace support so you can have multiple pages with the same name in different namespaces.
* Fully baked in documentation of all wiki functions.
* Wiki Markup allows you post non-formatted code and even auto-syntax highlighting for things like C#, PHP, SQL, etc. Can also explicitly specify language.
* Wiki markup supports basic formatting, headings and sub-headings, tagging, tables, callouts, alerts, variables, bullets lists, dynamic glossaries, inline search results, dynamic tag clouds, related linking, expanding sections, auto-table of contents, and much more.
* Wiki page editing is syntax highlighted.
* Built in search supports fuzzy matching to support even mild misspellings.
* Authentication: Built-in, Google, Microsoft, 2FA, OAuth, and LDAP.

# Default home page
![image](https://github.com/user-attachments/assets/7a8c0c6f-b865-415c-9b29-9833ba2cf58f)

# Site Metrics
We've beat the wiki up with more data than this, but this is our standard workload. ~45,000 pages, in ~400 namespaces, with ~250,000 revisions, created by ~1,000 users, manifesting ~5 million search tokens. The random fuzzy-match search time is 11 milliseconds. Not too shabby, right?

<img width="933" height="776" alt="image" src="https://github.com/user-attachments/assets/b2fc57b6-24b1-4f02-b2b0-3f6dc6730151" />

# Multiple languages.
<img width="927" height="557" alt="image" src="https://github.com/user-attachments/assets/8e99f359-320a-4696-b9a8-ead27ba5ed14" />

# Page search (inexact fuzzy matching with weighted tokens)
<img width="929" height="610" alt="image" src="https://github.com/user-attachments/assets/657927c6-f669-4e37-bdea-530c0e12adda" />

# Page History
<img width="1006" height="776" alt="image" src="https://github.com/user-attachments/assets/c0da4f47-5ce7-49c0-855e-0fbd17a0c78a" />

# Difference View
<img width="1004" height="773" alt="image" src="https://github.com/user-attachments/assets/dd90febf-e476-4344-85a4-774dbf644cae" />

# Example edit page
![image](https://github.com/user-attachments/assets/2e1205d2-fcd5-42aa-aa1a-ef35c5e9ac0a)

#Snippets
<img width="895" height="588" alt="image" src="https://github.com/user-attachments/assets/24b0d384-82d6-4a8d-acf5-030c44cd7578" />

# Multiple Themes
<img width="1022" height="702" alt="image" src="https://github.com/user-attachments/assets/af96a6b8-244c-4b9f-9e28-9aea9e35db7a" />

# Emojis Configuration 
<img width="1009" height="772" alt="image" src="https://github.com/user-attachments/assets/5e169d21-c616-452f-a70d-5d18f1add01e" />

# Role-based security
![image](https://github.com/user-attachments/assets/c8e84282-c4e3-4f57-8bca-60c7fe7df804)

# Compiliation
![image](https://github.com/user-attachments/assets/55dc9836-9dd4-4fcc-a922-923868d7d731)

# Deleted Pages
![image](https://github.com/user-attachments/assets/80559132-60dc-42ab-bb51-cb66d25658df)

# History and Revert
![image](https://github.com/user-attachments/assets/92f74860-afab-421e-9db9-99040e1d4431)

# Side-by-side Differences
<img width="920" height="627" alt="image" src="https://github.com/user-attachments/assets/6b03cbc2-ba85-4aaa-bd72-6facf00ff542" />

# Page Attachments
![image](https://github.com/user-attachments/assets/751640d8-bd3f-4b63-ae69-916f624f09bc)

# Attachment Revisions
![image](https://github.com/user-attachments/assets/a49540cb-24b7-42a7-b955-3c6fa17a5180)

# Built-in documentation list
![image](https://github.com/user-attachments/assets/00b23663-972d-4791-8698-99e54bbc601c)

# Built-in documentation example
![image](https://github.com/user-attachments/assets/ab6a1893-d0b5-4ba5-8262-d71423d8d49d)

# Configuration
![image](https://github.com/user-attachments/assets/8afe427e-cafd-48cc-92e3-a67529c379a3)

# Admin page list
![image](https://github.com/user-attachments/assets/9a8068c9-2176-4ee4-8670-5f74f3470002)

# Admin role list
![image](https://github.com/user-attachments/assets/2aa340d1-c1eb-4ee9-b3c7-91ff6e4f0a7b)

## Database Providers

By default TightWiki runs entirely on SQLite with zero configuration, as described above. It can also be built
against Microsoft SQL Server or PostgreSQL instead, via Entity Framework Core. This is a **build-time** choice,
not something you flip in a config file at runtime — pick the provider you want when you build/publish, and
deploy the resulting output as-is. Whichever provider you build with is the *only* datastore used at runtime;
a SQL Server/Postgres build never touches a `.db` file and never ships a SQLite package.

### Choosing a provider

The provider is selected via the MSBuild property `DataProvider` (`Sqlite` | `SqlServer` | `Postgres`), which
conditionally compiles the right bootstrap code in `TightWiki/Program.cs` and pulls in the matching project
reference in `TightWiki/TightWiki.csproj`. If `DataProvider` isn't specified, it defaults to `Sqlite`.

```
dotnet build .\TightWiki -p:DataProvider=SqlServer
dotnet publish .\TightWiki -c Release -p:DataProvider=Postgres --runtime linux-x64 --self-contained false
```

**Important:** after switching `-p:DataProvider=...`, run `dotnet restore` again (either without `-p`, or with
the same `DataProvider` value you're about to build with) before your next `dotnet build`/`dotnet publish`. Which
project reference gets restored (SQLite/Dapper vs. the EF Core driver project) is resolved at *restore* time, not
build time. Following a provider switch with `dotnet build --no-restore` (or a plain `dotnet build`/`dotnet test`
that implicitly falls back to the default `Sqlite`) against a stale restore for a different provider fails with a
confusing `CS0246: The type or namespace name 'Dapper' could not be found` (or a similar `NTDLS.SqliteDapperWrapper`
error) — that's not a code bug, just an out-of-date `project.assets.json` from the previous restore.

### Visual Studio Solution Configurations (Debug/Release per provider)

`TightWiki.csproj` declares six Solution Configurations — the normal `Debug`/`Release` (which stay `Sqlite`)
plus a Debug and a Release variant per EF Core provider:

| Configuration       | `DataProvider` | Debug symbols |
|----------------------|----------------|---------------|
| `Debug`               | `Sqlite`       | on            |
| `Release`              | `Sqlite`       | off           |
| `Debug-SqlServer`      | `SqlServer`    | on            |
| `Release-SqlServer`    | `SqlServer`    | off           |
| `Debug-Postgres`       | `Postgres`     | on            |
| `Release-Postgres`     | `Postgres`     | off           |

Each `Release-*` variant carries the same `DebugSymbols`/`DebugType` settings as plain `Release` — it's a real
Release build (no debug symbols), just targeting SQL Server/Postgres instead of SQLite; it isn't only a Debug-time
convenience. Pick any of the six from the Configuration dropdown in the toolbar (or Build → Configuration
Manager) exactly like you would Debug/Release today — no environment variable, no `.csproj.user`, no
closing/reopening Visual Studio; switching the dropdown re-evaluates every project the same way any other
configuration change would. Do a Rebuild Solution after switching — same restore gotcha as above, since NuGet
resolves the SQLite/EF-Core-driver project reference at restore time. For SQL Server via LocalDB,
`appsettings.Development.json` already ships a working `ConnectionStrings:TightWikiEfCore` value, so no further
configuration is needed to hit F5 after picking `Debug-SqlServer`.

An explicit `-p:DataProvider=...` (or the `DataProvider` OS environment variable, for CI/non-VS setups) still
overrides the Configuration mapping if both are present — see `TightWiki/TightWiki.csproj`.

### Connection strings

- **SQLite** (default): unchanged — `ConnectionStrings:DatabasePath` in `appsettings.json`, pointing at the
  folder holding the 8 SQLite `.db` files (plus optional per-database override keys such as `ConfigConnection`,
  `UsersConnection`, etc.).
- **SQL Server / PostgreSQL**: a single new key, `ConnectionStrings:TightWikiEfCore` — one connection string for
  the whole consolidated database (all 8 schemas plus ASP.NET Core Identity's `Users` schema). For example:

  ```jsonc
  // SQL Server / LocalDB
  "ConnectionStrings": {
    "TightWikiEfCore": "Server=(localdb)\\mssqllocaldb;Database=TightWiki;Trusted_Connection=True;"
  }
  ```

  ```jsonc
  // PostgreSQL
  "ConnectionStrings": {
    "TightWikiEfCore": "Host=localhost;Port=5432;Database=tightwiki;Username=postgres;Password=<password>"
  }
  ```

### Building with only the provider you need

`TightWiki.csproj`'s `ProjectReference`/`PackageReference` entries for SQLite (`TightWiki.Repository`,
`Microsoft.EntityFrameworkCore.Sqlite`) and for each EF Core driver (`TightWiki.Data.EfCore.SqlServer`,
`TightWiki.Data.EfCore.Postgres`) are conditioned on `DataProvider`, so a build only restores/compiles against
the provider you asked for — no hybrid setups, and no unused provider's NuGet packages or DLLs end up in the
published output.

### Seeding a new database

- SQLite keeps seeding new installs exactly as before, by copying the shipped `Data/*.db` files.
- SQL Server/Postgres builds instead read their initial data (configuration, themes, feature templates, default
  wiki pages, emoji + categories, menu items) from `Seed/tightwiki.seed.zip`, which must sit next to the
  published application. `Release.Build.bat` regenerates this package (via `GenerateSeedData`/
  `GenerateSeedData.bat`, from the developer's own populated `Data/*.db` files) and copies it into every SQL
  Server/Postgres publish output automatically — this generation step only ever runs on a developer machine as
  part of the release build, never at application runtime.

### Schema migrations

SQL Server/Postgres builds apply EF Core Migrations automatically on startup — for both the TightWiki model and
ASP.NET Core Identity (which shares the same connection string/database, in the `Users` schema) — so there is no
manual migration step to run when deploying or upgrading. `Generate-EfMigrations.ps1` (repo root) is a separate,
developer-only tool for regenerating the EF Core scaffold/migrations from the SQLite schema; it has no role in
running or deploying the application.

## License
[MIT](https://choosealicense.com/licenses/mit/)
