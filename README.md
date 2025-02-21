# TightWiki

For years I’ve worked at places where we just needed a simple to use, searchable, unobtrusive, no-nonsense, collaborative and free place to dump documentation.
The first thing that comes to mind is a Wiki but for some reason I can never find anything that "checks all the boxes". Hopefully you'll find this one does for you.

:yum: TightWiki is an ASP.NET Core MVC Razor WIKI written in C# that sits on top of a SQLite database (zero configuration required).

:crossed_fingers: Play with the latest dev build at http://TightWiki.com/. If you want to edit, you can signup using google auth or native TightWiki login.

:eyes: Or check out the full wiki [documentation](https://tightwiki.com/Wiki%20Help%20::%20Wiki%20Help) to learn about the engine functionality.

:star: Ready to run it for yourself? Check out the [installation instructions](https://tightwiki.com/wiki_help::installation)!

:boom: Also be sure to check out the screenshots below the feature list...

:anguished: Its been like a modern retelling of Sisyphus, only this time the stone is RegEx.

# :astonished: Features (some of them anyway)
* MIT license, you can use it for free at home or at your business.
* Open source, you can make changes, submit fixes or just make suggestions.
* Completely customizable and rebrandable including name, title, footer, copyright and all images.
* User signup can be disabled, enabled and can require users to verify email before logging in.
* Multiple user roles are supported for admin, moderators, contributors and basic members.
* Easy page linking. Can even link to pages that do not exist and the link will subtly prompt you to create the page when logged in with a role that has page creation support.
* Admin shows missing pages, namespace metrics, users, roles, etc.
* Manual account creation, editing and deletion.
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

# Default home page
![image](https://github.com/user-attachments/assets/7ed1dbcb-0763-4a0e-a875-fe1364e876c7)

# Site Metrics
We've beat the wiki up with more data than this, but this is our standard workload. ~45,000 pages, in ~400 namespaces, with ~250,000 revisions, created by ~1,000 users, manifesting ~5 million search tokens. The random fuzzy-match search time is 11 milliseconds. Not too shabby, right?

![image](https://github.com/user-attachments/assets/02d9da1f-e164-44ce-aee9-0c42eedc4180)

# Page search (inexact fuzzy matching with weighted tokens)
![image](https://github.com/user-attachments/assets/b3caf0eb-32cf-43ad-885d-b2c8d10ae1ef)

# Page History
![image](https://github.com/user-attachments/assets/3dba0f07-5758-4039-9b2c-0b7a354e76f3)

# Example edit page
![image](https://github.com/user-attachments/assets/10eb3281-dad9-41fe-ba11-55019515e343)

# Build in documentation list
![image](https://github.com/user-attachments/assets/00b23663-972d-4791-8698-99e54bbc601c)

# Build in documentation example
![image](https://github.com/user-attachments/assets/ab6a1893-d0b5-4ba5-8262-d71423d8d49d)

# Configuration
![image](https://github.com/user-attachments/assets/8afe427e-cafd-48cc-92e3-a67529c379a3)

# Admin page list
![image](https://github.com/user-attachments/assets/9a8068c9-2176-4ee4-8670-5f74f3470002)

# Admin role list
![image](https://github.com/user-attachments/assets/2aa340d1-c1eb-4ee9-b3c7-91ff6e4f0a7b)


