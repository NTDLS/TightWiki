BEGIN TRANSACTION;

INSERT INTO ConfigurationEntry(ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE name = 'External Authentication'),
	'OIDC : Use OIDC Authentication',
	0,
	(SELECT Id FROM DataType WHERE name = 'Boolean'),
	'Whether or not OpenID Connect is enabled.',
	0,
	0
UNION ALL
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE name = 'External Authentication'),
	'OIDC : Authority',
	'',
	(SELECT Id FROM DataType WHERE name = 'String'),
	'',
	0,
	0
UNION ALL
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE name = 'External Authentication'),
	'OIDC : ClientId',
	'',
	(SELECT Id FROM DataType WHERE name = 'String'),
	'',
	0,
	0
UNION ALL
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE name = 'External Authentication'),
	'OIDC : ClientSecret',
	'',
	(SELECT Id FROM DataType WHERE name = 'String'),
	'',
	0,
	0;

COMMIT TRANSACTION;
