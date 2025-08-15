# Database Upgrade Initialization
When TightWiki is run, any scripts in the folders contained in "TightWiki.Repository\Scripts\Initialization\Versions"
are executed. The "previous version" of TightWiki is stored in the Config database VersionState table.

The scripts are executed in the order denoted by the name of the folders in "Version\*", these folders are
expected to be named with a three-part version scheme. MMM.mmm.ppp (major.minor.patch).

The scripts are only executed if the three-part folder version is
greater than the "previous version" from the VersionState table.

Theses scripts are executed in the order of their name as well, their name consists of three parts:
"\^EXECUTION_ORDER\^DATABASE_NAME\^SCRIPT_NAME" where the execution order should be a zero padded numeric string,
database name is the key from ManagedDataStorage.Collection, and script name is whatever you want to call it.

# Database Upgrade Script Macros
Supported upgrade scripts macros, these must exist on the first line of the script file:

* --##IF EXISTS(SELECT n FROM...)
* --##IF NOT EXISTS(SELECT n FROM...)
* --##IF TABLE EXISTS(TABLE_NAME)
* --##IF TABLE NOT EXISTS(TABLE_NAME)
* --##IF COLUMN EXISTS(TABLE_NAME, COLUMN_NAME)
* --##IF COLUMN NOT EXISTS(TABLE_NAME, COLUMN_NAME)
* --##IF INDEX EXISTS(TABLE_NAME)
* --##IF INDEX NOT EXISTS(TABLE_NAME)

# PreInitialization
The scripts in this folder are always run if the version changes.
They are run BEFORE all other scripts for the previous versions have been run.

# PostInitialization
The scripts in this folder are always run if the version changes.
They are run AFTER all other scripts for the previous versions have been run.
