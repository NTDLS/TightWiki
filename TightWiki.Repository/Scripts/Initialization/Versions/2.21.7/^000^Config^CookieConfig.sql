BEGIN TRANSACTION;

INSERT INTO ConfigurationGroup(Name, Description)
SELECT
	'Cookies', 'Authentication and Cookies.';

INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'Cookies'),
	'Persist Keys Path',
	NULL,
	2,
	'Filesystem location where persistent cookies are stored for saved session logins.',
	0,
	0;

INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'Cookies'),
	'Expiraion Hours',
	360,
	1,
	'Number of hours before cookies expire.',
	0,
	1;

COMMIT TRANSACTION;
