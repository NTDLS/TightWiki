INSERT INTO ConfigurationEntry (ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership'),
	'Signup Terms',
	'',
	5,
	'HTML shown to people who sign up, with a checkbox they must tick before the account is created. Leave empty to not ask for agreement. Default: Empty.',
	0,
	0
WHERE NOT EXISTS(
	SELECT 1 FROM ConfigurationEntry
	WHERE Name = 'Signup Terms'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership'));
