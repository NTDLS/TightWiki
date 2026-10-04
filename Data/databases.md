## Config.db
The configuration for the site. Such as site name, email setup, customizations, etc.

## Defaults.db
The default data (configuration, themes, help pages, built-in pages, feature templates) that is used for initial seeding of the other databases and for restoring defaults from the Database admin page. This is part of the application and is re-created at startup.

## DeletedPageRevisions.db
When page revisions are deleted, they are moved to this database. They can be viewed and restored from the Pages admin page.

## DeletedPages.db
When entire pages are deleted, they are moved to this database along with all of their file attachments.

## Emoji.db
Contains all emoji image binaries and their tags.

## Logging.db
Contains the site event log, including exceptions.

## Pages.db
Contains site pages, their file attachments, tags, processing instructions and search tokens.

## Statistics.db
When enabled, contains the wiki compilation statistics.

## Users.db
Contains user accounts and profiles, this is for local login and 3rd party authentication providers.

## Backups
Not a database. The Database admin page can create backups, each of which is a zip file containing a snapshot of every database. Restoring a database is a manual operation as the databases are locked for write while the site is running.
