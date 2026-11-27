INSERT INTO ConfigurationEntry (ConfigurationGroupId, Name, Value, DataTypeId, Description, IsEncrypted, IsRequired)
SELECT
	(SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership'),
	'Notify Administrators of New Signup',
	'0',
	3,
	'Whether administrators are sent an email when a new account signs up. Requires a configured email server. Default: Disabled.',
	0,
	1
WHERE NOT EXISTS(
	SELECT 1 FROM ConfigurationEntry
	WHERE Name = 'Notify Administrators of New Signup'
	AND ConfigurationGroupId = (SELECT Id FROM ConfigurationGroup WHERE Name = 'Membership'));
