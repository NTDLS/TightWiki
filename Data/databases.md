## Config.db
The configuration for the site. Such as site name, email setup, customizations, etc.

## Defaults.db
The default data (configuration, help pages, built-in pages, feature templates) that is used for initial seeding of the other databases and for restoring defaults from the Database admin page. This is part of the application and is re-created at startup.

## DeletedPageRevisions.db
When page revisions are deleted, they are moved to this database. They can be viewed and restored from the Pages admin page.

## DeletedPages.db
When entire pages are deleted, they are moved to this database along with the details of their file attachments. The attachment files themselves stay in the Attachments folder until the deleted page is purged.

## Emoji.db
Contains all emoji image binaries and their tags.

## Logging.db
Contains the site event log, including exceptions.

## Pages.db
Contains site pages, the details of their file attachments, tags, processing instructions and search tokens.
Attachments uploaded before attachments were stored on disk also have their content in this database.

## Attachments
Not a database. The content of file attachments, one file per attachment revision, named `{PageFileId}\{Revision}` plus the extension of the uploaded file, for example `83\2.mp4`.
It is next to the databases unless the `AttachmentPath` setting in appsettings.json says otherwise.

## Statistics.db
When enabled, contains the wiki compilation statistics.

## Users.db
Contains user accounts and profiles, this is for local login and 3rd party authentication providers.

## Backups
Not a database. The Database admin page can create backups, each of which is a zip file containing a snapshot of every database, and optionally a copy of the Attachments folder. Restoring a database is a manual operation as the databases are locked for write while the site is running.
