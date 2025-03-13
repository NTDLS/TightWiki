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
